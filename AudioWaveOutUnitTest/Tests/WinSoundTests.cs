//----------------------------------------------------------------------------
// File Name: WinSoundTests.cs
// 
// Description: 
// WinSoundTests is responsible for UnitTesting WinSound.cs Class
//
//
// Author(s):
// Egor Waken
//
// History:
// 07 May 2024	Egor Waken       Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//----------------------------------------------------------------------------

namespace AudioWaveOutUnitTest.Tests
{
    [TestFixture]
    public class WinSoundTests : BaseSetup
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        [Test]
        public void GetPlaybackNamesNotEmpty()
        {
            // Arrange: Prepare the test by obtaining playback device names
            List<string> playbackNames = WinSound.GetPlaybackNames();

            // Act: Perform the action (calling the method under test)
            // Assert: Verify the expected behavior
            Assert.IsNotNull(playbackNames, "Playback names list should not be null");
            Assert.IsNotEmpty(playbackNames, "Playback names list should not be empty");

            Logger.Info($"Pass GetPlaybackNames_NotEmpty Test");
        }

        [Test]
        public void GetRecordingNamesNotEmpty()
        {
            // Arrange: Prepare the test by obtaining recording device names
            List<string> recordingNames = WinSound.GetRecordingNames();

            // Act: Perform the action (calling the method under test)
            // Assert: Verify the expected behavior
            Assert.IsNotNull(recordingNames, "Recording names list should not be null");
            Assert.IsNotEmpty(recordingNames, "Recording names list should not be empty");

            Logger.Info($"Pass GetRecordingNames_NotEmpty Test");
        }

        /*
        [Test]
        public void GetWaveInDeviceIdByNameExistingNameReturnsValidId()
        {
            // Arrange: Prepare the test by specifying an existing device name
            string existingDeviceName = "Device A";

            // Emulate a list of available playback devices
            List<string> playbackNames = new List<string> { "Device A", "Device B" };


            // Create a mock for WinSound
            var mockWinSound = new Mock<WinSound>();

            // Configure the mock for the static method GetPlaybackNames()
            mockWinSound.Setup(w => WinSound.GetPlaybackNames()).Returns(playbackNames);

            // Act: Perform the action (calling the method under test)
            int deviceId = WinSound.GetWaveInDeviceIdByName(existingDeviceName);

            // Assert: Verify the expected behavior
            Assert.AreNotEqual(-1, deviceId, $"Device ID for '{existingDeviceName}' should not be -1");

            // Log test pass
            Logger.Info($"Pass GetWaveInDeviceIdByName_ExistingDeviceName_ReturnsValidId Test");
        }
        */

        [Test]
        public void GetWaveInDeviceIdByNameNonExistingNameReturnsWaveMapper()
        {
            // Arrange: Prepare the test by specifying a non-existing device name
            string nonExistingDeviceName = "NonExistingDevice";

            // Act: Perform the action (calling the method under test)
            int deviceId = WinSound.GetWaveInDeviceIdByName(nonExistingDeviceName);

            // Assert: Verify the expected behavior
            Assert.AreEqual(Win32.WAVE_MAPPER, deviceId, $"Device ID for '{nonExistingDeviceName}' should be WAVE_MAPPER");

            Logger.Info($"Pass GetWaveInDeviceIdByName_NonExistingName_ReturnsWaveMapper Test");
        }

        [Test]
        public void FlagToStringReturnsExpectedString()
        {
            // Arrange: Prepare the test by defining the flags
            var flags = Win32.WaveHdrFlags.WHDR_PREPARED | Win32.WaveHdrFlags.WHDR_DONE;

            // Act: Perform the action (calling the method under test)
            string result = WinSound.FlagToString(flags);

            // Assert: Verify the expected behavior
            Assert.AreEqual("PREPARED DONE ", result, "FlagToString result should match the expected string");

            Logger.Info($"Pass FlagToString_CorrectFlags_ReturnsExpectedString Test");
        }


    }
}