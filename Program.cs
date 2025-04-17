using mcpserver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Método principal
async Task Main(string[] args)
{
    // Executa o servidor MCP normalmente
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.AddConsole(consoleLogOptions =>
    {
        // Configure all logs to go to stderr
        consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    // Registrar os serviços do CodeInsightsIA
    builder.Services.AddSingleton<CodeInsightsIA>();
    
    // Configurar o servidor MCP
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    await builder.Build().RunAsync();
}

// Executa o método Main
await Main(args);