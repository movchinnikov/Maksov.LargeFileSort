using Xunit;
using Assert = Xunit.Assert;

namespace Maksov.LargeFileSort.GenerateApp.Tests;

public class FileGeneratorTests
{
    private readonly TestableFileGenerator _fileGenerator = new();

    [Fact]
    public async Task GenerateAsync_ShouldCreateOutputFile_WhenGivenValidParameters()
    {
        var outputFilePath = Path.GetTempFileName();
        const float fileSizeGb = 0.01f; // Small size for testing
        const int maxPartSizeMb = 1;

        await _fileGenerator.GenerateAsync(outputFilePath, fileSizeGb, maxPartSizeMb);

        Assert.True(File.Exists(outputFilePath), "Output file should be created.");
        File.Delete(outputFilePath);
    }

    [Fact]
    public async Task GeneratePartFilesAsync_ShouldReturnCorrectNumberOfPartFiles_WhenGivenValidParameters()
    {
        const float fileSizeGb = 0.01f; // Small size for testing
        const int maxPartSizeMb = 1;
        var minFileCount = (int) Math.Ceiling(fileSizeGb * 1024 / maxPartSizeMb);

        var partFiles = await _fileGenerator.GeneratePartFilesAsync(fileSizeGb, maxPartSizeMb);

        Assert.True(partFiles.Length >= minFileCount, "Part files should be created.");
        foreach (var partFile in partFiles)
        {
            Assert.True(File.Exists(partFile), "Part file should exist.");
            File.Delete(partFile);
        }
    }

    [Fact]
    public async Task InternalGeneratePartFilesAsync_ShouldGenerateFileWithApproximateSize_WhenCalled()
    {
        var tempFilePath = Path.GetTempFileName();
        const long partSize = 1024; // 1KB for testing

        await _fileGenerator.InternalGeneratePartFilesAsync(tempFilePath, partSize);

        var fileInfo = new FileInfo(tempFilePath);
        Assert.True(fileInfo.Length >= partSize, "File should be approximately of the specified size.");
        File.Delete(tempFilePath);
    }

    [Fact]
    public void MergeFiles_ShouldCombinePartFilesIntoOneOutputFile()
    {
        var outputFilePath = Path.GetTempFileName();
        string[] partFiles =
        [
            Path.GetTempFileName(),
            Path.GetTempFileName()
        ];

        File.WriteAllText(partFiles[0], "Test data for part 1.");
        File.WriteAllText(partFiles[1], "Test data for part 2.");

        _fileGenerator.MergeFiles(outputFilePath, partFiles);

        Assert.True(File.Exists(outputFilePath), "Output file should be created.");
        Assert.Contains("Test data for part 1.", File.ReadAllText(outputFilePath));
        Assert.Contains("Test data for part 2.", File.ReadAllText(outputFilePath));

        File.Delete(outputFilePath);
        foreach (var partFile in partFiles)
        {
            if (File.Exists(partFile))
            {
                File.Delete(partFile);
            }
        }
    }
}