using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.IO;
using static System.Net.WebRequestMethods;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Elastic APM Demo...");

        // Load Configuration
        var configuration = LoadConfiguration();

        // Read APM settings from appsettings.json
        string apmServerUrl = configuration["ElasticApm:ServerUrl"];
        string serviceName = configuration["ElasticApm:ServiceName"];
        string environment = configuration["ElasticApm:Environment"];
        string secretToken = configuration["ElasticApm:SecretToken"];

        // Read delay from environment variable or appsettings.json
        int startupDelay = GetConfigValue(configuration, "ElasticApm:StartupDelaySeconds", 10);
        Console.WriteLine($"[INFO] Applying startup delay of {startupDelay} seconds...");
        await Task.Delay(startupDelay * 1000);

        // Set Elastic APM environment variables
        Environment.SetEnvironmentVariable("ELASTIC_APM_SERVER_URL", apmServerUrl);
        Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_NAME", serviceName);
        Environment.SetEnvironmentVariable("ELASTIC_APM_ENVIRONMENT", environment);
        if (!string.IsNullOrEmpty(secretToken))
        {
            Environment.SetEnvironmentVariable("ELASTIC_APM_SECRET_TOKEN", secretToken);
        }

        Console.WriteLine($"configuration:::apmServerUrl: {apmServerUrl}, serviceName: {serviceName}, environment: {environment}");

        // Check APM Connection
        bool isConnected = await CheckApmConnection(apmServerUrl);
        if (!isConnected)
        {
            Console.WriteLine("[ERROR] Unable to connect to APM Server. Check configuration.");
            return;
        }
        Console.WriteLine("[INFO] Connected to APM Server successfully.");

        // Start a transaction manually
        var transaction = Agent.Tracer.StartTransaction("SampleTransaction", ApiConstants.TypeRequest);

        try
        {
            await SimulateHttpCall();
            await SimulateDatabaseQuery();
            SimulateProcessing();
        }
        catch (Exception ex)
        {
            transaction.CaptureException(ex);
        }
        finally
        {
            transaction.End();
        }

        Console.WriteLine("Elastic APM Demo completed. Check Kibana for traces.");
    }

    // ✅ UPDATED HEALTH CHECK METHOD (Uses root `/` instead of `/healthcheck`)
    static async Task<bool> CheckApmConnection(string apmServerUrl)
    {
        try
        {
            using HttpClient client = new();
            var response = await client.GetAsync(apmServerUrl); // ✅ USE ROOT `/`
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ✅ Updated Helper method to get config values from env vars or appsettings.json
    static int GetConfigValue(IConfiguration configuration, string key, int defaultValue)
    {
        string envValue = Environment.GetEnvironmentVariable(key.Replace(":", "__"));
        return !string.IsNullOrEmpty(envValue) && int.TryParse(envValue, out int envIntValue)
            ? envIntValue
            : configuration.GetValue<int>(key, defaultValue);
    }

    static IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    static async Task SimulateHttpCall()
    {
        var span = Agent.Tracer.CurrentTransaction?.StartSpan("HTTP Call", ApiConstants.TypeExternal);
        using HttpClient client = new();
        var response = await client.GetAsync("https://jsonplaceholder.typicode.com/todos/1");
        span?.End();
    }

    static async Task SimulateDatabaseQuery()
    {
        var span = Agent.Tracer.CurrentTransaction?.StartSpan("DB Query", ApiConstants.TypeDb);
        await Task.Delay(500);
        span?.End();
    }

    static void SimulateProcessing()
    {
        var span = Agent.Tracer.CurrentTransaction?.StartSpan("Processing Data", "custom");
        System.Threading.Thread.Sleep(1000);
        span?.End();
    }
}


//curl - X POST http://apm-server:8200/intake/v2/events \
//-H "Content-Type: application/x-ndjson" \
//-H "Authorization: Bearer YOUR_SECRET_TOKEN" \   # 👈 Add this if authentication is enabled
//    --data - binary @- << EOF
//{ "metadata":{ "service":{ "name":"test-service","agent":{ "name":"dotnet","version":"6.0.0"} } } }
//{ "transaction":{ "id":"12345","type":"custom","duration":100,"timestamp":$(($(date +% s % N) / 1000)),"trace_id":"abcdef12345678901234567890123456","span_count":{ "started":0,"dropped":0} } }
//EOF

//linux
//curl -X POST http://apm-server:8200/intake/v2/events -H "Content-Type: application/x-ndjson" --data-binary "{\"metadata\":{\"service\":{\"name\":\"test-service\",\"agent\":{\"name\":\"dotnet\",\"version\":\"6.0.0\"}}}}\n{\"transaction\":{\"id\":\"12345\",\"type\":\"custom\",\"duration\":100,\"timestamp\":$(($(date +%s%N)/1000)),\"trace_id\":\"abcdef12345678901234567890123456\",\"span_count\":{\"started\":0,\"dropped\":0}}}"
// 

//windows
//curl -X POST http://apm-server:8200/intake/v2/events -H "Content-Type: application/x-ndjson" --data-binary "{\"metadata\":{\"service\":{\"name\":\"test-service\",\"agent\":{\"name\":\"dotnet\",\"version\":\"6.0.0\"}}}}\n{\"transaction\":{\"id\":\"12345\",\"type\":\"custom\",\"duration\":100,\"timestamp\":$(($(date +%s%N)/1000)),\"trace_id\":\"abcdef12345678901234567890123456\",\"span_count\":{\"started\":0,\"dropped\":0}}}"
// 

//Output_if_All_Good:
//HTTP / 1.1 202 Accepted


//Output_if_Issue:
//{
//    "accepted": 0,
//    "errors": [
//    {
//        "message": "validation error: transaction: 'trace_id' required"
//    }
//    ]
//}

