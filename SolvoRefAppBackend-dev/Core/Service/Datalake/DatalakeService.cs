using Microsoft.Data.SqlClient;
using Azure.Core;
using Azure.Identity;
using Core.Contracts.Datalake;
using Core.Models.Global;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Data;
using Core.Models.Datalake;

namespace Core.Service.Datalake
{
    public class DatalakeService : IDatalakeService
    {
        private readonly ClientSecretCredential _credential;
        private readonly TokenRequestContext _tokenRequestContext;
        private readonly string _datalakeConectionString;
        public DatalakeService(IConfiguration configuration)
        {
            _credential = new ClientSecretCredential(configuration["AzureDatalakeData:tenantId"], configuration["AzureDatalakeData:clientId"], configuration["AzureDatalakeData:clientSecret"]);
            _tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            _datalakeConectionString = configuration.GetConnectionString("DataLakeConnectionStringHeadCount") ?? "";
        }

        public async Task<Response<EmployeeInfoCheck>> GetEmployeeInfoForCheck(string personalId, string email)
        {
            var token = await _credential.GetTokenAsync(_tokenRequestContext);
            using (SqlConnection sqlConnection = new SqlConnection(_datalakeConectionString) { AccessToken = token.Token })
            {
                Response<EmployeeInfoCheck> response = new();
                EmployeeInfoCheck? info = new();

                try
                {
                    await sqlConnection.OpenAsync();
                    string sqlQuery = @$"SELECT [STATUS], [NOM_AREA] FROM [dbo].[empleados_unificado] WHERE [personaL_ID] = @personalId";

                    using (SqlCommand command = new SqlCommand(sqlQuery, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@personalId", personalId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                info.NomArea = reader.IsDBNull(reader.GetOrdinal("NOM_AREA")) ? string.Empty : reader.GetString("NOM_AREA");
                                info.Status = reader.IsDBNull(reader.GetOrdinal("STATUS")) ? string.Empty : reader.GetString("STATUS");
                            }
                            else
                            {
                                info = null;
                            }
                        }
                    }
                    response.Success = true;
                    response.Data = info;
                    response.StatusCode = HttpStatusCode.OK;
                    return response;
                }
                catch (Exception)
                {
                    await sqlConnection.CloseAsync();
                    response.Success = false;
                    response.Errors = ["An unexpected error occurred while retrieving employee information."];
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return response;
                }
                finally
                {
                    await sqlConnection.CloseAsync();
                }
            }
        }
    }
}
