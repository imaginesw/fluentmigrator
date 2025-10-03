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
#if NET8_0_OR_GREATER
        // .NET 8: carrega a Sap.Data.Hana.Net.v8.0
        const string asmName = "Sap.Data.Hana.Net.v8.0";
#else
            // .NET Framework: carrega a Sap.Data.Hana.v4.5
            const string asmName = "Sap.Data.Hana.v4.5";
#endif
            var asm = Load(asmName);
            var t = asm.GetType("Sap.Data.Hana.HanaFactory", throwOnError: true)!;
            var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)!;
            var f = p.GetValue(null) as DbProviderFactory;
            if (f is null) throw new InvalidOperationException("HanaFactory.Instance não retornou DbProviderFactory.");
            return f;
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
