using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Npgsql;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace FuncCrudPostgreSqlApi.DeleteRow;

public class DeleteRow
{
    private readonly ILogger<DeleteRow> _logger;

    public DeleteRow(ILogger<DeleteRow> logger)
    {
        _logger = logger;
    }

    [Function("DeleteRow")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "")] HttpRequestData req)
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
                // Open connection
                await conn.OpenAsync();

                // DeleteRow
                using (var command = new NpgsqlCommand("DELETE FROM inventory WHERE name = @n", conn))
                {
                    command.Parameters.AddWithValue("n", "orange");

                    int nRows = await command.ExecuteNonQueryAsync();
                    Console.Out.WriteLine(String.Format("Number of rows deleted={0}", nRows));
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
}
