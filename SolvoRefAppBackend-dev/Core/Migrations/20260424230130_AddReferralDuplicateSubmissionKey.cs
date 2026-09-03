using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralDuplicateSubmissionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralSubmissionKey",
                table: "Referral",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                ;WITH Deduped AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY
                                LOWER(LTRIM(RTRIM(ISNULL(ReferrerID, N'')))),
                                LOWER(LTRIM(RTRIM(ISNULL(ReferralID, N'')))),
                                LOWER(LTRIM(RTRIM(ISNULL(Email, N''))))
                            ORDER BY Id
                        ) AS RowNumber
                    FROM dbo.Referral
                )
                DELETE FROM Deduped
                WHERE RowNumber > 1;
                """);

            migrationBuilder.Sql("""
                UPDATE dbo.Referral
                SET ReferralSubmissionKey = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(
                    LOWER(LTRIM(RTRIM(ISNULL(ReferrerID, N'')))), N'|',
                    LOWER(LTRIM(RTRIM(ISNULL(ReferralID, N'')))), N'|',
                    LOWER(LTRIM(RTRIM(ISNULL(Email, N''))))
                )), 2));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ReferralSubmissionKey",
                table: "Referral",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referral_ReferralSubmissionKey",
                table: "Referral",
                column: "ReferralSubmissionKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Referral_ReferralSubmissionKey",
                table: "Referral");

            migrationBuilder.DropColumn(
                name: "ReferralSubmissionKey",
                table: "Referral");
        }
    }
}
