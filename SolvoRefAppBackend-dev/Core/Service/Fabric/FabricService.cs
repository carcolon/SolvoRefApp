using System.Data;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Core.Contracts.Fabric;
using Core.Feature.Referrals.UpdateReferralStatus;
using Core.Models.Fabric;
using Core.Models.Global;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Core.Service.Fabric
{
    public class FabricService : IFabricService
    {
        private static readonly string[] ActiveVacancyStatuses =
        [
            "Testing",
            "testing"
        ];
        private readonly ClientSecretCredential _credential;
        private readonly TokenRequestContext _tokenRequestContext;
        private readonly string _fabricConnectionStringDwLeads;
        private readonly string _fabricConnectionStringDwPeopleHr;
        private readonly string _fabricConnectionStringDw_excel;
        private readonly ILogger<FabricService> _logger;

        public FabricService(IConfiguration configuration, ILogger<FabricService> logger)
        {
            _credential = new ClientSecretCredential(configuration["AzureDatalakeData:tenantId"], configuration["AzureDatalakeData:clientId"], configuration["AzureDatalakeData:clientSecret"]);
            _tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            _fabricConnectionStringDwLeads = configuration.GetConnectionString("FabricConnectionStringDwLeads") ?? "";
            _fabricConnectionStringDwPeopleHr = configuration.GetConnectionString("FabricConnectionStringDwPeopleHr") ?? "";
            _fabricConnectionStringDw_excel = configuration.GetConnectionString("FabricConnectionStringDw_excel") ?? "";
            _logger = logger;
        }

        public async Task<Response<List<PaymentSchedule>>> GetAllPaymentSchedule()
        {
            Response<List<PaymentSchedule>> response = new();
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDw_excel) { AccessToken = token.Token };
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                List<PaymentSchedule> data = new();
                string sqlQuery = @"
                    SELECT  
                    *
                    FROM [gld].[ps_calendario_pagos]";
                using (SqlCommand command = new SqlCommand(sqlQuery, sqlConnection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        var hasPeriodColumn = HasColumn(reader, "period");
                        while (reader.Read())
                        {
                            data.Add(new()
                            {
                                Employer = reader.IsDBNull(reader.GetOrdinal("employer")) ? string.Empty : reader.GetString("employer"),
                                PaymentFrequency = reader.IsDBNull(reader.GetOrdinal("payment_frequency")) ? string.Empty : reader.GetString("payment_frequency"),
                                Period = hasPeriodColumn && !reader.IsDBNull(reader.GetOrdinal("period")) ? Convert.ToString(reader.GetValue(reader.GetOrdinal("period"))) ?? string.Empty : string.Empty,
                                DeadLine1 = reader.IsDBNull(reader.GetOrdinal("deadline_1")) ? null : reader.GetDateTime("deadline_1"),
                                PaymentDate1 = reader.IsDBNull(reader.GetOrdinal("payment_date_1")) ? null : reader.GetDateTime("payment_date_1"),
                                DeadLine2 = reader.IsDBNull(reader.GetOrdinal("deadline_2")) ? null : reader.GetDateTime("deadline_2"),
                                PaymentDate2 = reader.IsDBNull(reader.GetOrdinal("payment_date_2")) ? null : reader.GetDateTime("payment_date_2"),
                            });
                        }
                    }
                }
                response.Success = true;
                response.Data = data;
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (System.Exception)
            {
                await sqlConnection.CloseAsync();
                response.Success = false;
                response.Errors.Add("An unexpected error occurred while retrieving payment schedule.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<ExtraUser>> GetExtraUserInformation(string email)
        {
            Response<ExtraUser> response = new();
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwPeopleHr) { AccessToken = token.Token };
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                ExtraUser extraUser = new();
                string sqlQuery = @"
                    SELECT  
                    payroll_company, 
                    country,
                    status,
                    solvo_id,
                    personal_id,
                    payroll_frequency_classification
                    FROM [gld].[wolfpack_without_salary]
                    WHERE LOWER(corporate_email) = LOWER(@email);";
                using (SqlCommand command = new SqlCommand(sqlQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue("@email", email);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            extraUser.Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString("status");
                            extraUser.SolId = reader.IsDBNull(reader.GetOrdinal("solvo_id")) ? string.Empty : reader.GetString("solvo_id");
                            extraUser.PayrollCompany = reader.IsDBNull(reader.GetOrdinal("payroll_company")) ? string.Empty : reader.GetString("payroll_company");
                            extraUser.Country = reader.IsDBNull(reader.GetOrdinal("country")) ? string.Empty : reader.GetString("country");
                            extraUser.PersonalId = reader.IsDBNull(reader.GetOrdinal("personal_id")) ? string.Empty : reader.GetString("personal_id");
                            extraUser.PayrollFrequencyClassification = reader.IsDBNull(reader.GetOrdinal("payroll_frequency_classification")) ? string.Empty : reader.GetString("payroll_frequency_classification");
                        }
                    }
                }
                response.Success = true;
                response.Data = extraUser;
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (System.Exception)
            {
                await sqlConnection.CloseAsync();
                response.Success = false;
                response.Errors.Add("An unexpected error occurred while retrieving user information.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }

        }

        public async Task<Response<ExtraUser>> GetExtraUserInformationBySolvoId(string solvoId)
        {
            Response<ExtraUser> response = new();
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwPeopleHr) { AccessToken = token.Token };
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                ExtraUser extraUser = new();
                string sqlQuery = @"
                    SELECT TOP 1
                    payroll_company,
                    country,
                    status,
                    solvo_id,
                    personal_id,
                    corporate_email,
                    payroll_frequency_classification
                    FROM [gld].[wolfpack_without_salary]
                    WHERE LOWER(LTRIM(RTRIM(solvo_id))) = LOWER(LTRIM(RTRIM(@solvoId)));";
                using (SqlCommand command = new SqlCommand(sqlQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue("@solvoId", solvoId);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            extraUser.Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString("status");
                            extraUser.SolId = reader.IsDBNull(reader.GetOrdinal("solvo_id")) ? string.Empty : reader.GetString("solvo_id");
                            extraUser.PayrollCompany = reader.IsDBNull(reader.GetOrdinal("payroll_company")) ? string.Empty : reader.GetString("payroll_company");
                            extraUser.Country = reader.IsDBNull(reader.GetOrdinal("country")) ? string.Empty : reader.GetString("country");
                            extraUser.PersonalId = reader.IsDBNull(reader.GetOrdinal("personal_id")) ? string.Empty : reader.GetString("personal_id");
                            extraUser.Email = reader.IsDBNull(reader.GetOrdinal("corporate_email")) ? string.Empty : reader.GetString("corporate_email");
                            extraUser.PayrollFrequencyClassification = reader.IsDBNull(reader.GetOrdinal("payroll_frequency_classification")) ? string.Empty : reader.GetString("payroll_frequency_classification");
                        }
                    }
                }
                response.Success = true;
                response.Data = extraUser;
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (System.Exception)
            {
                await sqlConnection.CloseAsync();
                response.Success = false;
                response.Errors.Add("An unexpected error occurred while retrieving user information by Solvo ID.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }

        }

        public async Task<Response<List<ExtraUser>>> GetExtraUserInformation(List<string> data)
        {
            Response<List<ExtraUser>> response = new();
            List<ExtraUser> extraUsers = [];
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwPeopleHr) { AccessToken = token.Token };
            int batchSize = 500;
            var batchedIds = data.Select((id, index) => new { id, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.id).ToList()).ToList();
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                foreach (var batch in batchedIds)
                {
                    using (SqlCommand command = new SqlCommand { Connection = sqlConnection })
                    {
                        var sqlQuery = @"
                    SELECT 
                    solvo_id, 
                    payroll_company, 
                    country,
                    status,
                    personal_id,
                    corporate_email,
                    payroll_frequency_classification
                    FROM [gld].[wolfpack_without_salary]
                    WHERE corporate_email COLLATE Latin1_General_CI_AS IN (
                        SELECT [value] FROM OPENJSON(@emailsJson)
                    );";
                        command.CommandText = sqlQuery;
                        command.Parameters.Add("@emailsJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(batch);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                extraUsers.Add(new()
                                {
                                    SolId = reader.IsDBNull(reader.GetOrdinal("solvo_id")) ? string.Empty : reader.GetString("solvo_id"),
                                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString("status"),
                                    PayrollCompany = reader.IsDBNull(reader.GetOrdinal("payroll_company")) ? string.Empty : reader.GetString("payroll_company"),
                                    Country = reader.IsDBNull(reader.GetOrdinal("country")) ? string.Empty : reader.GetString("country"),
                                    PersonalId = reader.IsDBNull(reader.GetOrdinal("personal_id")) ? string.Empty : reader.GetString("personal_id"),
                                    PayrollFrequencyClassification = reader.IsDBNull(reader.GetOrdinal("payroll_frequency_classification")) ? string.Empty : reader.GetString("payroll_frequency_classification"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("corporate_email")) ? string.Empty : reader.GetString("corporate_email")
                                });
                            }
                        }
                    }
                }
                response.Success = true;
                response.Data = extraUsers;
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (System.Exception)
            {
                await sqlConnection.CloseAsync();
                response.Success = false;
                response.Errors.Add("An unexpected error occurred while retrieving user information.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }

        }

        public async Task<Response<List<ExtraUser>>> GetExtraUserInformationByPersonalId(List<string> personalIds)
        {
            Response<List<ExtraUser>> response = new();
            List<ExtraUser> extraUsers = [];
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDw_excel) { AccessToken = token.Token };
            int batchSize = 500;
            var batchedIds = personalIds.Select((id, index) => new { id, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.id).ToList()).ToList();
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                foreach (var batch in batchedIds)
                {
                    using (SqlCommand command = new SqlCommand { Connection = sqlConnection })
                    {
                        var sqlQuery = @"
                    SELECT 
                    start_date,
                    status,
                    personal_id
                    FROM [gld].[wolfpack]
                    WHERE personal_id IN (
                        SELECT [value] FROM OPENJSON(@personalIdsJson)
                    );";
                        command.CommandText = sqlQuery;
                        command.Parameters.Add("@personalIdsJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(batch);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                extraUsers.Add(new()
                                {
                                    StartDate = reader.IsDBNull(reader.GetOrdinal("start_date")) ? DateTime.MinValue : reader.GetDateTime("start_date"),
                                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? string.Empty : reader.GetString("status"),
                                    PersonalId = reader.IsDBNull(reader.GetOrdinal("personal_id")) ? string.Empty : reader.GetString("personal_id")
                                });
                            }
                        }
                    }
                }
                response.Success = true;
                response.Data = extraUsers;
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (System.Exception)
            {
                await sqlConnection.CloseAsync();
                response.Success = false;
                response.Errors.Add("An unexpected error occurred while retrieving user information.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<List<ExtraUser>>> GetActiveEmployeesByPersonalId(List<string> personalIds)
        {
            return await GetEmployeesByPersonalId(personalIds, activeOnly: true);
        }

        public async Task<Response<List<ExtraUser>>> GetEmployeesByPersonalId(List<string> personalIds)
        {
            return await GetEmployeesByPersonalId(personalIds, activeOnly: false);
        }

        private async Task<Response<List<ExtraUser>>> GetEmployeesByPersonalId(List<string> personalIds, bool activeOnly)
        {
            Response<List<ExtraUser>> response = new();
            List<ExtraUser> employees = [];
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwPeopleHr) { AccessToken = token.Token };
            int batchSize = 500;
            var normalizedIds = personalIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var batchedIds = normalizedIds.Select((id, index) => new { id, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.id).ToList()).ToList();

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                var columns = await GetTableColumns(sqlConnection, "gld", "wolfpack_without_salary");
                var personalIdColumn = GetExistingColumns(columns, GetEmployeePersonalIdColumnCandidates()).FirstOrDefault();
                var startDateColumn = GetExistingColumns(columns, GetEmployeeStartDateColumnCandidates()).FirstOrDefault();
                var statusColumn = GetExistingColumns(columns, GetEmployeeStatusColumnCandidates()).FirstOrDefault();
                var partitionDateColumn = GetExistingColumns(columns, "partition_date", "save_date", "updated_date").FirstOrDefault();

                if (string.IsNullOrWhiteSpace(personalIdColumn) ||
                    string.IsNullOrWhiteSpace(startDateColumn) ||
                    string.IsNullOrWhiteSpace(statusColumn))
                {
                    response.Success = false;
                    response.Errors = ["The wolfpack_without_salary table does not expose the expected personal_id, start_date or status columns."];
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                foreach (var batch in batchedIds)
                {
                    using SqlCommand command = new SqlCommand { Connection = sqlConnection };
                    var quotedPersonalIdColumn = QuoteSqlIdentifier(personalIdColumn);
                    var quotedStartDateColumn = QuoteSqlIdentifier(startDateColumn);
                    var quotedStatusColumn = QuoteSqlIdentifier(statusColumn);
                    var partitionDateSelect = string.IsNullOrWhiteSpace(partitionDateColumn)
                        ? "CAST(NULL AS datetime2) AS PartitionDate"
                        : $"TRY_CAST({QuoteSqlIdentifier(partitionDateColumn)} AS datetime2) AS PartitionDate";
                    var partitionDateOrder = string.IsNullOrWhiteSpace(partitionDateColumn)
                        ? "StartDate DESC"
                        : "PartitionDate DESC";
                    command.CommandText = $@"
WITH EmployeeRows AS (
SELECT
    CAST({quotedPersonalIdColumn} AS nvarchar(4000)) AS PersonalId,
    TRY_CAST({quotedStartDateColumn} AS datetime2) AS StartDate,
    CAST({quotedStatusColumn} AS nvarchar(4000)) AS Status,
    {partitionDateSelect}
FROM gld.wolfpack_without_salary
WHERE LTRIM(RTRIM(CAST({quotedPersonalIdColumn} AS nvarchar(4000)))) IN (
    SELECT [value] FROM OPENJSON(@personalIdsJson)
)
{(activeOnly ? $"AND LOWER(LTRIM(RTRIM(CAST({quotedStatusColumn} AS nvarchar(4000))))) = 'active'" : string.Empty)}
AND TRY_CAST({quotedStartDateColumn} AS datetime2) IS NOT NULL
)
SELECT PersonalId, StartDate, Status
FROM (
    SELECT
        PersonalId,
        StartDate,
        Status,
        ROW_NUMBER() OVER (PARTITION BY PersonalId, StartDate ORDER BY {partitionDateOrder}) AS RowNumber
    FROM EmployeeRows
) ranked
WHERE RowNumber = 1
ORDER BY PersonalId, StartDate;";
                    command.Parameters.Add("@personalIdsJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(batch);

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        employees.Add(new ExtraUser
                        {
                            PersonalId = reader.IsDBNull(reader.GetOrdinal("PersonalId")) ? string.Empty : reader.GetString(reader.GetOrdinal("PersonalId")),
                            StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("StartDate")),
                            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? string.Empty : reader.GetString(reader.GetOrdinal("Status"))
                        });
                    }
                }

                response.Success = true;
                response.Data = employees;
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception)
            {
                response.Success = false;
                response.Errors = ["An unexpected error occurred while retrieving active employee information."];
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<object>> GetActiveEmployeeDiagnostics(string personalId)
        {
            var response = new Response<object>();
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwPeopleHr) { AccessToken = token.Token };

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    await sqlConnection.OpenAsync();
                }

                var columns = await GetTableColumns(sqlConnection, "slv", "st_empleados_unificado");
                var personalIdColumns = GetExistingColumns(columns, GetEmployeePersonalIdColumnCandidates());
                var startDateColumn = GetExistingColumns(columns, GetEmployeeStartDateColumnCandidates()).FirstOrDefault();
                var statusColumn = GetExistingColumns(columns, GetEmployeeStatusColumnCandidates()).FirstOrDefault();
                var textColumns = await GetTextTableColumns(sqlConnection, "slv", "st_empleados_unificado");
                var searchedColumns = textColumns
                    .Where(column =>
                        column.Contains("id", StringComparison.OrdinalIgnoreCase) ||
                        column.Contains("dni", StringComparison.OrdinalIgnoreCase) ||
                        column.Contains("document", StringComparison.OrdinalIgnoreCase) ||
                        column.Contains("ident", StringComparison.OrdinalIgnoreCase) ||
                        column.Contains("cedula", StringComparison.OrdinalIgnoreCase) ||
                        column.Contains("personal", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var knownColumnRows = await GetTableDiagnosticRows(
                    sqlConnection,
                    "slv",
                    "st_empleados_unificado",
                    personalIdColumns,
                    personalId);
                var textColumnRows = await GetTableDiagnosticRows(
                    sqlConnection,
                    "slv",
                    "st_empleados_unificado",
                    searchedColumns,
                    personalId);

                response.Success = true;
                response.Data = new
                {
                    AvailableColumns = columns.OrderBy(column => column, StringComparer.OrdinalIgnoreCase).ToList(),
                    DetectedColumns = new
                    {
                        PersonalIdCandidates = personalIdColumns,
                        StartDate = startDateColumn,
                        Status = statusColumn
                    },
                    KnownPersonalIdColumnSearch = new
                    {
                        Value = personalId,
                        SearchedColumns = personalIdColumns,
                        RowCount = knownColumnRows.Count,
                        Rows = knownColumnRows
                    },
                    AnyTextColumnPersonalIdSearch = new
                    {
                        Value = personalId,
                        SearchedColumns = searchedColumns,
                        RowCount = textColumnRows.Count,
                        Rows = textColumnRows
                    }
                };
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception)
            {
                response.Success = false;
                response.Errors = ["An unexpected error occurred while retrieving active employee diagnostics."];
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<object>> GetPeopleHrDiagnostics(string personalId)
        {
            var response = new Response<object>();
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwPeopleHr) { AccessToken = token.Token };

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    await sqlConnection.OpenAsync();
                }

                var databaseName = await GetCurrentDatabaseName(sqlConnection);
                var visibleTables = await GetVisibleTables(sqlConnection);
                var candidateTables = visibleTables
                    .Where(table => IsPeopleHrCandidateTable(table.Schema, table.Name))
                    .Take(30)
                    .ToList();

                var inspectedTables = new List<object>();
                foreach (var table in candidateTables)
                {
                    var columns = await GetTableColumns(sqlConnection, table.Schema, table.Name);
                    var personalIdColumns = GetExistingColumns(columns, GetEmployeePersonalIdColumnCandidates());
                    var startDateColumn = GetExistingColumns(columns, GetEmployeeStartDateColumnCandidates()).FirstOrDefault();
                    var statusColumn = GetExistingColumns(columns, GetEmployeeStatusColumnCandidates()).FirstOrDefault();
                    var textColumns = await GetTextTableColumns(sqlConnection, table.Schema, table.Name);
                    var searchedColumns = personalIdColumns.Count > 0
                        ? personalIdColumns
                        : GetLikelyPersonalIdTextColumns(textColumns);
                    var rows = await GetTableDiagnosticRows(sqlConnection, table.Schema, table.Name, searchedColumns, personalId);

                    inspectedTables.Add(new
                    {
                        table.Schema,
                        Table = table.Name,
                        AvailableColumns = columns.OrderBy(column => column, StringComparer.OrdinalIgnoreCase).ToList(),
                        DetectedColumns = new
                        {
                            PersonalIdCandidates = personalIdColumns,
                            StartDate = startDateColumn,
                            Status = statusColumn
                        },
                        Search = new
                        {
                            Value = personalId,
                            SearchedColumns = searchedColumns,
                            RowCount = rows.Count,
                            Rows = rows
                        }
                    });
                }

                response.Success = true;
                response.Data = new
                {
                    Connection = new
                    {
                        Open = sqlConnection.State == ConnectionState.Open,
                        Database = databaseName
                    },
                    ExpectedHeadcountTable = new
                    {
                        Schema = "slv",
                        Table = "st_empleados_unificado",
                        Exists = visibleTables.Any(table =>
                            table.Schema.Equals("slv", StringComparison.OrdinalIgnoreCase) &&
                            table.Name.Equals("st_empleados_unificado", StringComparison.OrdinalIgnoreCase))
                    },
                    VisibleTableCount = visibleTables.Count,
                    VisibleTables = visibleTables
                        .Take(200)
                        .Select(table => new { table.Schema, Table = table.Name })
                        .ToList(),
                    CandidateTableCount = candidateTables.Count,
                    CandidateTables = inspectedTables
                };
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception)
            {
                response.Success = false;
                response.Errors = ["An unexpected error occurred while retrieving PeopleHR diagnostics."];
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<object>> GetPeopleHrTableDiagnostics(string schema, string table, string? personalId)
        {
            var response = new Response<object>();
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwPeopleHr) { AccessToken = token.Token };
            var normalizedSchema = (schema ?? string.Empty).Trim();
            var normalizedTable = (table ?? string.Empty).Trim();
            var normalizedPersonalId = (personalId ?? string.Empty).Trim();

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    await sqlConnection.OpenAsync();
                }

                var databaseName = await GetCurrentDatabaseName(sqlConnection);
                var visibleTables = await GetVisibleTables(sqlConnection);
                var exists = visibleTables.Any(item =>
                    item.Schema.Equals(normalizedSchema, StringComparison.OrdinalIgnoreCase) &&
                    item.Name.Equals(normalizedTable, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    response.Success = true;
                    response.Data = new
                    {
                        Connection = new
                        {
                            Open = sqlConnection.State == ConnectionState.Open,
                            Database = databaseName
                        },
                        RequestedTable = new
                        {
                            Schema = normalizedSchema,
                            Table = normalizedTable,
                            Exists = false
                        },
                        VisibleTableCount = visibleTables.Count,
                        SimilarTables = visibleTables
                            .Where(item =>
                                item.Schema.Contains(normalizedSchema, StringComparison.OrdinalIgnoreCase) ||
                                item.Name.Contains(normalizedTable, StringComparison.OrdinalIgnoreCase) ||
                                normalizedTable.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
                            .Take(50)
                            .Select(item => new { item.Schema, Table = item.Name })
                            .ToList()
                    };
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }

                var columns = await GetTableColumns(sqlConnection, normalizedSchema, normalizedTable);
                var textColumns = await GetTextTableColumns(sqlConnection, normalizedSchema, normalizedTable);
                var personalIdColumns = GetExistingColumns(columns, GetEmployeePersonalIdColumnCandidates());
                var startDateColumn = GetExistingColumns(columns, GetEmployeeStartDateColumnCandidates()).FirstOrDefault();
                var statusColumn = GetExistingColumns(columns, GetEmployeeStatusColumnCandidates()).FirstOrDefault();
                var searchedColumns = personalIdColumns.Count > 0
                    ? personalIdColumns
                    : GetLikelyPersonalIdTextColumns(textColumns);
                var matchingRows = string.IsNullOrWhiteSpace(normalizedPersonalId)
                    ? []
                    : await GetTableDiagnosticRows(sqlConnection, normalizedSchema, normalizedTable, searchedColumns, normalizedPersonalId);
                var sampleRows = await GetTableSampleRows(sqlConnection, normalizedSchema, normalizedTable, 5);
                var rowCount = await GetTableRowCount(sqlConnection, normalizedSchema, normalizedTable);

                response.Success = true;
                response.Data = new
                {
                    Connection = new
                    {
                        Open = sqlConnection.State == ConnectionState.Open,
                        Database = databaseName
                    },
                    RequestedTable = new
                    {
                        Schema = normalizedSchema,
                        Table = normalizedTable,
                        Exists = true
                    },
                    RowCount = rowCount,
                    AvailableColumns = columns.OrderBy(column => column, StringComparer.OrdinalIgnoreCase).ToList(),
                    DetectedColumns = new
                    {
                        PersonalIdCandidates = personalIdColumns,
                        StartDate = startDateColumn,
                        Status = statusColumn
                    },
                    PersonalIdSearch = new
                    {
                        Value = normalizedPersonalId,
                        SearchedColumns = searchedColumns,
                        RowCount = matchingRows.Count,
                        Rows = matchingRows
                    },
                    Sample = new
                    {
                        RowCount = sampleRows.Count,
                        Rows = sampleRows
                    }
                };
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception)
            {
                response.Success = false;
                response.Errors = ["An unexpected error occurred while retrieving PeopleHR table diagnostics."];
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<List<UpdateReferralStatusDto>>> GetReferralStatuses(List<string> sources, List<string> emails)
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };
            List<UpdateReferralStatusDto> referralStatus = new();
            int batchSize = 500;
            var batchedIds = sources.Select((id, index) => new { id, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.id).ToList()).ToList();
            var normalizedEmails = emails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            var batchedEmails = normalizedEmails.Select((email, index) => new { email, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.email).ToList()).ToList();

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                var columns = await GetTableColumns(sqlConnection, "gld", "sc_applicants");
                var sourceColumn = GetExistingColumns(columns, "source").FirstOrDefault();
                var emailColumn = GetExistingColumns(columns, GetApplicantEmailColumnCandidates()).FirstOrDefault();
                var applicantStatusColumn = GetExistingColumns(columns, "status", "applicant_status").FirstOrDefault();
                var ownershipColumn = GetExistingColumns(columns, "ownership").FirstOrDefault();
                var statusLeadColumn = GetExistingColumns(columns, "nombre_estado_examen_ht").FirstOrDefault();
                var resumeAvailableColumn = GetExistingColumns(columns, "resume_available").FirstOrDefault();
                var huntyEnglishScoreColumn = GetExistingColumns(columns, "hunty_english_score").FirstOrDefault();

                if (string.IsNullOrWhiteSpace(sourceColumn) ||
                    string.IsNullOrWhiteSpace(resumeAvailableColumn))
                {
                    var missingColumns = new[]
                    {
                        string.IsNullOrWhiteSpace(sourceColumn) ? "source" : string.Empty,
                        string.IsNullOrWhiteSpace(resumeAvailableColumn) ? "resume_available" : string.Empty
                    }.Where(column => !string.IsNullOrWhiteSpace(column)).ToList();

                    return Response<List<UpdateReferralStatusDto>>.ErrorResponse(
                        [$"The sc_applicants table does not expose the expected required columns: {string.Join(", ", missingColumns)}."],
                        HttpStatusCode.BadRequest);
                }

                var quotedSourceColumn = QuoteSqlIdentifier(sourceColumn);
                var quotedResumeAvailableColumn = QuoteSqlIdentifier(resumeAvailableColumn);
                var applicantStatusSelect = string.IsNullOrWhiteSpace(applicantStatusColumn)
                    ? "CAST(NULL AS nvarchar(4000)) AS ApplicantStatus"
                    : $"CAST({QuoteSqlIdentifier(applicantStatusColumn)} AS nvarchar(4000)) AS ApplicantStatus";
                var ownershipSelect = string.IsNullOrWhiteSpace(ownershipColumn)
                    ? "CAST(NULL AS nvarchar(4000)) AS Ownership"
                    : $"CAST({QuoteSqlIdentifier(ownershipColumn)} AS nvarchar(4000)) AS Ownership";
                var statusLeadSelect = string.IsNullOrWhiteSpace(statusLeadColumn)
                    ? "CAST(NULL AS nvarchar(4000)) AS StatusLead"
                    : $"CAST({QuoteSqlIdentifier(statusLeadColumn)} AS nvarchar(4000)) AS StatusLead";
                var huntyEnglishScoreSelect = string.IsNullOrWhiteSpace(huntyEnglishScoreColumn)
                    ? "CAST(NULL AS nvarchar(4000)) AS HuntyEnglishScore"
                    : $"CAST({QuoteSqlIdentifier(huntyEnglishScoreColumn)} AS nvarchar(4000)) AS HuntyEnglishScore";
                var emailSelect = string.IsNullOrWhiteSpace(emailColumn)
                    ? "CAST(NULL AS nvarchar(4000)) AS Email"
                    : $"CAST({QuoteSqlIdentifier(emailColumn)} AS nvarchar(4000)) AS Email";
                var emailFilter = string.IsNullOrWhiteSpace(emailColumn)
                    ? string.Empty
                    : $@"
OR LOWER(LTRIM(RTRIM(CAST({QuoteSqlIdentifier(emailColumn)} AS nvarchar(4000))))) IN (
    SELECT [value] FROM OPENJSON(@emailsJson)
)";

                var batchCount = Math.Max(batchedIds.Count, batchedEmails.Count);
                for (var i = 0; i < batchCount; i++)
                {
                    var sourceBatch = i < batchedIds.Count ? batchedIds[i] : [];
                    var emailBatch = i < batchedEmails.Count ? batchedEmails[i] : [];
                    using (SqlCommand command = new SqlCommand { Connection = sqlConnection })
                    {
                        var sqlQuery = $@"SELECT
    CAST({quotedSourceColumn} AS nvarchar(4000)) AS Source,
    {emailSelect},
    {applicantStatusSelect},
    {ownershipSelect},
    {statusLeadSelect},
    CAST({quotedResumeAvailableColumn} AS nvarchar(4000)) AS ResumeAvailable,
    {huntyEnglishScoreSelect}
FROM gld.sc_applicants
WHERE CAST({quotedSourceColumn} AS nvarchar(4000)) IN (
    SELECT [value] FROM OPENJSON(@sourcesJson)
)
{emailFilter}";
                        command.CommandText = sqlQuery;
                        command.Parameters.Add("@sourcesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(sourceBatch);
                        command.Parameters.Add("@emailsJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(emailBatch);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            while (reader.Read())
                            {
                                referralStatus.Add(new UpdateReferralStatusDto
                                {
                                    Source = reader.IsDBNull(reader.GetOrdinal("Source")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("Source"))) ?? string.Empty,
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("Email"))) ?? string.Empty,
                                    ApplicantStatus = reader.IsDBNull(reader.GetOrdinal("ApplicantStatus")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("ApplicantStatus"))) ?? string.Empty,
                                    Ownership = reader.IsDBNull(reader.GetOrdinal("Ownership")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("Ownership"))) ?? string.Empty,
                                    StatusLead = reader.IsDBNull(reader.GetOrdinal("StatusLead")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("StatusLead"))) ?? string.Empty,
                                    ResumeAvailable = reader.IsDBNull(reader.GetOrdinal("ResumeAvailable")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("ResumeAvailable"))) ?? string.Empty,
                                    HuntyEnglishScore = reader.IsDBNull(reader.GetOrdinal("HuntyEnglishScore")) ? string.Empty : Convert.ToString(reader.GetValue(reader.GetOrdinal("HuntyEnglishScore"))) ?? string.Empty
                                });
                            }
                    }
                }
                return Response<List<UpdateReferralStatusDto>>.SuccessResponse(referralStatus, HttpStatusCode.OK);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An unexpected error occurred while retrieving referral statuses from gld.sc_applicants.");
                return Response<List<UpdateReferralStatusDto>>.ErrorResponse(
                    ["An unexpected error occurred while retrieving referral statuses."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<object>> GetApplicantStatusDiagnostics(string source, string email, bool includeSourceSearch = true)
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                var columns = await GetTableColumns(sqlConnection, "gld", "sc_applicants");
                var sourceColumn = GetExistingColumns(columns, "source").FirstOrDefault();
                var emailCandidates = GetExistingColumns(columns, GetApplicantEmailColumnCandidates());
                var applicantStatusColumn = GetExistingColumns(columns, "status", "applicant_status").FirstOrDefault();
                var ownershipColumn = GetExistingColumns(columns, "ownership").FirstOrDefault();
                var resumeAvailableColumn = GetExistingColumns(columns, "resume_available").FirstOrDefault();
                var statusLeadColumn = GetExistingColumns(columns, "nombre_estado_examen_ht").FirstOrDefault();
                var huntyEnglishScoreColumn = GetExistingColumns(columns, "hunty_english_score").FirstOrDefault();
                var textColumns = await GetTextTableColumns(sqlConnection, "gld", "sc_applicants");

                var exactEmailRows = emailCandidates.Count == 0
                    ? []
                    : await GetApplicantDiagnosticRows(sqlConnection, emailCandidates, email);
                var anyTextEmailRows = await GetApplicantDiagnosticRows(sqlConnection, textColumns, email);
                var detectedColumns = new
                {
                    Source = sourceColumn,
                    EmailCandidates = emailCandidates,
                    ApplicantStatus = applicantStatusColumn,
                    Ownership = ownershipColumn,
                    ResumeAvailable = resumeAvailableColumn,
                    StatusLead = statusLeadColumn,
                    HuntyEnglishScore = huntyEnglishScoreColumn
                };

                if (!includeSourceSearch)
                {
                    var rows = exactEmailRows.Count > 0 ? exactEmailRows : anyTextEmailRows;
                    return Response<object>.SuccessResponse(new
                    {
                        Search = new
                        {
                            Source = source,
                            Email = email
                        },
                        MatchType = exactEmailRows.Count > 0 ? "KnownEmailColumnSearch" : "AnyTextColumnEmailSearch",
                        DetectedColumns = detectedColumns,
                        RowCount = rows.Count,
                        Rows = rows
                    }, HttpStatusCode.OK);
                }

                var sourceRows = string.IsNullOrWhiteSpace(sourceColumn)
                    ? []
                    : await GetApplicantDiagnosticRows(sqlConnection, sourceColumn, source);

                return Response<object>.SuccessResponse(new
                {
                    AvailableColumns = columns.OrderBy(column => column, StringComparer.OrdinalIgnoreCase).ToList(),
                    DetectedColumns = detectedColumns,
                    SourceSearch = new
                    {
                        Value = source,
                        RowCount = sourceRows.Count,
                        Rows = sourceRows
                    },
                    KnownEmailColumnSearch = new
                    {
                        Value = email,
                        RowCount = exactEmailRows.Count,
                        Rows = exactEmailRows
                    },
                    AnyTextColumnEmailSearch = new
                    {
                        Value = email,
                        SearchedColumns = textColumns,
                        RowCount = anyTextEmailRows.Count,
                        Rows = anyTextEmailRows
                    }
                }, HttpStatusCode.OK);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An unexpected error occurred while running sc_applicants diagnostics.");
                return Response<object>.ErrorResponse(
                    ["An unexpected error occurred while running sc_applicants diagnostics."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<List<UpdateReferralPlacementDto>>> GetReferralPlacements(List<string> emails)
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };
            List<UpdateReferralPlacementDto> placements = new();
            int batchSize = 500;
            var normalizedEmails = emails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            var batchedEmails = normalizedEmails.Select((email, index) => new { email, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.email).ToList()).ToList();

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                var columns = await GetTableColumns(sqlConnection, "gld", "sc_placements");
                var emailColumn = GetExistingColumns(columns, "email", "candidate_email", "applicant_email", "personal_email", "correo").FirstOrDefault();
                var placementDateColumn = GetExistingColumns(columns, "placement_created_on").FirstOrDefault();

                if (string.IsNullOrWhiteSpace(emailColumn) || string.IsNullOrWhiteSpace(placementDateColumn))
                {
                    return Response<List<UpdateReferralPlacementDto>>.ErrorResponse(
                        ["The sc_placements table does not expose the expected email or placement_created_on columns."],
                        HttpStatusCode.BadRequest);
                }

                foreach (var batch in batchedEmails)
                {
                    using SqlCommand command = new SqlCommand { Connection = sqlConnection };
                    var quotedEmailColumn = QuoteSqlIdentifier(emailColumn);
                    var quotedPlacementDateColumn = QuoteSqlIdentifier(placementDateColumn);
                    command.CommandText = $@"
SELECT
    CAST({quotedEmailColumn} AS nvarchar(4000)) AS Email,
    TRY_CAST({quotedPlacementDateColumn} AS datetime2) AS PlacementCreatedOn
FROM gld.sc_placements
WHERE LOWER(LTRIM(RTRIM(CAST({quotedEmailColumn} AS nvarchar(4000))))) IN (
    SELECT [value] FROM OPENJSON(@emailsJson)
)
AND TRY_CAST({quotedPlacementDateColumn} AS datetime2) IS NOT NULL";
                    command.Parameters.Add("@emailsJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(batch);

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        placements.Add(new UpdateReferralPlacementDto
                        {
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                            PlacementCreatedOn = reader.IsDBNull(reader.GetOrdinal("PlacementCreatedOn")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("PlacementCreatedOn"))
                        });
                    }
                }

                return Response<List<UpdateReferralPlacementDto>>.SuccessResponse(placements, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return Response<List<UpdateReferralPlacementDto>>.ErrorResponse(
                    ["An unexpected error occurred while retrieving referral placements."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<List<string>>> GetHuntyEmails(List<string> emails)
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };
            List<string> huntyEmails = new();
            int batchSize = 500;
            var normalizedEmails = emails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            var batchedEmails = normalizedEmails.Select((email, index) => new { email, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.email).ToList()).ToList();

            try
            {
                if (normalizedEmails.Count == 0)
                {
                    return Response<List<string>>.SuccessResponse(huntyEmails, HttpStatusCode.OK);
                }

                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                var columns = await GetTableColumns(sqlConnection, "gld", "tb_hunty");
                var emailColumn = GetExistingColumns(columns, "email", "candidate_email", "applicant_email", "personal_email", "correo").FirstOrDefault();

                if (string.IsNullOrWhiteSpace(emailColumn))
                {
                    return Response<List<string>>.ErrorResponse(
                        ["The tb_hunty table does not expose the expected email column."],
                        HttpStatusCode.BadRequest);
                }

                var quotedEmailColumn = QuoteSqlIdentifier(emailColumn);
                foreach (var batch in batchedEmails)
                {
                    using SqlCommand command = new SqlCommand { Connection = sqlConnection };
                    command.CommandText = $@"
SELECT DISTINCT
    LOWER(LTRIM(RTRIM(CAST({quotedEmailColumn} AS nvarchar(4000))))) AS Email
FROM gld.tb_hunty
WHERE LOWER(LTRIM(RTRIM(CAST({quotedEmailColumn} AS nvarchar(4000))))) IN (
    SELECT [value] FROM OPENJSON(@emailsJson)
)";
                    command.Parameters.Add("@emailsJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(batch);

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        var email = reader.IsDBNull(reader.GetOrdinal("Email"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Email"));

                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            huntyEmails.Add(email.Trim().ToLowerInvariant());
                        }
                    }
                }

                return Response<List<string>>.SuccessResponse(huntyEmails.Distinct().ToList(), HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return Response<List<string>>.ErrorResponse(
                    ["An unexpected error occurred while retrieving tb_hunty emails."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<bool>> ReferredValidation(string phone, string email)
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };


            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();


                await using var command = new SqlCommand("gld.sp_referral_app_validation", sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 60
                };

                // Tipar parámetros (ajusta longitudes/tipos según el SP)
                command.Parameters.Add("@tel", SqlDbType.VarChar, 50).Value = (object?)phone ?? DBNull.Value;
                command.Parameters.Add("@email", SqlDbType.VarChar, 256).Value = (object?)email ?? DBNull.Value;


                bool validate = false;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {

                    int idxValido = reader.GetOrdinal("es_valido");
                    int esValido = reader.IsDBNull(idxValido) ? 0 : reader.GetInt32(idxValido);

                    validate = esValido == 1;

                }

                return Response<bool>.SuccessResponse(validate, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return Response<bool>.ErrorResponse(
                    ["An unexpected error occurred while validating referral data."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<List<FabricJobPosting>>> GetActiveJobPostings()
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };
            List<FabricJobPosting> vacancies = new();

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                const string sqlQuery = @"
SELECT
    CAST(job_code AS nvarchar(4000)) AS ExternalVacancyId,
    CAST(position_name AS nvarchar(4000)) AS PositionName,
    CAST(main_country AS nvarchar(4000)) AS Country
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
);";

                using SqlCommand command = new SqlCommand(sqlQuery, sqlConnection);
                command.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    vacancies.Add(new FabricJobPosting
                    {
                        ExternalVacancyId = reader.IsDBNull(reader.GetOrdinal("ExternalVacancyId"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("ExternalVacancyId")).Trim(),
                        PositionName = reader.IsDBNull(reader.GetOrdinal("PositionName"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("PositionName")).Trim(),
                        Country = reader.IsDBNull(reader.GetOrdinal("Country"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Country")).Trim()
                    });
                }

                _logger.LogInformation(
                    "Fabric active vacancy query returned {Count} row(s) from dw_leads_funnel.gld.job_posting using direct columns job_code, position_name, main_country and status {Status}.",
                    vacancies.Count,
                    string.Join(", ", ActiveVacancyStatuses));

                _logger.LogInformation(
                    "Fabric active vacancy extracted sample rows: {Sample}",
                    string.Join(" || ", vacancies
                        .Take(5)
                        .Select(item => $"code={item.ExternalVacancyId}, title={item.PositionName}, country={item.Country}")));

                try
                {
                    await LogRawJobPostingSamples(sqlConnection);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(
                        logEx,
                        "Skipped non-critical Fabric raw job posting sample logging after successful active vacancy query.");
                }

                return Response<List<FabricJobPosting>>.SuccessResponse(vacancies, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active job postings from Fabric.");
                return Response<List<FabricJobPosting>>.ErrorResponse(
                    ["An unexpected error occurred while retrieving job postings from Fabric."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<string>> ExportActiveJobPostingSchemaProfileCsv()
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                var columnMetadata = await GetJobPostingColumnMetadata(sqlConnection);
                var profiles = columnMetadata.ToDictionary(
                    item => item.Name,
                    item => new JobPostingColumnProfile(item.Name, item.DataType),
                    StringComparer.OrdinalIgnoreCase);

                const string sampleSql = @"
SELECT TOP (200) *
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
);";

                using var command = new SqlCommand(sampleSql, sqlConnection);
                command.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        var columnName = reader.GetName(index);
                        if (!profiles.TryGetValue(columnName, out var profile))
                        {
                            profile = new JobPostingColumnProfile(columnName, reader.GetDataTypeName(index));
                            profiles[columnName] = profile;
                        }

                        if (reader.IsDBNull(index))
                        {
                            continue;
                        }

                        var value = Convert.ToString(reader.GetValue(index))?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            continue;
                        }

                        profile.NonEmptyCount++;
                        if (!IsPlaceholderValue(value))
                        {
                            profile.NonPlaceholderCount++;
                            if (profile.SampleValues.Count < 5 && !profile.SampleValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                            {
                                profile.SampleValues.Add(value);
                            }
                        }
                    }
                }

                var lines = new List<string>
                {
                    "ColumnName,DataType,NonEmptyCount,NonPlaceholderCount,SampleValues"
                };

                foreach (var profile in profiles.Values.OrderByDescending(item => item.NonPlaceholderCount).ThenBy(item => item.ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    lines.Add(string.Join(",",
                        EscapeCsv(profile.ColumnName),
                        EscapeCsv(profile.DataType),
                        profile.NonEmptyCount.ToString(),
                        profile.NonPlaceholderCount.ToString(),
                        EscapeCsv(string.Join(" | ", profile.SampleValues))));
                }

                var csv = string.Join(Environment.NewLine, lines);

                _logger.LogInformation(
                    "Exported job_posting schema profile for statuses {Statuses}. Columns: {Count}",
                    string.Join(", ", ActiveVacancyStatuses),
                    profiles.Count);

                return Response<string>.SuccessResponse(csv, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export job_posting schema profile from Fabric.");
                return Response<string>.ErrorResponse(
                    ["An unexpected error occurred while exporting job_posting schema profile from Fabric."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<string>> ExportActiveJobPostingRawCsv()
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                const string sqlQuery = @"
SELECT TOP (200) *
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
);";

                using var command = new SqlCommand(sqlQuery, sqlConnection);
                command.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);

                using var reader = await command.ExecuteReaderAsync();
                var headers = Enumerable.Range(0, reader.FieldCount)
                    .Select(reader.GetName)
                    .ToList();

                var lines = new List<string>
                {
                    string.Join(",", headers.Select(EscapeCsv))
                };

                while (await reader.ReadAsync())
                {
                    var values = new List<string>(reader.FieldCount);
                    for (var index = 0; index < reader.FieldCount; index++)
                    {
                        var value = reader.IsDBNull(index)
                            ? string.Empty
                            : Convert.ToString(reader.GetValue(index)) ?? string.Empty;

                        values.Add(EscapeCsv(value));
                    }

                    lines.Add(string.Join(",", values));
                }

                _logger.LogInformation("Exported raw job_posting rows for statuses {Statuses}.", string.Join(", ", ActiveVacancyStatuses));

                return Response<string>.SuccessResponse(string.Join(Environment.NewLine, lines), HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export raw job_posting rows from Fabric.");
                return Response<string>.ErrorResponse(
                    ["An unexpected error occurred while exporting raw job_posting rows from Fabric."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        public async Task<Response<FabricConnectionDiagnostics>> GetActiveJobPostingDiagnostics()
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using SqlConnection sqlConnection = new SqlConnection(_fabricConnectionStringDwLeads) { AccessToken = token.Token };

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    await sqlConnection.OpenAsync();

                var builder = new SqlConnectionStringBuilder(_fabricConnectionStringDwLeads);
                var diagnostics = new FabricConnectionDiagnostics
                {
                    DataSource = sqlConnection.DataSource ?? builder.DataSource,
                    InitialCatalog = sqlConnection.Database ?? builder.InitialCatalog,
                    SourceTable = "gld.job_posting",
                    StatusesFilter = ActiveVacancyStatuses.ToList()
                };

                diagnostics.DatabaseName = await TryGetScalarValue(sqlConnection, "DB_NAME()", diagnostics.DiagnosticsErrors, "DB_NAME");
                diagnostics.SuserSname = await TryGetScalarValue(sqlConnection, "SUSER_SNAME()", diagnostics.DiagnosticsErrors, "SUSER_SNAME");
                diagnostics.OriginalLogin = await TryGetScalarValue(sqlConnection, "ORIGINAL_LOGIN()", diagnostics.DiagnosticsErrors, "ORIGINAL_LOGIN");
                diagnostics.UserName = await TryGetScalarValue(sqlConnection, "USER_NAME()", diagnostics.DiagnosticsErrors, "USER_NAME");
                diagnostics.CurrentUser = await TryGetScalarValue(sqlConnection, "CURRENT_USER", diagnostics.DiagnosticsErrors, "CURRENT_USER");

                const string tableCandidatesSql = @"
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'gld'
AND (
    TABLE_NAME = 'job_posting'
    OR TABLE_NAME = 'sc_job_posting'
    OR TABLE_NAME LIKE '%job%posting%'
)
ORDER BY TABLE_SCHEMA, TABLE_NAME;";

                try
                {
                    using var tableCommand = new SqlCommand(tableCandidatesSql, sqlConnection);
                    using var tableReader = await tableCommand.ExecuteReaderAsync();
                    while (await tableReader.ReadAsync())
                    {
                        diagnostics.TableCandidates.Add(new FabricTableCandidate
                        {
                            Schema = tableReader.IsDBNull(0) ? string.Empty : tableReader.GetString(0),
                            Name = tableReader.IsDBNull(1) ? string.Empty : tableReader.GetString(1)
                        });
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.DiagnosticsErrors.Add($"TABLE_CANDIDATES: {ex.Message}");
                }

                const string matchingCountSql = @"
SELECT COUNT(1)
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
);";

                try
                {
                    using var countCommand = new SqlCommand(matchingCountSql, sqlConnection);
                    countCommand.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);
                    diagnostics.MatchingStatusRowCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                }
                catch (Exception ex)
                {
                    diagnostics.DiagnosticsErrors.Add($"MATCHING_STATUS_ROW_COUNT: {ex.Message}");
                }

                const string usableRequiredFieldsCountSql = @"
SELECT COUNT(1)
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
)
AND NULLIF(LTRIM(RTRIM(CAST(job_code AS nvarchar(4000)))), '') IS NOT NULL
AND NULLIF(LTRIM(RTRIM(CAST(position_name AS nvarchar(4000)))), '') IS NOT NULL
AND NULLIF(LTRIM(RTRIM(CAST(main_country AS nvarchar(4000)))), '') IS NOT NULL
AND LOWER(LTRIM(RTRIM(CAST(job_code AS nvarchar(4000))))) NOT IN ('x', 'xx', 'xxx', 'xxxx', 'n/a', 'na')
AND LOWER(LTRIM(RTRIM(CAST(position_name AS nvarchar(4000))))) NOT IN ('x', 'xx', 'xxx', 'xxxx', 'n/a', 'na')
AND LOWER(LTRIM(RTRIM(CAST(main_country AS nvarchar(4000))))) NOT IN ('x', 'xx', 'xxx', 'xxxx', 'n/a', 'na');";

                try
                {
                    using var usableCountCommand = new SqlCommand(usableRequiredFieldsCountSql, sqlConnection);
                    usableCountCommand.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);
                    diagnostics.UsableRequiredFieldsRowCount = Convert.ToInt32(await usableCountCommand.ExecuteScalarAsync());
                }
                catch (Exception ex)
                {
                    diagnostics.DiagnosticsErrors.Add($"USABLE_REQUIRED_FIELDS_ROW_COUNT: {ex.Message}");
                }

                const string statusCountsSql = @"
SELECT TOP (25)
    CAST(job_status AS nvarchar(4000)) AS JobStatus,
    COUNT(1) AS StatusCount
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
)
OR LOWER(LTRIM(RTRIM(job_status))) LIKE '%testing%'
GROUP BY CAST(job_status AS nvarchar(4000))
ORDER BY COUNT(1) DESC;";

                try
                {
                    using var statusCommand = new SqlCommand(statusCountsSql, sqlConnection);
                    statusCommand.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);

                    using var statusReader = await statusCommand.ExecuteReaderAsync();
                    while (await statusReader.ReadAsync())
                    {
                        diagnostics.StatusCounts.Add(new FabricJobStatusCount
                        {
                            JobStatus = statusReader.IsDBNull(0) ? string.Empty : statusReader.GetString(0),
                            Count = statusReader.IsDBNull(1) ? 0 : Convert.ToInt32(statusReader.GetValue(1))
                        });
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.DiagnosticsErrors.Add($"STATUS_COUNTS: {ex.Message}");
                }

                const string allStatusCountsSql = @"
SELECT TOP (25)
    CAST(job_status AS nvarchar(4000)) AS JobStatus,
    COUNT(1) AS StatusCount
FROM gld.job_posting
GROUP BY CAST(job_status AS nvarchar(4000))
ORDER BY COUNT(1) DESC;";

                try
                {
                    using var allStatusCommand = new SqlCommand(allStatusCountsSql, sqlConnection);
                    using var allStatusReader = await allStatusCommand.ExecuteReaderAsync();
                    while (await allStatusReader.ReadAsync())
                    {
                        diagnostics.AllStatusCounts.Add(new FabricJobStatusCount
                        {
                            JobStatus = allStatusReader.IsDBNull(0) ? string.Empty : allStatusReader.GetString(0),
                            Count = allStatusReader.IsDBNull(1) ? 0 : Convert.ToInt32(allStatusReader.GetValue(1))
                        });
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.DiagnosticsErrors.Add($"ALL_STATUS_COUNTS: {ex.Message}");
                }

                const string sampleSql = @"
SELECT TOP (5)
    CAST(job_code AS nvarchar(4000)) AS JobCode,
    CAST(position_name AS nvarchar(4000)) AS PositionName,
    CAST(main_country AS nvarchar(4000)) AS MainCountry,
    CAST(job_status AS nvarchar(4000)) AS JobStatus
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
);";

                try
                {
                    using var sampleCommand = new SqlCommand(sampleSql, sqlConnection);
                    sampleCommand.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);

                    using var sampleReader = await sampleCommand.ExecuteReaderAsync();
                    while (await sampleReader.ReadAsync())
                    {
                        diagnostics.SampleRows.Add(new FabricConnectionDiagnosticsRow
                        {
                            JobCode = sampleReader.IsDBNull(0) ? string.Empty : sampleReader.GetString(0),
                            PositionName = sampleReader.IsDBNull(1) ? string.Empty : sampleReader.GetString(1),
                            MainCountry = sampleReader.IsDBNull(2) ? string.Empty : sampleReader.GetString(2),
                            JobStatus = sampleReader.IsDBNull(3) ? string.Empty : sampleReader.GetString(3)
                        });
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.DiagnosticsErrors.Add($"SAMPLE_ROWS: {ex.Message}");
                }

                const string unfilteredSampleSql = @"
SELECT TOP (5)
    CAST(job_code AS nvarchar(4000)) AS JobCode,
    CAST(position_name AS nvarchar(4000)) AS PositionName,
    CAST(main_country AS nvarchar(4000)) AS MainCountry,
    CAST(job_status AS nvarchar(4000)) AS JobStatus
FROM gld.job_posting;";

                try
                {
                    using var unfilteredSampleCommand = new SqlCommand(unfilteredSampleSql, sqlConnection);

                    using var unfilteredSampleReader = await unfilteredSampleCommand.ExecuteReaderAsync();
                    while (await unfilteredSampleReader.ReadAsync())
                    {
                        diagnostics.UnfilteredSampleRows.Add(new FabricConnectionDiagnosticsRow
                        {
                            JobCode = unfilteredSampleReader.IsDBNull(0) ? string.Empty : unfilteredSampleReader.GetString(0),
                            PositionName = unfilteredSampleReader.IsDBNull(1) ? string.Empty : unfilteredSampleReader.GetString(1),
                            MainCountry = unfilteredSampleReader.IsDBNull(2) ? string.Empty : unfilteredSampleReader.GetString(2),
                            JobStatus = unfilteredSampleReader.IsDBNull(3) ? string.Empty : unfilteredSampleReader.GetString(3)
                        });
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.DiagnosticsErrors.Add($"UNFILTERED_SAMPLE_ROWS: {ex.Message}");
                }

                _logger.LogInformation(
                    "Fabric diagnostics resolved DataSource={DataSource}, InitialCatalog={InitialCatalog}, DB={Database}, SUSER_SNAME={Suser}, ORIGINAL_LOGIN={OriginalLogin}, USER_NAME={UserName}, CURRENT_USER={CurrentUser}. Errors={Errors}",
                    diagnostics.DataSource,
                    diagnostics.InitialCatalog,
                    diagnostics.DatabaseName,
                    diagnostics.SuserSname,
                    diagnostics.OriginalLogin,
                    diagnostics.UserName,
                    diagnostics.CurrentUser,
                    diagnostics.DiagnosticsErrors.Count == 0 ? "<none>" : string.Join(" | ", diagnostics.DiagnosticsErrors));

                return Response<FabricConnectionDiagnostics>.SuccessResponse(diagnostics, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Fabric connection diagnostics.");
                return Response<FabricConnectionDiagnostics>.ErrorResponse(
                    ["An unexpected error occurred while retrieving Fabric connection diagnostics."],
                    HttpStatusCode.BadRequest);
            }
            finally
            {
                await sqlConnection.CloseAsync();
            }
        }

        private static async Task<string> TryGetScalarValue(
            SqlConnection sqlConnection,
            string expression,
            List<string> errors,
            string label)
        {
            try
            {
                using var command = new SqlCommand($"SELECT CAST({expression} AS nvarchar(4000));", sqlConnection);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToString(result)?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                errors.Add($"{label}: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task LogRawJobPostingSamples(SqlConnection sqlConnection)
        {
            const string sqlQuery = @"
SELECT TOP (3) *
FROM gld.job_posting
WHERE LOWER(LTRIM(RTRIM(job_status))) IN (
    SELECT LOWER(LTRIM(RTRIM([value])))
    FROM OPENJSON(@jobStatusesJson)
);";

            using SqlCommand command = new SqlCommand(sqlQuery, sqlConnection);
            command.Parameters.Add("@jobStatusesJson", SqlDbType.NVarChar).Value = JsonSerializer.Serialize(ActiveVacancyStatuses);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            var samples = new List<string>();

            while (await reader.ReadAsync())
            {
                var fields = new List<string>();
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    if (reader.IsDBNull(index))
                    {
                        continue;
                    }

                    var value = Convert.ToString(reader.GetValue(index))?.Trim();
                    if (string.IsNullOrWhiteSpace(value) || IsPlaceholderValue(value))
                    {
                        continue;
                    }

                    fields.Add($"{reader.GetName(index)}={value}");
                }

                samples.Add(fields.Count == 0 ? "<no useful values>" : string.Join(" | ", fields));
            }

            _logger.LogInformation(
                "Fabric raw job_posting sample rows with non-placeholder values: {Samples}",
                samples.Count == 0 ? "<none>" : string.Join(" || ", samples));
        }

        private static async Task<HashSet<string>> GetTableColumns(SqlConnection sqlConnection, string schema, string table)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const string sql = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table;";

            using var command = new SqlCommand(sql, sqlConnection);
            command.Parameters.Add("@schema", SqlDbType.NVarChar, 128).Value = schema;
            command.Parameters.Add("@table", SqlDbType.NVarChar, 128).Value = table;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    columns.Add(reader.GetString(0));
                }
            }

            return columns;
        }

        private static async Task<string> GetCurrentDatabaseName(SqlConnection sqlConnection)
        {
            using var command = new SqlCommand("SELECT DB_NAME();", sqlConnection);
            var value = await command.ExecuteScalarAsync();
            return Convert.ToString(value) ?? string.Empty;
        }

        private static async Task<List<(string Schema, string Name)>> GetVisibleTables(SqlConnection sqlConnection)
        {
            const string sql = @"
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;";

            using var command = new SqlCommand(sql, sqlConnection);
            var tables = new List<(string Schema, string Name)>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add((
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
            }

            return tables;
        }

        private static async Task<List<string>> GetTextTableColumns(SqlConnection sqlConnection, string schema, string table)
        {
            var columns = new List<string>();
            const string sql = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @schema
  AND TABLE_NAME = @table
  AND DATA_TYPE IN ('char', 'nchar', 'varchar', 'nvarchar', 'text', 'ntext');";

            using var command = new SqlCommand(sql, sqlConnection);
            command.Parameters.Add("@schema", SqlDbType.NVarChar, 128).Value = schema;
            command.Parameters.Add("@table", SqlDbType.NVarChar, 128).Value = table;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    columns.Add(reader.GetString(0));
                }
            }

            return columns
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static async Task<List<Dictionary<string, string>>> GetApplicantDiagnosticRows(
            SqlConnection sqlConnection,
            string column,
            string value)
        {
            return await GetApplicantDiagnosticRows(sqlConnection, [column], value);
        }

        private static async Task<List<Dictionary<string, string>>> GetTableDiagnosticRows(
            SqlConnection sqlConnection,
            string schema,
            string table,
            IReadOnlyCollection<string> columns,
            string value)
        {
            if (columns.Count == 0 || string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            var predicates = columns
                .Select(column => $"LOWER(LTRIM(RTRIM(CAST({QuoteSqlIdentifier(column)} AS nvarchar(4000))))) = @value")
                .ToList();
            var sql = $@"
SELECT TOP (20) *
FROM {QuoteSqlIdentifier(schema)}.{QuoteSqlIdentifier(table)}
WHERE {string.Join(" OR ", predicates)};";

            using var command = new SqlCommand(sql, sqlConnection);
            command.Parameters.Add("@value", SqlDbType.NVarChar).Value = value.Trim().ToLowerInvariant();

            var rows = new List<Dictionary<string, string>>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i)
                        ? string.Empty
                        : Convert.ToString(reader.GetValue(i)) ?? string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static async Task<List<Dictionary<string, string>>> GetTableSampleRows(
            SqlConnection sqlConnection,
            string schema,
            string table,
            int top)
        {
            var sql = $@"
SELECT TOP ({Math.Max(1, top)}) *
FROM {QuoteSqlIdentifier(schema)}.{QuoteSqlIdentifier(table)};";

            using var command = new SqlCommand(sql, sqlConnection);
            var rows = new List<Dictionary<string, string>>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i)
                        ? string.Empty
                        : Convert.ToString(reader.GetValue(i)) ?? string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static async Task<long?> GetTableRowCount(SqlConnection sqlConnection, string schema, string table)
        {
            var sql = $@"
SELECT COUNT_BIG(1)
FROM {QuoteSqlIdentifier(schema)}.{QuoteSqlIdentifier(table)};";

            using var command = new SqlCommand(sql, sqlConnection);
            var value = await command.ExecuteScalarAsync();
            return value == null || value == DBNull.Value
                ? null
                : Convert.ToInt64(value);
        }

        private static async Task<List<Dictionary<string, string>>> GetApplicantDiagnosticRows(
            SqlConnection sqlConnection,
            IReadOnlyCollection<string> columns,
            string value)
        {
            if (columns.Count == 0 || string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            var predicates = columns
                .Select(column => $"LOWER(LTRIM(RTRIM(CAST({QuoteSqlIdentifier(column)} AS nvarchar(4000))))) = @value")
                .ToList();
            var sql = $@"
SELECT TOP (20) *
FROM gld.sc_applicants
WHERE {string.Join(" OR ", predicates)};";

            using var command = new SqlCommand(sql, sqlConnection);
            command.Parameters.Add("@value", SqlDbType.NVarChar).Value = value.Trim().ToLowerInvariant();

            var rows = new List<Dictionary<string, string>>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i)
                        ? string.Empty
                        : Convert.ToString(reader.GetValue(i)) ?? string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static string QuoteSqlIdentifier(string identifier)
        {
            return $"[{identifier.Replace("]", "]]")}]";
        }

        private static bool HasColumn(IDataRecord reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> GetExistingColumns(HashSet<string> availableColumns, params string[] candidates)
        {
            return candidates
                .Where(candidate => availableColumns.Contains(candidate))
                .ToList();
        }

        private static string[] GetApplicantEmailColumnCandidates()
        {
            return
            [
                "email",
                "candidate_email",
                "applicant_email",
                "personal_email",
                "correo",
                "correo_electronico",
                "e_mail",
                "mail",
                "email_address",
                "candidate_email_address",
                "applicant_email_address",
                "personal_email_address"
            ];
        }

        private static string[] GetEmployeePersonalIdColumnCandidates()
        {
            return
            [
                "personal_id",
                "dni",
                "document_number",
                "numero_documento",
                "numero_identificacion",
                "numero_de_identificacion",
                "identification_number",
                "id_number",
                "national_id",
                "national_id_number",
                "ssn",
                "cedula",
                "cedula_ciudadania",
                "identificacion",
                "documento",
                "employee_id",
                "employee_number"
            ];
        }

        private static string[] GetEmployeeStartDateColumnCandidates()
        {
            return
            [
                "start_date",
                "star_date",
                "hire_date",
                "hiring_date",
                "employment_start_date",
                "date_of_hire",
                "fecha_ingreso",
                "fecha_de_ingreso",
                "fecha_inicio",
                "fecha_contratacion"
            ];
        }

        private static string[] GetEmployeeStatusColumnCandidates()
        {
            return
            [
                "status",
                "employee_status",
                "employment_status",
                "worker_status",
                "estado",
                "estado_empleado",
                "estado_colaborador",
                "state"
            ];
        }

        private static bool IsPeopleHrCandidateTable(string schema, string table)
        {
            var value = $"{schema}.{table}".ToLowerInvariant();
            return value.Contains("emplead") ||
                value.Contains("employee") ||
                value.Contains("headcount") ||
                value.Contains("head_count") ||
                value.Contains("personal") ||
                value.Contains("people") ||
                value.Contains("worker") ||
                value.Contains("colaborador") ||
                value.Contains("staff") ||
                value.Equals("slv.st_empleados_unificado", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> GetLikelyPersonalIdTextColumns(IEnumerable<string> columns)
        {
            return columns
                .Where(column =>
                    column.Contains("id", StringComparison.OrdinalIgnoreCase) ||
                    column.Contains("dni", StringComparison.OrdinalIgnoreCase) ||
                    column.Contains("document", StringComparison.OrdinalIgnoreCase) ||
                    column.Contains("ident", StringComparison.OrdinalIgnoreCase) ||
                    column.Contains("cedula", StringComparison.OrdinalIgnoreCase) ||
                    column.Contains("personal", StringComparison.OrdinalIgnoreCase) ||
                    column.Contains("ssn", StringComparison.OrdinalIgnoreCase) ||
                    column.Contains("employee", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static string GetFirstPreferredValue(SqlDataReader reader, IEnumerable<string> candidates)
        {
            string? fallbackValue = null;

            foreach (var candidate in candidates)
            {
                var ordinal = reader.GetOrdinal(candidate);
                if (reader.IsDBNull(ordinal))
                {
                    continue;
                }

                var value = Convert.ToString(reader.GetValue(ordinal))?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                fallbackValue ??= value;
                if (!IsPlaceholderValue(value))
                {
                    return value;
                }
            }

            return fallbackValue ?? string.Empty;
        }

        private static bool IsPlaceholderValue(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalized)
                || normalized == "n/a"
                || normalized == "na"
                || normalized.All(ch => ch == 'x');
        }

        private static async Task<List<JobPostingColumnMetadata>> GetJobPostingColumnMetadata(SqlConnection sqlConnection)
        {
            const string sql = @"
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'gld' AND TABLE_NAME = 'job_posting'
ORDER BY ORDINAL_POSITION;";

            using var command = new SqlCommand(sql, sqlConnection);
            using var reader = await command.ExecuteReaderAsync();
            var columns = new List<JobPostingColumnMetadata>();

            while (await reader.ReadAsync())
            {
                columns.Add(new JobPostingColumnMetadata(
                    reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
            }

            return columns;
        }

        private static string EscapeCsv(string value)
        {
            var safe = value ?? string.Empty;
            if (!safe.Contains(',') && !safe.Contains('"') && !safe.Contains('\n') && !safe.Contains('\r'))
            {
                return safe;
            }

            return $"\"{safe.Replace("\"", "\"\"")}\"";
        }

        private sealed record JobPostingColumnMetadata(string Name, string DataType);

        private sealed class JobPostingColumnProfile
        {
            public JobPostingColumnProfile(string columnName, string dataType)
            {
                ColumnName = columnName;
                DataType = dataType;
            }

            public string ColumnName { get; }
            public string DataType { get; }
            public int NonEmptyCount { get; set; }
            public int NonPlaceholderCount { get; set; }
            public List<string> SampleValues { get; } = new();
        }

        private static Dictionary<string, string> ReadRow(SqlDataReader reader)
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                var name = reader.GetName(index);
                var value = reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index))?.Trim() ?? string.Empty;
                row[name] = value;
            }

            return row;
        }

        private static List<string> ExpandCandidateColumns(
            HashSet<string> availableColumns,
            IEnumerable<string> explicitCandidates,
            Regex[] patterns)
        {
            var columns = new List<string>();

            foreach (var candidate in explicitCandidates)
            {
                if (!columns.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    columns.Add(candidate);
                }
            }

            foreach (var column in availableColumns.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                if (patterns.Any(pattern => pattern.IsMatch(column)) &&
                    !columns.Contains(column, StringComparer.OrdinalIgnoreCase))
                {
                    columns.Add(column);
                }
            }

            // Always score the full schema. Known names and regex hits go first,
            // but we do not exclude unknown columns that may hold the real values.
            foreach (var column in availableColumns.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                if (!columns.Contains(column, StringComparer.OrdinalIgnoreCase))
                {
                    columns.Add(column);
                }
            }

            return columns;
        }

        private static string SelectBestVacancyIdColumn(List<Dictionary<string, string>> rows, List<string> candidates)
        {
            return ScoreColumns(rows, candidates, ScoreVacancyIdColumn)
                .FirstOrDefault()?.Column ?? candidates.FirstOrDefault() ?? string.Empty;
        }

        private static string SelectBestPositionNameColumn(List<Dictionary<string, string>> rows, List<string> candidates)
        {
            return ScoreColumns(rows, candidates, ScorePositionNameColumn)
                .FirstOrDefault()?.Column ?? candidates.FirstOrDefault() ?? string.Empty;
        }

        private static string SelectBestCountryColumn(List<Dictionary<string, string>> rows, List<string> candidates)
        {
            return ScoreColumns(rows, candidates, ScoreCountryColumn)
                .FirstOrDefault()?.Column ?? candidates.FirstOrDefault() ?? string.Empty;
        }

        private static List<ColumnScore> ScoreColumns(
            List<Dictionary<string, string>> rows,
            List<string> candidates,
            Func<string, IReadOnlyList<string>, double> scorer)
        {
            return candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(column =>
                {
                    var values = rows
                        .Select(row => GetValue(row, column))
                        .ToList();

                    return new ColumnScore(
                        column,
                        scorer(column, values),
                        values.Count(value => !IsPlaceholderValue(value)),
                        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty);
                })
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.NonPlaceholderCount)
                .ThenBy(item => item.Column, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string DescribeTopCandidates(List<ColumnScore> candidates)
        {
            return string.Join(" || ",
                candidates
                    .Take(10)
                    .Select(item =>
                        $"{item.Column}:score={item.Score:0.00}:nonPlaceholder={item.NonPlaceholderCount}:sample={item.Sample}"));
        }

        private static string ResolveVacancyIdValue(
            Dictionary<string, string> row,
            IReadOnlyList<ColumnScore> rankedColumns)
        {
            return ResolveBestRowValue(
                row,
                rankedColumns,
                value => !IsPlaceholderValue(value) && Regex.IsMatch(value, @"[A-Za-z0-9]"),
                value => !string.IsNullOrWhiteSpace(value));
        }

        private static string ResolvePositionNameValue(
            Dictionary<string, string> row,
            IReadOnlyList<ColumnScore> rankedColumns)
        {
            return ResolveBestRowValue(
                row,
                rankedColumns,
                value => !IsPlaceholderValue(value) && LooksLikePositionName(value),
                value => !string.IsNullOrWhiteSpace(value) && LooksLikePositionName(value));
        }

        private static string ResolveCountryValue(
            Dictionary<string, string> row,
            IReadOnlyList<ColumnScore> rankedColumns)
        {
            return ResolveBestRowValue(
                row,
                rankedColumns,
                value => !IsPlaceholderValue(value) && LooksLikeCountry(value),
                value => !string.IsNullOrWhiteSpace(value));
        }

        private static string ResolveBestRowValue(
            Dictionary<string, string> row,
            IReadOnlyList<ColumnScore> rankedColumns,
            Func<string, bool> preferredPredicate,
            Func<string, bool> fallbackPredicate)
        {
            foreach (var candidate in rankedColumns)
            {
                var value = GetValue(row, candidate.Column);
                if (preferredPredicate(value))
                {
                    return value;
                }
            }

            foreach (var candidate in rankedColumns)
            {
                var value = GetValue(row, candidate.Column);
                if (fallbackPredicate(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static bool LooksLikePositionName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            if (LooksLikeDateOrDateTime(trimmed))
            {
                return false;
            }

            return trimmed.Length >= 3
                && Regex.IsMatch(trimmed, @"[A-Za-z]", RegexOptions.IgnoreCase)
                && !Regex.IsMatch(trimmed, @"^https?://", RegexOptions.IgnoreCase)
                && !Regex.IsMatch(trimmed, @"^[\{\[]")
                && !Regex.IsMatch(trimmed, @"^\d+(\.\d+)?$");
        }

        private static bool LooksLikeCountry(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            if (Regex.IsMatch(trimmed, @"^\d+(\.\d+)?$"))
            {
                return false;
            }

            return KnownCountryNames.Contains(trimmed, StringComparer.OrdinalIgnoreCase)
                || Regex.IsMatch(trimmed, @"^[A-Z]{2,3}$")
                || Regex.IsMatch(trimmed, @"^[A-Za-z][A-Za-z\s\-\(\)]+$");
        }

        private static bool LooksLikeDateOrDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            if (DateTime.TryParse(trimmed, out _))
            {
                return true;
            }

            return Regex.IsMatch(trimmed, @"^\d{1,2}/\d{1,2}/\d{2,4}(\s+\d{1,2}:\d{2}(:\d{2})?\s*(AM|PM)?)?$", RegexOptions.IgnoreCase)
                || Regex.IsMatch(trimmed, @"^\d{4}-\d{2}-\d{2}(T|\s)\d{2}:\d{2}", RegexOptions.IgnoreCase);
        }

        private static double ScoreVacancyIdColumn(string column, IReadOnlyList<string> values)
        {
            var cleanValues = values.Where(value => !IsPlaceholderValue(value)).ToList();
            if (cleanValues.Count == 0)
            {
                return double.MinValue;
            }

            var nameScore = Regex.IsMatch(column, @"(job|posting|position|vacancy).*(id|code)|(^|_)(id|code)$", RegexOptions.IgnoreCase)
                ? 8
                : Regex.IsMatch(column, @"id|code", RegexOptions.IgnoreCase) ? 4 : 0;

            var uniqueRatio = cleanValues.Distinct(StringComparer.OrdinalIgnoreCase).Count() / (double)cleanValues.Count;
            var alphaNumericRatio = cleanValues.Count(value => Regex.IsMatch(value, @"^[a-z0-9\-_]+$", RegexOptions.IgnoreCase)) / (double)cleanValues.Count;
            var dateLikeRatio = cleanValues.Count(LooksLikeDateOrDateTime) / (double)cleanValues.Count;
            var yearOnlyRatio = cleanValues.Count(value => Regex.IsMatch(value.Trim(), @"^(19|20)\d{2}$")) / (double)cleanValues.Count;
            var avgLength = cleanValues.Average(value => value.Length);

            return nameScore
                + (cleanValues.Count * 1.5)
                + (uniqueRatio * 10)
                + (alphaNumericRatio * 6)
                - (dateLikeRatio * 20)
                - (yearOnlyRatio * 10)
                - Math.Abs(avgLength - 12) * 0.08;
        }

        private static double ScorePositionNameColumn(string column, IReadOnlyList<string> values)
        {
            var cleanValues = values.Where(value => !IsPlaceholderValue(value)).ToList();
            if (cleanValues.Count == 0)
            {
                return double.MinValue;
            }

            var nameScore = Regex.IsMatch(column, @"(position|job|posting).*(name|title)|(^|_)(name|title)$", RegexOptions.IgnoreCase)
                ? 8
                : Regex.IsMatch(column, @"name|title", RegexOptions.IgnoreCase) ? 4 : 0;

            var titleLikeRatio = cleanValues.Count(LooksLikePositionName) / (double)cleanValues.Count;
            var spacedRatio = cleanValues.Count(value => value.Contains(' ')) / (double)cleanValues.Count;
            var uniqueRatio = cleanValues.Distinct(StringComparer.OrdinalIgnoreCase).Count() / (double)cleanValues.Count;
            var dateLikeRatio = cleanValues.Count(LooksLikeDateOrDateTime) / (double)cleanValues.Count;
            var numericRatio = cleanValues.Count(value => Regex.IsMatch(value.Trim(), @"^\d+(\.\d+)?$")) / (double)cleanValues.Count;
            var avgLength = cleanValues.Average(value => value.Length);

            return nameScore
                + (cleanValues.Count * 1.2)
                + (titleLikeRatio * 12)
                + (spacedRatio * 5)
                + (uniqueRatio * 4)
                - (dateLikeRatio * 25)
                - (numericRatio * 10)
                - Math.Abs(avgLength - 28) * 0.05;
        }

        private static double ScoreCountryColumn(string column, IReadOnlyList<string> values)
        {
            var cleanValues = values.Where(value => !IsPlaceholderValue(value)).ToList();
            if (cleanValues.Count == 0)
            {
                return double.MinValue;
            }

            var nameScore = Regex.IsMatch(column, @"country|nation", RegexOptions.IgnoreCase)
                ? 10
                : Regex.IsMatch(column, @"location|region|site|market", RegexOptions.IgnoreCase) ? 3 : 0;

            var normalizedValues = cleanValues
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            var countryLikeRatio = normalizedValues.Count(LooksLikeCountry) / (double)normalizedValues.Count;
            var numericRatio = normalizedValues.Count(value => Regex.IsMatch(value.Trim(), @"^\d+(\.\d+)?$")) / (double)normalizedValues.Count;

            var distinctCount = normalizedValues.Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var boundedDistinctScore = distinctCount is >= 1 and <= 80 ? 4 : 0;

            return nameScore
                + (cleanValues.Count * 1.1)
                + (countryLikeRatio * 10)
                - (numericRatio * 15)
                + boundedDistinctScore;
        }

        private static string GetValue(Dictionary<string, string> row, string column)
        {
            return string.IsNullOrWhiteSpace(column) || !row.TryGetValue(column, out var value)
                ? string.Empty
                : value.Trim();
        }

        private sealed record ColumnScore(string Column, double Score, int NonPlaceholderCount, string Sample);

        private static readonly Regex[] VacancyIdPatterns =
        [
            new Regex(@"(^|_)(job|posting|position).*(code|id)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(^|_)(code|id)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        ];

        private static readonly Regex[] PositionPatterns =
        [
            new Regex(@"position.*(name|title)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"job.*(name|title)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(^|_)(title|name)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        ];

        private static readonly Regex[] CountryPatterns =
        [
            new Regex(@"country", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"location.*country", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        ];

        private static readonly string[] KnownCountryNames =
        [
            "Argentina", "Bolivia", "Brazil", "Brasil", "Canada", "Chile", "Colombia", "Costa Rica",
            "Dominican Republic", "Ecuador", "El Salvador", "Guatemala", "Honduras", "Mexico",
            "Nicaragua", "Panama", "Paraguay", "Peru", "Puerto Rico", "United States", "USA",
            "Uruguay", "Venezuela", "Spain", "Remote"
        ];
    }
}
