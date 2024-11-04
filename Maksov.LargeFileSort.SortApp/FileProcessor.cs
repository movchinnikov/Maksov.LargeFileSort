using System.Diagnostics;
using System.Text;
using Serilog;

namespace Maksov.LargeFileSort.SortApp;

public class FileProcessor
{
    private const int Mb = 1024 * 1024;
    private static readonly SemaphoreSlim? Semaphore = new(Environment.ProcessorCount);

    public async Task SortFileAsync(string inputFilePath, string outputFilePath)
    {
        var partFilesArr = Array.Empty<string>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Information($"Starting sorting of file {inputFilePath}");
            var partFiles = await SplitFilePartsAsync(inputFilePath);
            partFilesArr = partFiles.ToArray();

            if (partFilesArr.Any())
            {
                await SortFilesAsync(partFilesArr);
                MergeFilesAsync(partFilesArr, outputFilePath);
            }
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {ErrorMessage}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
        finally
        {
            foreach (var file in partFilesArr.Where(File.Exists))
            {
                try { File.Delete(file); } catch { /* Ignore file delete errors */ }
            }

            stopwatch.Stop();
            Log.Information($"Completed sorting of file {inputFilePath}");
            Log.Debug($"Total time taken: {stopwatch.Elapsed:c}");
        }
    }
    
    protected async Task<IEnumerable<string>> SplitFilePartsAsync(string inputFilePath, float chunkSizeMb = 1.0f)
    {
        var chunkIndex = 0;
        var files = new List<string>();

        await using var inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
        var bufferSize = (int)Math.Ceiling(chunkSizeMb * Mb);
        var buffer = new byte[bufferSize];
        int bytesRead;
        var totalChunks = (inputFileStream.Length / bufferSize) + 1;

        var guid = Guid.NewGuid().ToString("N");

        while ((bytesRead = inputFileStream.Read(buffer, 0, bufferSize)) > 0)
        {
            Log.Information($"Splitting chunk {chunkIndex + 1} of {totalChunks}");
            var lastNewLineIndex = Array.LastIndexOf(buffer, (byte)10, bytesRead - 1);
            var lengthToWrite = lastNewLineIndex != -1 ? lastNewLineIndex : bytesRead;
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"chunk_{guid}_{chunkIndex}.tmp");
            files.Add(tempFilePath);

            await using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                tempFileStream.Write(buffer, 0, lengthToWrite);
                Log.Information($"Finished writing chunk {chunkIndex + 1} of {totalChunks}: {tempFilePath}");
            }

            chunkIndex++;

            if (lastNewLineIndex == -1) continue;
            long bytesToRewind = bytesRead - lastNewLineIndex - 1;
            inputFileStream.Seek(-bytesToRewind, SeekOrigin.Current);
        }

        return files;
    }

    protected async Task SortFilesAsync(string[] partFiles)
    {
        Log.Debug("Starting sorting of all file parts");

        var tasks = new Task[partFiles.Length];
        for (var i = 0; i < partFiles.Length; i++)
        {
            var fileIndex = i;
            tasks[i] = Task.Run(() =>
            {
                Semaphore!.Wait();
                try
                {
                    Log.Information($"Starting sort of file part {fileIndex + 1} of {partFiles.Length}: {partFiles[fileIndex]}");
                    InternalSortFile(partFiles[fileIndex]);
                }
                finally
                {
                    Semaphore.Release();
                }
                Log.Debug($"Completed sorting of file part {fileIndex + 1} of {partFiles.Length}: {partFiles[fileIndex]}");
            });
        }

        await Task.WhenAll(tasks);
        Log.Debug("Completed sorting of all file parts");
    }

    protected void MergeFilesAsync(string[] inputFilePaths, string outputFilePath)
    {
        var maxChunkSize = 10;
        var tempFiles = new List<string>(inputFilePaths);

        Log.Information("Starting merging process");

        while (tempFiles.Count > 1)
        {
            var mergedFiles = new List<string>();
            var tasks = new List<Task>();

            for (int i = 0; i < tempFiles.Count; i += maxChunkSize)
            {
                var chunk = tempFiles.Skip(i).Take(maxChunkSize).ToArray();
                var tempOutputFile = Path.GetTempFileName();
                mergedFiles.Add(tempOutputFile);

                if (tasks.Count >= Environment.ProcessorCount)
                {
                    Task.WaitAny(tasks.ToArray());
                    tasks.RemoveAll(t => t.IsCompleted);
                }

                var i1 = i;
                var tempFilesCount = tempFiles.Count;
                tasks.Add(Task.Run(() =>
                {
                    Log.Information($"Starting merge {i1} files out of {tempFilesCount} files");
                    InternalMergeFiles(chunk, tempOutputFile);
                    Log.Information($"Completed merge {i1} files out of {tempFilesCount} files");
                }));
            }

            Task.WaitAll(tasks.ToArray());
            tempFiles = mergedFiles;
            Log.Debug("Completed a merging pass, reducing the number of temporary files");
        }

        if (tempFiles.Count != 1) return;
        
        if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
        File.Move(tempFiles[0], outputFilePath, true);
        Log.Information($"Final merge completed. Output file created at: {outputFilePath}");
    }

    private void InternalSortFile(string filePath)
    {
        var tempFilePath = Path.GetTempFileName();
        using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var buffer = new byte[inputStream.Length];
        inputStream.Read(buffer, 0, buffer.Length);

        ReadOnlySpan<byte> contentSpan = buffer.AsSpan();
        var newline = (byte)'\n';
        var carriageReturn = (byte)'\r';

        var start = 0;
        var lines = new List<(int start, int length)>();

        for (var i = 0; i < contentSpan.Length; i++)
        {
            if (contentSpan[i] != newline) continue;
            lines.Add((start, i - start));
            start = i + 1;
            if (i > 0 && contentSpan[i - 1] == carriageReturn)
            {
                start++;
            }
        }

        if (start >= contentSpan.Length) return;
        lines.Add((start, -1));

        lines.Sort((a, b) =>
        {
            ReadOnlySpan<byte> contentSpan = buffer.AsSpan();
            var lineA = a.length != -1 ? contentSpan.Slice(a.start, a.length).ToArray() : contentSpan[a.start..].ToArray();
            var lineB = b.length != -1 ? contentSpan.Slice(b.start, b.length).ToArray() : contentSpan[b.start..].ToArray();

            return StringComparison(lineA, lineB);
        });

        using (var outputStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
        {
            foreach (var lineVal in lines)
            {
                ReadOnlySpan<byte> contentSpan1 = buffer.AsSpan();
                var line = lineVal.length != -1
                    ? contentSpan.Slice(lineVal.start, lineVal.length).ToArray()
                    : contentSpan[lineVal.start..].ToArray();
                outputStream.Write(line);
                outputStream.Write("\n"u8);
            }

            outputStream.Flush();
        }

        File.Delete(filePath);
        File.Move(tempFilePath, filePath);
    }

    private void InternalMergeFiles(string[] inputFilePaths, string outputFilePath)
    {
        var priorityQueue = new SortedList<byte[], List<int>>(Comparer<byte[]>.Create(StringComparison));

        var fileStreams = inputFilePaths.Select(filePath => new FileStream(filePath, FileMode.Open, FileAccess.Read))
            .ToArray();
        var readers = fileStreams.Select(fs => new StreamReader(fs)).ToArray();

        try
        {
            for (var i = 0; i < readers.Length; i++)
            {
                if (!TryReadLine(readers[i], out var line)) continue;
                AddToPriorityQueue(priorityQueue, line, i);
            }

            using var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);

            while (priorityQueue.Count > 0)
            {
                var min = priorityQueue.First();
                var line = min.Key;
                var fileIndex = min.Value[0];

                min.Value.RemoveAt(0);
                if (min.Value.Count == 0)
                {
                    priorityQueue.RemoveAt(0);
                }

                outputStream.Write(line);
                outputStream.Write("\n"u8);


                if (outputStream.Length >= Mb) outputStream.Flush();

                if (TryReadLine(readers[fileIndex], out var nextLine))
                {
                    AddToPriorityQueue(priorityQueue, nextLine, fileIndex);
                }
            }

            outputStream.Flush();
        }
        finally
        {
            for (var i = 0; i < readers.Length; i++)
            {
                readers[i].Dispose();
                fileStreams[i].Dispose();
                File.Delete(inputFilePaths[i]);
            }
        }
    }

    private void AddToPriorityQueue(SortedList<byte[], List<int>> priorityQueue, byte[] line, int fileIndex)
    {
        if (!priorityQueue.TryGetValue(line, out var fileIndices))
        {
            fileIndices = new List<int>();
            priorityQueue[line] = fileIndices;
        }

        fileIndices.Add(fileIndex);
    }

    private static int StringComparison(ReadOnlySpan<byte> lineA, ReadOnlySpan<byte> lineB)
    {
        const byte period = (byte)'.';

        var numberPartA = ReadOnlySpan<byte>.Empty;
        var stringPartA = ReadOnlySpan<byte>.Empty;
        var numberPartB = ReadOnlySpan<byte>.Empty;
        var stringPartB = ReadOnlySpan<byte>.Empty;

        var periodIndexA = lineA.IndexOf(period);
        if (periodIndexA > 0)
        {
            numberPartA = lineA[..periodIndexA];
            stringPartA = lineA[(periodIndexA + 2)..];
        }

        var periodIndexB = lineB.IndexOf(period);
        if (periodIndexB > 0)
        {
            numberPartB = lineB[..periodIndexB];
            stringPartB = lineB[(periodIndexB + 2)..];
        }

        var stringComparison = CompareLexicographically(stringPartA, stringPartB);
        return stringComparison != 0 ? stringComparison : ParseInt(numberPartA).CompareTo(ParseInt(numberPartB));
    }

    private static int StringComparison(byte[] lineA, byte[] lineB)
    {
        return StringComparison(lineA.AsSpan(), lineB.AsSpan());
    }

    private static int CompareLexicographically(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var minLength = Math.Min(a.Length, b.Length);
        for (var i = 0; i < minLength; i++)
        {
            var diff = a[i].CompareTo(b[i]);
            if (diff != 0) return diff;
        }
        return a.Length.CompareTo(b.Length);
    }

    private static int ParseInt(ReadOnlySpan<byte> span)
    {
        var result = 0;
        foreach (var b in span)
        {
            result = result * 10 + (b - (byte)'0');
        }
        return result;
    }

    private static bool TryReadLine(StreamReader reader, out byte[] line)
    {
        var lineString = reader.ReadLine();
        line = lineString != null ? Encoding.UTF8.GetBytes(lineString) : [];
        return lineString != null;
    }
}