using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileReferralProductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repair known production drift even when __EFMigrationsHistory says the
            // original migrations were applied. Every statement is intentionally
            // idempotent so this migration is safe for both dev and production.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Referral', N'U') IS NULL
                    THROW 51000, 'Required table dbo.Referral is missing. Restore the baseline schema before deploying.', 1;

                IF COL_LENGTH(N'dbo.Referral', N'HowHear') IS NULL
                BEGIN
                    ALTER TABLE dbo.Referral
                        ADD HowHear nvarchar(max) NOT NULL
                            CONSTRAINT DF_Referral_HowHear_Reconcile DEFAULT N'';
                    ALTER TABLE dbo.Referral
                        DROP CONSTRAINT DF_Referral_HowHear_Reconcile;
                END;

                IF COL_LENGTH(N'dbo.Referral', N'ReferrerEmployeeId') IS NULL
                    ALTER TABLE dbo.Referral ADD ReferrerEmployeeId nvarchar(64) NULL;

                IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
                    THROW 51000, 'Required table dbo.AspNetUsers is missing. Restore the baseline schema before deploying.', 1;

                IF COL_LENGTH(N'dbo.AspNetUsers', N'EmployeeId') IS NULL
                    ALTER TABLE dbo.AspNetUsers ADD EmployeeId nvarchar(64) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration repairs pre-existing drift. Rolling it back must not
            // remove columns that may have been created by earlier migrations.
        }
    }
}
