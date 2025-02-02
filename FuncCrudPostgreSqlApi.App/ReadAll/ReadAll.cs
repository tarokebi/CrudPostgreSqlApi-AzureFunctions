using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Npgsql;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace FuncCrudPostgreSqlApi.ReadAll;

public class ReadAll
{
    private readonly ILogger<ReadAll> _logger;

    public ReadAll(ILogger<ReadAll> logger)
    {
        _logger = logger;
    }

    [Function("ReadAll")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "")] HttpRequestData req)
    {
        _logger.LogInformation("Azure Function triggered: Reading from PostgreSQL.");

        // Get value from Function App's environment variable
        string? connString = Environment.GetEnvironmentVariable("PostgreSQL_ConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            _logger.LogError("Connection string is not configured.");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Database connection string is missing.");
            return errorResponse;
        }

        List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();
                using (var command = new NpgsqlCommand("SELECT * FROM inventory", conn))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>
                        {
                            { "id", reader.GetInt32(0) },
                            { "name", reader.GetString(1) },
                            { "quantity", reader.GetInt32(2) }
                        };
                        results.Add(row);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading from PostgreSQL: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Database error: {ex.Message}");
            return errorResponse;
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(results));

        return response;
    }

    //private static string GetConnectionString()
    //{
    //    string host = Environment.GetEnvironmentVariable("PG_HOST") ?? "mydemoserver.postgres.database.azure.com";
    //    string user = Environment.GetEnvironmentVariable("PG_USER") ?? "mylogin";
    //    string dbname = Environment.GetEnvironmentVariable("PG_DBNAME") ?? "postgres";
    //    string password = Environment.GetEnvironmentVariable("PG_PASSWORD") ?? "<server_admin_password>";
    //    string port = Environment.GetEnvironmentVariable("PG_PORT") ?? "5432";

    //    return $"Server={host};User Id={user};Database={dbname};Port={port};Password={password};SSLMode=Prefer";
    //}
}
