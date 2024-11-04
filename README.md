# File Generator Project

## Introduction

This project is designed to generate a large text file with configurable parameters, making it suitable for testing and data processing tasks. The utility allows users to specify the file's size, path, and partition size for efficient file generation.

## Task

The main objective of this project is to generate a text file of a specified size at a designated file path. The content format follows a structured format where each line is numbered and contains a unique string value, separated by ". ".

## Parameters

The project accepts the following parameters:

- **`--desireFileSizeGb` / `-s`**: The desired size of the generated file in gigabytes (GB). This parameter determines the total file size.  
  - *Default*: `1.0` GB

- **`--outputFileNamePath` / `-o`**: The file path and name where the generated file will be saved. This should be a valid path where the user has write permissions. The default format is `"GeneratedLargeFile_<timestamp>.txt"`, where `<timestamp>` is the current date and time.
  
- **`--maxPartFileSizeMb` / `-p`**: Sets the maximum size of each file part in megabytes (MB). This helps split the file into manageable parts, each with a maximum size.  
  - *Default*: `100` MB

- **`--verbose`**: Enables verbose logging for more detailed output during file generation.  
  - *Default*: `false`

## Usage

1. **Setting Parameters**: Configure the above parameters as needed when running the application.
2. **Execution**: Run the application to generate the file with the specified configurations.

### Example Command

To generate a file with custom parameters, you might use:
```bash
dotnet run -- --desireFileSizeGb 2.0 --outputFileNamePath "LargeFile.txt" --maxPartFileSizeMb 200 --verbose
```
## Example Output
1. First line content
2. Second line content
3. Third line content
...

# File Sorting Utility

## Introduction

This utility is designed to sort the contents of a large text file and save the sorted result to a specified output file. It allows you to specify the input and output file paths, with additional options for logging.

## Task

The main objective of this project is to sort the contents of a large file provided by the user and save the sorted result to a new file. The tool is designed to handle large files efficiently and provides options for verbose logging.

## Parameters

The utility accepts the following command-line parameters:

- **`--inputFilePath` / `-i`**: Specifies the path to the input file that contains the data to be sorted.  
  - *Required*: Yes

- **`--outputFilePath` / `-o`**: Defines the path and filename for the sorted output file. By default, the output filename format is `"SortedLargeFile_<timestamp>.txt"`, where `<timestamp>` represents the current date and time.

- **`--verbose`**: Enables verbose logging for additional information during the execution.  
  - *Default*: `false`

## Usage

1. **Setting Parameters**: Configure the above parameters as needed before running the application.
2. **Execution**: Run the application from the command line to start the sorting process.

### Example Command

To sort a file with custom parameters, you might use:
```bash
dotnet run -- --inputFilePath "LargeFile.txt" --outputFilePath "SortedFile.txt" --verbose
