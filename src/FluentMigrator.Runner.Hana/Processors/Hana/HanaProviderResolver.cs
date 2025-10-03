using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FluentMigrator.Runner.Processors.Hana
{
    public static class HanaProviderResolver
    {
        public static DbProviderFactory GetFactory()
        {
            var asmName = Environment.Version.Major >= 8
            ? "Sap.Data.Hana.Net.v8.0"
            : "Sap.Data.Hana.v4.5";

            var asm = Load(asmName);

            // Ordem de tentativa: .NET moderno primeiro
            var typeNames = new[]
            {
            "Sap.Data.Hana.HanaClientFactory",   // .NET 6/7/8/9
            "Sap.Data.Hana.Core.HanaClientFactory", // caso raro
            "Sap.Data.Hana.HanaFactory"          // .NET Framework / legado
        };

            foreach (var tn in typeNames)
            {
                var t = asm.GetType(tn, throwOnError: false, ignoreCase: false);
                if (t == null) continue;

                // 1) Propriedade pública estática "Instance"
                var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (p?.PropertyType != null && typeof(DbProviderFactory).IsAssignableFrom(p.PropertyType))
                {
                    if (p.GetValue(null) is DbProviderFactory f1) return f1;
                }

                // 2) Campo público estático "Instance" (alguns providers usam field)
                var fld = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (fld?.FieldType != null && typeof(DbProviderFactory).IsAssignableFrom(fld.FieldType))
                {
                    if (fld.GetValue(null) is DbProviderFactory f2) return f2;
                }

                // 3) Se o próprio tipo herda de DbProviderFactory e tem ctor público/sem parâmetros
                if (typeof(DbProviderFactory).IsAssignableFrom(t))
                {
                    try
                    {
                        var f3 = Activator.CreateInstance(t) as DbProviderFactory;
                        if (f3 != null) return f3;
                    }
                    catch { /* ignora e tenta o próximo */ }
                }
            }

            // Diagnóstico útil:
            var where = asm.Location;
            var available = string.Join(", ",
                 asm.GetTypes()
                    .Where(x =>
                        x.Name.IndexOf("Factory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        x.FullName?.IndexOf("Hana", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(x => x.FullName)
                    .OrderBy(x => x)
                    .Take(30));

            throw new InvalidOperationException(
                $"Não foi possível obter uma DbProviderFactory do assembly '{asm.FullName}' em '{where}'. " +
                $"Tipos relacionados encontrados: {available}");
        }

        private static Assembly Load(string simpleName)
        {
            // 1) Tenta do AppDomain (se já estiver carregada)
            var loaded = Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                                    a => string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
            if (loaded != null) return loaded;

            try { return Assembly.Load(new AssemblyName(simpleName)); }
            catch
            {
                // 2) Fallback: procura ao lado do executável
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, simpleName + ".dll");
                return Assembly.LoadFrom(path);
            }
        }
    }
}
