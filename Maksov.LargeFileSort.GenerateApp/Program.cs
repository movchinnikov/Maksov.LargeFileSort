using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Serilog;

namespace Maksov.LargeFileSort.GenerateApp
{
    public static class Program
    {
        static Program()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
        }
        
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("Utility to generate a large text file in parts and merge them into a single file.")
            {
                new Option<float>(["--desireFileSizeGb", "-s"], getDefaultValue: () => 1.0f, description: "Desired file size in GB"),
                new Option<string>(["--outputFileNamePath", "-o"], getDefaultValue: () => $"GeneratedLargeFile_{DateTime.Now:yyyyMMdd_HHmmss}.txt", description: "Output file path"),
                new Option<int>(["--maxPartFileSizeMb", "-p"], getDefaultValue: () => 100, description: "Max part file size in MB"),
                new Option<bool>("--verbose", getDefaultValue: () => false, description: "Enable verbose logging")
            };

            rootCommand.Handler = CommandHandler.Create<float, string, int, bool>(async (desireFileSizeGb, outputFileNamePath, maxPartFileSizeMb, verbose) =>
            {
                if (verbose)
                {
                    Log.Information("Verbose mode enabled.");
                }
                
                if (File.Exists(outputFileNamePath))
                {
                    Console.WriteLine($"File {outputFileNamePath} already exists. Do you want to overwrite it? (y/n)");
                    var response = Console.ReadLine();
                    if (response?.ToLower() != "y")
                    {
                        Log.Information("Operation canceled by user.");
                        Console.WriteLine("Operation canceled by user.");
                        return;
                    }
                }

                var fileGenerator = new FileGenerator();

                await fileGenerator.GenerateAsync(outputFileNamePath, desireFileSizeGb, maxPartFileSizeMb);
            });

            await rootCommand.InvokeAsync(args);
        }
    }
}
