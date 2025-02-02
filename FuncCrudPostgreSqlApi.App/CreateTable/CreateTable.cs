using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FuncCrudPostgreSqlApi.CreateTable;

public class CreateTable
{
    private readonly ILogger<CreateTable> _logger;

    public CreateTable(ILogger<CreateTable> logger)
    {
        _logger = logger;
    }

    [Function("CreateTable")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "create")] HttpRequestData req)
    {
        _logger.LogInformation("Processing request to initialize database.");

        // Get value from Function App's environment variable
        string? connString = Environment.GetEnvironmentVariable("PostgreSQL_ConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            _logger.LogError("Connection string is not configured.");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Database connection string is missing.");
            return errorResponse;
        }

        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                // Open connection
                await conn.OpenAsync();

                // Drop existsing tables
                using (var command = new NpgsqlCommand("DROP TABLE IF EXISTS inventory", conn))
                {
                    await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Dropped existing inventory table.");
                }

                // Create table
                using (var command = new NpgsqlCommand("CREATE TABLE inventory(id serial PRIMARY KEY, name VARCHAR(50), quantity INTEGER)", conn))
                {
                    await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Created inventory table.");
                }

                // Init table data
                using (var command = new NpgsqlCommand("INSERT INTO inventory (name, quantity) VALUES (@n1, @q1), (@n2, @q2), (@n3, @q3)", conn))
                {
                    command.Parameters.AddWithValue("n1", "banana");
                    command.Parameters.AddWithValue("q1", 150);
                    command.Parameters.AddWithValue("n2", "orange");
                    command.Parameters.AddWithValue("q2", 154);
                    command.Parameters.AddWithValue("n3", "apple");
                    command.Parameters.AddWithValue("q3", 100);

                    int nRows = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation($"Inserted {nRows} rows into inventory table.");
                }
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Database initialized successfully.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error initializing database: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Database initialization failed.");
            return errorResponse;
        }
    }
}