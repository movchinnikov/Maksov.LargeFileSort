using System.Diagnostics;
using System.Text;
using Serilog;

namespace Maksov.LargeFileSort.GenerateApp;

public class FileGenerator
{
    private const int Mb = 1024 * 1024;
    private const string Separator = ". ";
    private static readonly int SeparatorBytes = Encoding.UTF8.GetByteCount(Separator);
    private static readonly int NewLineBytes = Encoding.UTF8.GetByteCount(Environment.NewLine);
    private static readonly int SpaceBytes = Encoding.UTF8.GetByteCount(" ");

    private const string LoremIpsum =
        @"Lorem ipsum dolor sit amet consectetur adipiscing elit Etiam sed felis massa Nulla in libero vel lacus 
fringilla feugiat Aenean ex mauris vestibulum at feugiat eu pretium ut nunc Vivamus vitae blandit mauris Nulla 
facilisi Curabitur elit urna vulputate a ullamcorper vitae tempor non felis Vivamus imperdiet tempus ex in eleifend 
orci rhoncus sed Duis porttitor pellentesque nulla sit amet viverra Vivamus ut eros metus Integer venenatis orci 
quis pretium condimentum In hac habitasse platea dictumst Nam consectetur gravida ante ac ornare elit euismod non 
Nunc volutpat ipsum quam eu blandit metus cursus fringilla Donec bibendum sapien at dignissim pulvinar enim neque 
condimentum massa non hendrerit metus nibh nec eros In tristique quis erat sit amet pretium Duis nisl quam 
dignissim at pretium non consectetur quis urna Quisque tempus mauris nibh Duis ornare neque nec bibendum mollis 
tortor mi faucibus velit at dictum nulla nulla in nisl Integer tincidunt porttitor nisl sit amet aliquet In nisi 
risus molestie sit amet ullamcorper ac consectetur non diam Nullam ac purus ac eros tincidunt condimentum Fusce 
eget vestibulum quam at tristique velit Vivamus porttitor sodales nisl ut consequat Donec imperdiet imperdiet 
rhoncus Vivamus eleifend enim nec diam pellentesque id pretium ligula pellentesque";
        
    private static readonly string[] LoremIpsumArray;

    static FileGenerator()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        LoremIpsumArray = LoremIpsum.Replace("\n", "").Split(' ');
    }
        
    public FileGenerator() { }

    public virtual async Task GenerateAsync(string outputFileNamePath, float desireFileSizeGb, int maxPartFileSizeMb)
    {
        var stopwatch = Stopwatch.StartNew();
        Log.Information($"Start generating a {desireFileSizeGb}GB file");

        string[] partFiles = [];
                
        try
        {
            partFiles = await GeneratePartFilesAsync(desireFileSizeGb, maxPartFileSizeMb);
            MergeFiles(outputFileNamePath, partFiles);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {ErrorMessage}. {StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
        finally
        {
            foreach (var file in partFiles.Where(File.Exists))
            {
                try { File.Delete(file); } catch { /* Ignore file delete errors */ }
            }
                    
            stopwatch.Stop();
            Log.Information($"Completed generating a {desireFileSizeGb}GB file");
            var elapsedTimeSpan = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            Log.Debug($"Total time: {elapsedTimeSpan:c}");
        }
    }

    protected async Task<string[]> GeneratePartFilesAsync(float desireFileSizeGb, int maxPartFileSizeMb)
    {
        var partSizeBytes = maxPartFileSizeMb * Mb;
        var numberOfParts = (int)Math.Ceiling(desireFileSizeGb * 1024 / maxPartFileSizeMb);
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var partFiles = new string[numberOfParts];
        var tasks = new Task[numberOfParts];
        Log.Debug("Starting generation task");
        for (var i = 0; i < numberOfParts; i++)
        {
            await semaphore.WaitAsync();
            var tempFilePath = Path.GetTempFileName();
            var i1 = i + 1;  // To capture the current loop index correctly in the lambda below
            partFiles[i] = tempFilePath;
            tasks[i] = Task.Run(async () =>
            {
                Log.Debug("Starting generation task of part {part} out of {numberOfParts}", i1, numberOfParts);
                await InternalGeneratePartFilesAsync(tempFilePath, partSizeBytes);
                Log.Debug("Completed generation task of part {part} out of {numberOfParts}", i1, numberOfParts);
                semaphore.Release();
            });
        }
            
        await Task.WhenAll(tasks);
        Log.Debug("Completed generation task");

        return partFiles;
    }
        
    protected async Task InternalGeneratePartFilesAsync(string filePath, long partSize)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(fileStream, new UTF8Encoding(false, true), Mb);
        var random = new Random();
        var totalBytesWritten = 0L;
        var stringBuilder = new StringBuilder(1024);
            
        while (totalBytesWritten < partSize)
        {
            var number = random.Next(1, 9999).ToString();
            stringBuilder.Clear();
            stringBuilder.Append(number);
            stringBuilder.Append(Separator);
            totalBytesWritten += Encoding.UTF8.GetByteCount(number) + SeparatorBytes + NewLineBytes;
            var countWord = random.Next(1, 4);
            for (var i = 1; i <= countWord; i++)
            {
                var wordIndex = random.Next(0, LoremIpsumArray.Length);
                var word = LoremIpsumArray[wordIndex];
                stringBuilder.Append(word);
                totalBytesWritten += Encoding.UTF8.GetByteCount(word);
                if (i == countWord) continue;
                stringBuilder.Append(' ');
                totalBytesWritten += SpaceBytes;
            }
            stringBuilder.Append(Environment.NewLine);
            await writer.WriteAsync(stringBuilder);
                
            if (totalBytesWritten > partSize) break;
        }
        await writer.FlushAsync();
    }

    protected void MergeFiles(string outputFilePath, string[] partFilePaths)
    {
        using var outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        var i = 0;
        Log.Debug("Starting merging task");
        foreach (var partFilePath in partFilePaths)
        {
            using (var partFileStream = new FileStream(partFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Log.Debug("Starting merging task of part {part} out of {numberOfParts}", i, partFilePaths.Length);
                partFileStream.CopyTo(outputFileStream);
                Log.Debug("Completed merging task of part {part} out of {numberOfParts}", i, partFilePaths.Length);
            }
            File.Delete(partFilePath);
            i++;
        }
        Log.Debug("Completed merging task");
    }
}