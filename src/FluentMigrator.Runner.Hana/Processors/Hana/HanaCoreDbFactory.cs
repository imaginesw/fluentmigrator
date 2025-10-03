using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sap.Data.Hana;

namespace FluentMigrator.Runner.Processors.Hana
{
    public sealed  class HanaCoreDbFactory
    {
        /// <summary>
        /// Delegate que devolve sempre a DbProviderFactory do HANA v8.
        /// </summary>
        public Func<DbProviderFactory> Factory { get; } = () => HanaFactory.Instance;
    }
}
