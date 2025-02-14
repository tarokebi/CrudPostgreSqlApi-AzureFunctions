using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace FuncBookCrudlApi.App.Controllers;

public class FruitControllerFunc
{
    private readonly ILogger<FruitControllerFunc> _logger;
    private string? connString = Environment.GetEnvironmentVariable("PostgreSQL_ConnectionString");

    public FruitControllerFunc(ILogger<FruitControllerFunc> logger)
    {
        _logger = logger;
    }


    [Function("FruitControllerFunc")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "delete", Route = "book")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        // Get value from Function App's environment variable
        if (string.IsNullOrEmpty(connString))
        {
            _logger.LogError("Connection string is not configured.");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Database connection string is missing.");
            return errorResponse;
        }

        string method = req.Method.ToUpperInvariant();
        switch (method)
        {
            case "POST":
                return await CreateTable(req);
            case "GET":
                return await ReadAll(req);
            case "PUT":
                return await Update(req);
            case "DELETE":
                return await Delete(req);
            default:
                var response = req.CreateResponse(System.Net.HttpStatusCode.MethodNotAllowed);
                await response.WriteStringAsync("Method not allowed.");
                return response;
        }
    }

    public async Task<HttpResponseData> CreateTable(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "create")] HttpRequestData req)
    {
        _logger.LogInformation("Processing request to initialize database.");

        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var command = new NpgsqlCommand("DROP TABLE IF EXISTS inventory", conn))
                {
                    await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Dropped existing inventory table.");
                }

                using (var command = new NpgsqlCommand("CREATE TABLE inventory(id serial PRIMARY KEY, name VARCHAR(50), quantity INTEGER)", conn))
                {
                    await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("Created inventory table.");
                }

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

    public async Task<HttpResponseData> ReadAll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "read")] HttpRequestData req)
    {
        _logger.LogInformation("Azure Function triggered: Reading from PostgreSQL.");

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

    public async Task<HttpResponseData> Update(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "update")] HttpRequestData req)
    {
        _logger.LogInformation("Azure Function triggered: Reading from PostgreSQL.");

        List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

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


    public async Task<HttpResponseData> Delete(
    [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "delete")] HttpRequestData req)
    {
        _logger.LogInformation("Azure Function triggered: Reading from PostgreSQL.");

        List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

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
