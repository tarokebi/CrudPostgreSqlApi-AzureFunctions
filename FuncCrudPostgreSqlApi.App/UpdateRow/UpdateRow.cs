using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Npgsql;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace FuncCrudPostgreSqlApi.UpdateRow;

public class UpdateRow
{
    private readonly ILogger<UpdateRow> _logger;

    public UpdateRow(ILogger<UpdateRow> logger)
    {
        _logger = logger;
    }

    [Function("UpdateRow")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "update")] HttpRequestData req)
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

                // Update
                using (var command = new NpgsqlCommand("UPDATE inventory SET quantity = @q WHERE name = @n", conn))
                {
                    command.Parameters.AddWithValue("n", "banana");
                    command.Parameters.AddWithValue("q", 200);

                    int nRows = await command.ExecuteNonQueryAsync();
                    Console.Out.WriteLine(String.Format("Number of rows updated={0}", nRows));
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
