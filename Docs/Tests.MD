# Project Structure and Documentation

## Description:

This documentation provides a comprehensive overview of each file's purpose, how logs are managed, the role of 
each test, and the packages involved. For further details, refer to the source code and comments within each file.

## Packages Used

- **NUnit**: Provides the testing framework.
    - **Namespace**: `global using NUnit.Framework;`
    - **Install**: `dotnet add package NUnit`
- **NLog**: Manages logging within the application and tests.
    - **Namespace**: `global using NLog;`, `global using NLog.Config;`
    - **Install**: `dotnet add package NLog`, `dotnet add package NLog.Config`, `dotnet add package NLog.Targets`
- **Moq**: Used for mocking dependencies during tests.
    - **Namespace**: `global using Moq;`
    - **Install**: `dotnet add package Moq`

### Usage

#### Build and Run Tests

To build and run the tests from the console:

1. **Navigate to the test project directory**:
    
    `cd MyApplication.Tests`
    
2. **Build the test project**:
    
    `dotnet build`
    
3. **Run the tests**:
    
    `dotnet test`
    

#### Logging

- **Console Logging**: Logs are output to the console during test execution.
- **File Logging**: Logs are written to `log.txt` located in the directory specified by `${basedir}../../../log.txt`. Ensure the directory exists or adjust the path in `nlog.config`.


### 1. **BaseSetup.cs**

- **Description**:
    - `BaseSetup` is responsible for setting up and tearing down unit tests. It initializes the logging configuration using NLog and ensures proper cleanup after tests.
- **Author**: Egor Waken
- **History**: Created on 07 May 2024.
- **License**: BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.

#### Configuration Methods

- **SetUp**
    - **Purpose**: Initializes the logging configuration using the NLog configuration file before any tests are run.
    - **Details**: Sets up NLog configuration to log to both the console and a file.
- **TearDown**
    - **Purpose**: Handles any necessary cleanup after all tests are executed.

### 2. **NLog Configuration (nlog.config)**

- **Description**:
    - Defines the configuration for NLog, specifying log targets, formats, and rules.
- **Format**: XML.
- **Targets**:
    - **Console**: Logs are output to the console.
    - **File**: Logs are written to a file named `log.txt` located in the project's base directory.
- **Rules**:
    - Logs from all levels (Trace to Fatal) are written to both the console and the log file.


### 3. **Win32Tests.cs**

- **Description**:
    - `Win32Tests` is responsible for unit testing the methods provided by the `Win32.cs` class. This file focuses on testing functions related to audio output device management, specifically ensuring correct handling of device enumeration and management.
- **Author**: Egor Waken
- **History**: Created on 08 May 2024.
- **License**: BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.

#### Tests Description

- **Test_waveOutGetNumDevs_ReturnsNonNegativeValue**
    - **Purpose**: Verifies that `Win32.waveOutGetNumDevs` returns a non-negative number, which indicates a valid count of audio output devices.
    - **Expected Result**: The number of devices should be greater than or equal to 0.
- **Test_waveOutGetNumDevs_NoError**
    - **Purpose**: Checks that `Win32.waveOutGetNumDevs` does not return an error code, ensuring proper function execution without errors.
    - **Expected Result**: The return value should not be `Win32.MMSYSERR.MMSYSERR_NOERROR`.

#### Commented Tests

- **Test_waveOutOpenAndClose**
    - **Purpose**: Ensures that opening and closing an audio output device works correctly.
    - **Expected Result**: Device should be successfully opened and closed without errors.
- **Test_waveOutPrepareUnprepareHeader**
    - **Purpose**: Verifies the preparation and unpreparation of a WAVEHDR structure for audio output.
    - **Expected Result**: Both preparation and unpreparation should return 0, indicating success.
- **Test_waveOutWrite**
    - **Purpose**: Tests writing audio data to an output device.
    - **Expected Result**: The `waveOutWrite` function should execute successfully, indicated by a return value of 0.

### 4. **WinSoundTests.cs**

- **Description**:
    - `WinSoundTests` is responsible for unit testing the `WinSound.cs` class, which manages audio playback and recording devices.
- **Author**: Egor Waken
- **History**: Created on 07 May 2024.
- **License**: BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.

#### Tests Description

- **GetPlaybackNamesNotEmpty**
    - **Purpose**: Checks that the list of playback device names is not empty.
    - **Expected Result**: The list should not be null or empty.
- **GetRecordingNamesNotEmpty**
    - **Purpose**: Verifies that the list of recording device names is not empty.
    - **Expected Result**: The list should not be null or empty.
- **GetWaveInDeviceIdByNameNonExistingNameReturnsWaveMapper**
    - **Purpose**: Ensures that querying a non-existing device name returns `WAVE_MAPPER`.
    - **Expected Result**: The device ID should be `Win32.WAVE_MAPPER`.
- **FlagToStringReturnsExpectedString**
    - **Purpose**: Tests the conversion of flags to string representation.
    - **Expected Result**: The string should match the expected flag description.

#### Commented Test

- **GetWaveInDeviceIdByNameExistingNameReturnsValidId**
    - **Purpose**: Checks that querying an existing device name returns a valid device ID.
    - **Expected Result**: Device ID should not be -1 for a known device name.
