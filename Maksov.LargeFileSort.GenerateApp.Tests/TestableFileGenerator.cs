namespace Maksov.LargeFileSort.GenerateApp.Tests;

public class TestableFileGenerator : FileGenerator
{
    public new async Task<string[]> GeneratePartFilesAsync(float desireFileSizeGb, int maxPartFileSizeMb)
    {
        return await base.GeneratePartFilesAsync(desireFileSizeGb, maxPartFileSizeMb);
    }
    
    public new async Task InternalGeneratePartFilesAsync(string filePath, long partSize)
    {
        await base.InternalGeneratePartFilesAsync(filePath, partSize);
    }
    
    public new void MergeFiles(string outputFilePath, string[] partFilePaths)
    {
        base.MergeFiles(outputFilePath, partFilePaths);
    }
}