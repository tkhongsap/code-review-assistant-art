using Argento.ReportingService.Repository.Model;
using Argento.ReportingService.Repository.Model.ModelBase;
using Argento.ReportingService.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Argento.ReportingService.Repository.ReportingServiceDB
{
    public sealed class DbContextReportingServiceDB : DbContext
    {

        private ILogger logger;

        //public DbSet<AuditLogEntity> AuditLogs { get; set; }

        public DbContextReportingServiceDB(DbContextOptions<DbContextReportingServiceDB> options, ILogger<ArcadiaConstants.LoggerNames.Database> logger)
            : base(options)
        {
            this.logger = logger;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            const string schemaName = "dbo";

            const string reconcileProcessTableName = "ReconcileProcess";
            var reconcileProcessEntity = modelBuilder.Entity<ReconcileProcessEntity>();
            reconcileProcessEntity.ToTable(reconcileProcessTableName, schemaName);
            reconcileProcessEntity.HasKey(x => x.Id).HasName($"PK_{reconcileProcessTableName}_{nameof(ReconcileProcessEntity.Id)}");
            reconcileProcessEntity.HasIndex($"{nameof(ReconcileProcessEntity.Id)}", $"{nameof(ReconcileProcessEntity.IsDeleted)}");
            reconcileProcessEntity.HasIndex($"{nameof(ReconcileProcessEntity.ReportTypeId)}", $"{nameof(ReconcileProcessEntity.IsDeleted)}");
            reconcileProcessEntity.HasIndex($"{nameof(ReconcileProcessEntity.Issuer)}", $"{nameof(ReconcileProcessEntity.IsDeleted)}");


            const string reconcileProcessDetailsTableName = "ReconcileProcessDetails";
            var reconcileProcessDetailsEntity = modelBuilder.Entity<ReconcileProcessDetailsEntity>();
            reconcileProcessDetailsEntity.ToTable(reconcileProcessDetailsTableName, schemaName);
            reconcileProcessDetailsEntity.HasKey(x => x.Id).HasName($"PK_{reconcileProcessDetailsTableName}_{nameof(ReconcileProcessDetailsEntity.Id)}");
            reconcileProcessDetailsEntity.HasIndex($"{nameof(ReconcileProcessDetailsEntity.Id)}", $"{nameof(ReconcileProcessDetailsEntity.IsDeleted)}");
            reconcileProcessDetailsEntity.HasIndex($"{nameof(ReconcileProcessDetailsEntity.ReconcileProcessId)}", $"{nameof(ReconcileProcessDetailsEntity.IsDeleted)}");


            const string reportTypesTableName = "ReportTypes";
            var reportTypesEntity = modelBuilder.Entity<ReportTypesEntity>();
            reportTypesEntity.ToTable(reportTypesTableName, schemaName);
            reportTypesEntity.HasKey(x => x.Id).HasName($"PK_{reportTypesTableName}_{nameof(ReportTypesEntity.Id)}");
            reportTypesEntity.HasIndex($"{nameof(ReportTypesEntity.Id)}", $"{nameof(ReportTypesEntity.IsDeleted)}");
            reportTypesEntity.HasIndex($"{nameof(ReportTypesEntity.Name)}", $"{nameof(ReportTypesEntity.IsDeleted)}");


            const string settlementReportDetailsTableName = "SettlementReportDetails";
            var settlementReportDetailsEntity = modelBuilder.Entity<SettlementReportDetailsEntity>();
            settlementReportDetailsEntity.ToTable(settlementReportDetailsTableName, schemaName);
            settlementReportDetailsEntity.HasKey(x => x.Id).HasName($"PK_{settlementReportDetailsTableName}_{nameof(SettlementReportDetailsEntity.Id)}");
            settlementReportDetailsEntity.HasIndex($"{nameof(SettlementReportDetailsEntity.Id)}", $"{nameof(SettlementReportDetailsEntity.IsDeleted)}");
            settlementReportDetailsEntity.HasIndex($"{nameof(SettlementReportDetailsEntity.ReconcileProcessId)}", $"{nameof(SettlementReportDetailsEntity.IsDeleted)}");

            const string auditLogTableName = "AuditLogs";
            var auditLogEntity = modelBuilder.Entity<AuditLogEntity>().ToTable(auditLogTableName, schemaName);
            auditLogEntity.HasKey(x => x.Id).HasName($"PK_{auditLogTableName}_{nameof(AuditLogEntity.Id)}");
            auditLogEntity.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_IsDeleted");

            const string bankTableName = "Bank";
            var bankEntity = modelBuilder.Entity<BankEntity>();
            bankEntity.ToTable(bankTableName, schemaName);
            bankEntity.HasKey(x => x.Id).HasName($"PK_{bankTableName}_{nameof(BankEntity.Id)}");
            bankEntity.HasIndex($"{nameof(BankEntity.BankCode)}", $"{nameof(BankEntity.IsDeleted)}");

            const string userTableName = "Users";
            var userEntity = modelBuilder.Entity<UserEntity>();
            userEntity.ToTable(userTableName, schemaName);
            userEntity.HasKey(x => x.Id).HasName($"PK_{userTableName}_{nameof(UserEntity.Id)}");
            userEntity.HasIndex($"{nameof(UserEntity.UserName)}", $"{nameof(UserEntity.IsDeleted)}");

            const string transactionTableName = "Transaction";
            var transactionTableEntity = modelBuilder.Entity<TransactionEntity>();
            transactionTableEntity.ToTable(transactionTableName, schemaName);
            transactionTableEntity.HasKey(x => x.Id).HasName($"PK_{transactionTableName}_{nameof(TransactionEntity.Id)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.Id)}", $"{nameof(TransactionEntity.IsDeleted)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.MerchantId)}", $"{nameof(TransactionEntity.TransactionNo)}", $"{nameof(TransactionEntity.IsDeleted)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.TransactionNo)}", $"{nameof(TransactionEntity.IsDeleted)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.MerchantId)}", $"{nameof(TransactionEntity.IsDeleted)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.MainBranchId)}", $"{nameof(TransactionEntity.IsDeleted)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.ChargeId)}", $"{nameof(TransactionEntity.TransactionStatusId)}", $"{nameof(TransactionEntity.IsDeleted)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.ChargeId)}", $"{nameof(TransactionEntity.IsDeleted)}");
            transactionTableEntity.HasIndex($"{nameof(TransactionEntity.OrderId)}", $"{nameof(TransactionEntity.IsDeleted)}");

            const string merchantTableName = "Merchant";
            var merchantEntity = modelBuilder.Entity<MerchantEntity>().ToTable(merchantTableName, schemaName);
            merchantEntity.HasKey(x => x.Id).HasName($"PK_{merchantTableName}_{nameof(MerchantEntity.Id)}");
            merchantEntity.HasIndex($"{nameof(MerchantEntity.Id)}", $"{nameof(MerchantEntity.IsDeleted)}");
            merchantEntity.HasIndex($"{nameof(MerchantEntity.MainBranchId)}", $"{nameof(MerchantEntity.IsDeleted)}");
            merchantEntity.HasIndex($"{nameof(MerchantEntity.MerchantKey)}", $"{nameof(MerchantEntity.IsDeleted)}");
            merchantEntity.HasIndex($"{nameof(MerchantEntity.SecretKey)}", $"{nameof(MerchantEntity.IsDeleted)}");

            const string fundingHeadersTableName = "FundingHeaders";
            var fundingHeaderEntity = modelBuilder.Entity<FundingHeadersEntity>().ToTable(fundingHeadersTableName, schemaName);
            fundingHeaderEntity.HasKey(x => x.Id).HasName($"PK_{fundingHeadersTableName}_{nameof(FundingHeadersEntity.Id)}");
            fundingHeaderEntity.HasIndex($"{nameof(FundingHeadersEntity.Id)}", $"{nameof(FundingHeadersEntity.IsDeleted)}");

            const string fundingDetailsTableName = "FundingDetails";
            var fundingDetailEntity = modelBuilder.Entity<FundingDetailsEntity>().ToTable(fundingDetailsTableName, schemaName);
            fundingDetailEntity.HasKey(x => x.Id).HasName($"PK_{fundingDetailsTableName}_{nameof(FundingDetailsEntity.Id)}");
            fundingDetailEntity.HasIndex($"{nameof(FundingDetailsEntity.Id)}", $"{nameof(FundingDetailsEntity.IsDeleted)}");

            const string accountTableName = "Account";
            var accountEntity = modelBuilder.Entity<AccountEntity>();
            accountEntity.ToTable(accountTableName, schemaName);
            accountEntity.HasKey(x => x.Id).HasName($"PK_{accountTableName}_{nameof(AccountEntity.Id)}");
            accountEntity.HasIndex($"{nameof(AccountEntity.Id)}", $"{nameof(AccountEntity.IsDeleted)}");
            accountEntity.HasIndex($"{nameof(AccountEntity.MerchantId)}", $"{nameof(AccountEntity.IsDeleted)}");

            const string adminRoleMenusTableName = "AdminRoleMenus";
            var adminRoleMenusEntity = modelBuilder.Entity<AdminRoleMenusEntity>();
            adminRoleMenusEntity.ToTable(adminRoleMenusTableName, schemaName);
            adminRoleMenusEntity.HasKey(x => new {x.MenuId, x.RoleId}).HasName($"PK_AdminRoleMenus");

            const string adminRoleSubLevelTableName = "AdminRoleSubLevel";
            var adminRoleSubLevelEntity = modelBuilder.Entity<AdminRoleSubLevelEntity>();
            adminRoleSubLevelEntity.ToTable(adminRoleSubLevelTableName, schemaName);
            adminRoleSubLevelEntity.HasKey(x => x.Id).HasName($"PK_{adminRoleSubLevelTableName}_{nameof(AdminRoleSubLevelEntity.Id)}");

            const string adminRolesTableName = "AdminRoles";
            var adminRoleEntity = modelBuilder.Entity<AdminRolesEntity>();
            adminRoleEntity.ToTable(adminRolesTableName, schemaName);
            adminRoleEntity.HasKey(x => x.Id).HasName($"PK_{adminRoleSubLevelTableName}_{nameof(AdminRolesEntity.Id)}");

            const string adminMenuSubLevelsTableName = "AdminMenuSubLevels";
            var adminMenuSubLevelsEntity = modelBuilder.Entity<AdminMenuSubLevelsEntity>();
            adminMenuSubLevelsEntity.ToTable(adminMenuSubLevelsTableName, schemaName);
            adminMenuSubLevelsEntity.HasKey(x => x.Id).HasName($"PK_{adminRoleSubLevelTableName}_{nameof(AdminMenuSubLevelsEntity.Id)}");

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(new DatabaseLoggerFactory(logger));
        }

        private sealed class DatabaseLoggerFactory : ILoggerFactory
        {

            private readonly ILogger logger;

            public DatabaseLoggerFactory(ILogger logger)
            {
                this.logger = logger;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return logger;
            }

            public void AddProvider(ILoggerProvider provider)
            {
            }
        }
        public DbSet<TransactionEntity> CustomTransactionEntity { get; set; }
        public DbSet<MerchantEntity> CustomMerchantEntity { get; set; }
    }
}
