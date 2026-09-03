using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class RestoreReferralHowHearColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Referral', 'HowHear') IS NULL
                BEGIN
                    ALTER TABLE dbo.Referral ADD HowHear nvarchar(max) NOT NULL CONSTRAINT DF_Referral_HowHear DEFAULT N'';
                    ALTER TABLE dbo.Referral DROP CONSTRAINT DF_Referral_HowHear;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Referral', 'HowHear') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.Referral DROP COLUMN HowHear;
                END
                """);
        }
    }
}
