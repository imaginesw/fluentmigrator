using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using FluentMigrator.Generation;
using FluentMigrator.Runner.Generators.Hana;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors.Hana;
using FluentMigrator.Tests.Helpers;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Shouldly;

namespace FluentMigrator.Tests.Integration.Processors.Hana
{
    [TestFixture]
    [Category("Integration")]
    [Category("Hana")]
    public class HanaCoreColumnTests : BaseColumnTests
    {
        private ServiceProvider ServiceProvider { get; set; }
        private IServiceScope ServiceScope { get; set; }
        private HanaCoreProcessor Processor { get; set; }
        private IQuoter Quoter { get; set; }

        [Test]
        public override void CallingColumnExistsCanAcceptColumnNameWithSingleQuote()
        {
            var columnNameWithSingleQuote = Quoter.Quote("i'd");
            using (var table = new HanaCoreTestTable(Processor, null, $"{columnNameWithSingleQuote} int"))
                Processor.ColumnExists(null, table.Name, "i'd").ShouldBeTrue();
        }

        [Test]
        public override void CallingColumnExistsCanAcceptTableNameWithSingleQuote()
        {
            using (var table = new HanaCoreTestTable("Test'Table", Processor, null, "id int"))
                Processor.ColumnExists(null, table.Name, "id").ShouldBeTrue();
        }

        [Test]
        public override void CallingColumnExistsReturnsFalseIfColumnDoesNotExist()
        {
            using (var table = new HanaCoreTestTable(Processor, null, "id int"))
                Processor.ColumnExists(null, table.Name, "DoesNotExist").ShouldBeFalse();
        }

        [Test]
        public override void CallingColumnExistsReturnsFalseIfColumnDoesNotExistWithSchema()
        {
            using (var table = new HanaCoreTestTable(Processor, "test_schema", "id int"))
                Processor.ColumnExists("test_schema", table.Name, "DoesNotExist").ShouldBeFalse();
        }

        [Test]
        public override void CallingColumnExistsReturnsFalseIfTableDoesNotExist()
        {
            Processor.ColumnExists(null, "DoesNotExist", "DoesNotExist").ShouldBeFalse();
        }

        [Test]
        public override void CallingColumnExistsReturnsFalseIfTableDoesNotExistWithSchema()
        {
            Processor.ColumnExists("test_schema", "DoesNotExist", "DoesNotExist").ShouldBeFalse();
        }

        [Test]
        public override void CallingColumnExistsReturnsTrueIfColumnExists()
        {
            using (var table = new HanaCoreTestTable(Processor, null, "id int"))
                Processor.ColumnExists(null, table.Name, "id").ShouldBeTrue();
        }

        [Test]
        public override void CallingColumnExistsReturnsTrueIfColumnExistsWithSchema()
        {
            using (var table = new HanaCoreTestTable(Processor, "test_schema", "id int"))
                Processor.ColumnExists("test_schema", table.Name, "id").ShouldBeTrue();
        }

        [OneTimeSetUp]
        public void ClassSetUp()
        {
            IntegrationTestOptions.Hana.IgnoreIfNotEnabled();

            var serivces = ServiceCollectionExtensions.CreateServices()
                .ConfigureRunner(builder => builder.AddHanaNetCore())
                .AddScoped<IConnectionStringReader>(
                    _ => new PassThroughConnectionStringReader(IntegrationTestOptions.Hana.ConnectionString));
            ServiceProvider = serivces.BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void ClassTearDown()
        {
            ServiceProvider?.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            ServiceScope = ServiceProvider.CreateScope();
            Processor = ServiceScope.ServiceProvider.GetRequiredService<HanaCoreProcessor>();
            Quoter = ServiceScope.ServiceProvider.GetRequiredService<HanaQuoter>();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceScope?.Dispose();
            Processor?.Dispose();
        }
    }
}
