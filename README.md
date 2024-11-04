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

## Example Output