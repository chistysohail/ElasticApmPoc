using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;

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

        // Set Elastic APM environment variables
        Environment.SetEnvironmentVariable("ELASTIC_APM_SERVER_URL", apmServerUrl);
        Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_NAME", serviceName);
        Environment.SetEnvironmentVariable("ELASTIC_APM_ENVIRONMENT", environment);
        Environment.SetEnvironmentVariable("ELASTIC_APM_SECRET_TOKEN", secretToken);

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
