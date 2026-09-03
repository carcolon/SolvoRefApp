using Microsoft.Data.SqlClient;
using System.Net;
using Core.Contracts.DataSourcing;
using Core.Models.DataSourcing;
using Core.Models.Global;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Core.Service.DataSourcing
{
    public class DataSourcingService : IDataSourcingService
    {

        private readonly string _dataSourcingLeadsConnectionString;
        private readonly ILogger<DataSourcingService> _logger;

        public DataSourcingService(IConfiguration configuration, ILogger<DataSourcingService> logger)
        {
            _dataSourcingLeadsConnectionString = configuration.GetConnectionString("DataSourcingLeadsConnectionString") ?? "";
            _logger = logger;
        }

        public async Task<Response<bool>> Create(DataSourcingTable data)
        {

            using var sqlConnection = new SqlConnection(_dataSourcingLeadsConnectionString);
            Response<bool> response = new();
            try
            {
                await sqlConnection.OpenAsync();
                var columnMetadata = await GetLeadsColumnMetadataAsync(sqlConnection);
                var columnValueMap = BuildColumnValueMap(data);
                var availableColumns = columnValueMap.Keys
                    .Where(columnMetadata.ContainsKey)
                    .ToList();

                if (availableColumns.Count == 0)
                {
                    response.Success = false;
                    response.Errors = ["Lead table schema is not available."];
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }

                string columnList = string.Join(",", availableColumns);
                string parameterList = string.Join(",", availableColumns.Select(GetParameterName));
                string sqlQuery = $"INSERT INTO Leads ({columnList}) VALUES ({parameterList})";

                using (SqlCommand command = new SqlCommand(sqlQuery, sqlConnection))
                {
                    foreach (var columnName in availableColumns)
                    {
                        var metadata = columnMetadata[columnName];
                        var value = NormalizeValue(columnValueMap[columnName], metadata.MaxLength);
                        command.Parameters.AddWithValue(GetParameterName(columnName), value);
                    }

                    int rows = await command.ExecuteNonQueryAsync();
                    response.Data = rows == 1;
                }
                response.Success = true;
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to write lead data into DataSourcing.");
                await sqlConnection.CloseAsync();
                response.Success = false;
                response.Errors = ["An unexpected error occurred while writing lead data."];
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<object>> GetLeadDiagnostics(string email, string? source)
        {
            using var sqlConnection = new SqlConnection(_dataSourcingLeadsConnectionString);

            try
            {
                await sqlConnection.OpenAsync();
                var columnMetadata = await GetLeadsColumnMetadataAsync(sqlConnection);
                if (columnMetadata.Count == 0)
                {
                    return Response<object>.ErrorResponse(
                        ["Lead table schema is not available."],
                        HttpStatusCode.InternalServerError);
                }

                var diagnosticColumns = new[]
                {
                    "Id",
                    "Full_name",
                    "Email",
                    "PhoneNumber",
                    "Country",
                    "City",
                    "Fecha",
                    "apply_area",
                    "Company",
                    "Position",
                    "VacancyId",
                    "JPC",
                    "CompanyId",
                    "ReferrerEmployeeId",
                    "ReferrerId",
                    "SolvoPartner",
                    "ReferrerSolvoPartnerStatus",
                    "ReferralFromSolvoPartner",
                    "IsSolvoPartnerReferral",
                    "Cuenta_Referidos",
                    "Source",
                    "Comments"
                };
                var availableDiagnosticColumns = diagnosticColumns
                    .Where(columnMetadata.ContainsKey)
                    .ToList();

                if (!columnMetadata.ContainsKey("Email"))
                {
                    return Response<object>.ErrorResponse(
                        ["Lead table does not contain Email column for diagnostics."],
                        HttpStatusCode.InternalServerError);
                }

                var columnList = string.Join(", ", availableDiagnosticColumns.Select(column => $"[{column}]"));
                var orderBy = columnMetadata.ContainsKey("Id")
                    ? "ORDER BY [Id] DESC"
                    : string.Empty;
                var sqlQuery = $"""
                    SELECT TOP 10 {columnList}
                    FROM Leads
                    WHERE LOWER(LTRIM(RTRIM([Email]))) = @email
                    {orderBy};
                    """;

                var rows = new List<Dictionary<string, object?>>();
                using (var command = new SqlCommand(sqlQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        foreach (var columnName in availableDiagnosticColumns)
                        {
                            var value = reader[columnName];
                            row[columnName] = value == DBNull.Value ? null : value;
                        }

                        rows.Add(row);
                    }
                }

                return Response<object>.SuccessResponse(new
                {
                    Search = new
                    {
                        Email = email,
                        Source = source
                    },
                    Columns = new
                    {
                        VacancyIdExists = columnMetadata.ContainsKey("VacancyId"),
                        JpcExists = columnMetadata.ContainsKey("JPC"),
                        PositionExists = columnMetadata.ContainsKey("Position"),
                        CompanyExists = columnMetadata.ContainsKey("Company"),
                        SourceExists = columnMetadata.ContainsKey("Source"),
                        AvailableDiagnosticColumns = availableDiagnosticColumns
                    },
                    RowCount = rows.Count,
                    Rows = rows
                }, HttpStatusCode.OK);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to retrieve DataSourcing lead diagnostics.");
                return Response<object>.ErrorResponse(
                    ["An unexpected error occurred while retrieving DataSourcing lead diagnostics."],
                    HttpStatusCode.InternalServerError);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        private static Dictionary<string, object> BuildColumnValueMap(DataSourcingTable data)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Full_name"] = data.FullName,
                ["Email"] = data.Email,
                ["PhoneNumber"] = data.PhoneNumber,
                ["apply_area"] = data.ApplyArea,
                ["DNI"] = data.DNI,
                ["Experience"] = data.Experience,
                ["English_level"] = data.EnglishLevel,
                ["Country"] = data.Country,
                ["City"] = data.City ?? string.Empty,
                ["Fecha"] = data.Fecha,
                ["Source"] = data.Source,
                ["Comments"] = data.Comments,
                ["adset_name"] = data.AdSetName,
                ["Company"] = data.Company,
                ["Position"] = data.Position,
                ["VacancyId"] = data.VacancyId,
                ["JPC"] = data.ExternalVacancyId,
                ["CompanyId"] = data.CompanyId,
                ["Api_Key"] = data.Api_key,
                ["ReferrerEmployeeId"] = data.ReferrerEmployeeId,
                ["ReferrerId"] = data.ReferrerEmployeeId,
                ["SolvoPartner"] = data.ReferrerSolvoPartnerStatus,
                ["ReferrerSolvoPartnerStatus"] = data.ReferrerSolvoPartnerStatus,
                ["ReferralFromSolvoPartner"] = data.ReferralFromSolvoPartner,
                ["IsSolvoPartnerReferral"] = data.ReferralFromSolvoPartner,
            };

            if (IsSolvoPartnerReferral(data))
            {
                values["Cuenta_Referidos"] = "Solvo Partners ";
            }

            return values;
        }

        private static bool IsSolvoPartnerReferral(DataSourcingTable data)
        {
            return string.Equals(data.ReferrerSolvoPartnerStatus, "Active", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(data.ReferralFromSolvoPartner, "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetParameterName(string columnName)
        {
            return $"@p_{columnName.Replace(" ", "_").Replace("-", "_")}";
        }

        private static object NormalizeValue(object value, int? maxLength)
        {
            if (value is not string stringValue)
            {
                return value;
            }

            if (maxLength.HasValue && maxLength.Value > 0 && stringValue.Length > maxLength.Value)
            {
                return stringValue[..maxLength.Value];
            }

            return stringValue;
        }

        private static async Task<Dictionary<string, LeadColumnMetadata>> GetLeadsColumnMetadataAsync(SqlConnection sqlConnection)
        {
            const string schemaQuery = """
                SELECT COLUMN_NAME, CHARACTER_MAXIMUM_LENGTH
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Leads';
                """;

            var metadata = new Dictionary<string, LeadColumnMetadata>(StringComparer.OrdinalIgnoreCase);

            using var command = new SqlCommand(schemaQuery, sqlConnection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                int? maxLength = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                metadata[columnName] = new LeadColumnMetadata(maxLength);
            }

            return metadata;
        }

        private sealed record LeadColumnMetadata(int? MaxLength);
    }
}
