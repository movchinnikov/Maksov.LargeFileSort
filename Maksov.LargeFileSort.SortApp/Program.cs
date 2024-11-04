using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Serilog;

namespace Maksov.LargeFileSort.SortApp;

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
            new Option<string>(new[] {"--inputFilePath", "-i"}, "Input file path") { IsRequired = true },
            new Option<string>(new[] {"--outputFilePath", "-o"}, () => $"SortedLargeFile_{DateTime.Now:yyyyMMdd_HHmmss}.txt", "Output file path"),
            new Option<bool>("--verbose", () => false, "Enable verbose logging")
        };

        rootCommand.Handler = CommandHandler.Create<string, string, bool>(
            async (inputFilePath, outputFilePath, verbose) =>
            {
                if (verbose)
                {
                    Log.Information("Verbose mode enabled.");
                }

                if (File.Exists(outputFilePath))
                {
                    Console.WriteLine($"File {outputFilePath} already exists. Overwrite it? (y/n)");
                    if (Console.ReadLine()?.ToLower() != "y")
                    {
                        Log.Information("Operation canceled by user.");
                        Console.WriteLine("Operation canceled by user.");
                        return;
                    }
                }

                var fileProcessor = new FileProcessor();
                await fileProcessor.SortFileAsync(inputFilePath, outputFilePath);
            });

        await rootCommand.InvokeAsync(args);
    }
}
