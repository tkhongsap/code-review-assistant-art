using Argento.ReportingService.Utility;
using Arcadia.Extensions.DependencyInjection.Attributes;
using Arcadia.Extensions.DependencyInjection.Enums;
using Arcadia.Repository.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Argento.ReportingService.Repository.ReportingServiceDB
{
    [RegisterType(typeof(IUnitOfWorkReportingServiceDB), DependencyLifeTime.Scoped)]
    public sealed class UnitOfWorkReportingServiceDB : UnitOfWorkBase, IUnitOfWorkReportingServiceDB
    {
        private readonly Lazy<RepositoryConfiguration> repositoryConfigurationLazy;
        private readonly Lazy<DbContext> dbContextLazy;
        private readonly Lazy<ILogger> loggerLazy;
        private readonly Lazy<AppSettings> appSettings;

        public UnitOfWorkReportingServiceDB(IServiceProvider sp)
        {
            repositoryConfigurationLazy = new Lazy<RepositoryConfiguration>(() =>
            {
                return sp.GetRequiredService<RepositoryConfiguration>();
            });

            dbContextLazy = new Lazy<DbContext>(() =>
            {
                return sp.GetRequiredService<DbContextReportingServiceDB>();
            });

            loggerLazy = new Lazy<ILogger>(() =>
            {
                return sp.GetRequiredService<ILogger<ArcadiaConstants.LoggerNames.Database>>();
            });

            appSettings = new Lazy<AppSettings>(() => {
                return sp.GetRequiredService<AppSettings>();
            });
        }

        public override RepositoryConfiguration RepositoryConfiguration => repositoryConfigurationLazy.Value;

        public override DbContext DbContext => dbContextLazy.Value;

        public override ILogger Logger => loggerLazy.Value;

        public override void Dispose()
        {
            if (dbContextLazy.IsValueCreated)
            {
                dbContextLazy.Value.Dispose();
            }
        }

        protected override IDbConnection InitializeDbConnection()
        {
            string connectionString = appSettings.Value.ConnectionStrings.DefaultConnection;
            var sqlConnection = new NpgsqlConnection(connectionString);
            return sqlConnection;
        }
    }
}
