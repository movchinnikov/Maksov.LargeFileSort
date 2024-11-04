namespace Maksov.LargeFileSort.SortApp.Tests;

public class TestableFileProcessor : FileProcessor
{
    public new async Task<IEnumerable<string>> SplitFilePartsAsync(string inputFilePath, float chunkSizeMb = 1.0f)
    {
        return await base.SplitFilePartsAsync(inputFilePath, chunkSizeMb);
    }

    public new async Task SortFilesAsync(string[] partFiles)
    {
        await base.SortFilesAsync(partFiles);
    }

    public new void MergeFilesAsync(string[] inputFilePaths, string outputFilePath)
    {
        base.MergeFilesAsync(inputFilePaths, outputFilePath);
    }
}