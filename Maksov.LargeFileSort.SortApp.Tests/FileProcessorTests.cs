using NUnit.Framework;
using Xunit;
using Assert = Xunit.Assert;

namespace Maksov.LargeFileSort.SortApp.Tests;

public class FileProcessorTests
{
    private readonly TestableFileProcessor _fileProcessor;

    public FileProcessorTests()
    {
        _fileProcessor = new TestableFileProcessor();
    }

    [Fact]
    public async Task SplitFilePartsAsync_ShouldCreateParts_WhenFileIsSplit()
    {
        var inputFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(inputFilePath, "1. A\n2. A\n3. B\n1. C\n6. D\n5. E\n4. F");

        var parts = await _fileProcessor.SplitFilePartsAsync(inputFilePath, 0.0001f); // Small chunk size for testing

        var enumerable = parts as string[] ?? parts.ToArray();
        Assert.True(enumerable.Count() > 1, "The file should be split into multiple parts.");
        foreach (var part in enumerable)
        {
            Assert.True(File.Exists(part), "Part file should exist.");
            File.Delete(part);
        }

        File.Delete(inputFilePath);
    }

    [Fact]
    public async Task SortFilesAsync_ShouldSortEachFilePart_WhenCalled()
    {
        var tempFilePath1 = Path.GetTempFileName();
        var tempFilePath2 = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath1, "1. C\n2. A\n3. B");
        await File.WriteAllTextAsync(tempFilePath2, "4. F\n5. E\n6. D\n1. A");

        await _fileProcessor.SortFilesAsync(new[] { tempFilePath1, tempFilePath2 });

        Assert.Equal("2. A\n3. B\n1. C\n", await File.ReadAllTextAsync(tempFilePath1));
        Assert.Equal("1. A\n6. D\n5. E\n4. F\n", await File.ReadAllTextAsync(tempFilePath2));

        File.Delete(tempFilePath1);
        File.Delete(tempFilePath2);
    }

    [Fact]
    public async Task MergeFilesAsync_ShouldCombineSortedPartsIntoOneFile_WhenCalled()
    {
        var outputFilePath = Path.GetTempFileName();
        var tempFilePath1 = Path.GetTempFileName();
        var tempFilePath2 = Path.GetTempFileName();
        
        await File.WriteAllTextAsync(tempFilePath1, "2. A\n3. B\n1. C");
        await File.WriteAllTextAsync(tempFilePath2, "1. A\n6. D\n5. E\n4. F");

        _fileProcessor.MergeFilesAsync(new[] { tempFilePath1, tempFilePath2 }, outputFilePath);

        Assert.True(File.Exists(outputFilePath), "Output file should be created.");
        Assert.Equal("1. A\n2. A\n3. B\n1. C\n6. D\n5. E\n4. F\n", File.ReadAllText(outputFilePath));

        File.Delete(outputFilePath);
        File.Delete(tempFilePath1);
        File.Delete(tempFilePath2);
    }
}