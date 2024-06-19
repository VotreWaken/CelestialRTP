//----------------------------------------------------------------------------
// File Name: Win32Tests.cs
// 
// Description: 
// WinSoundTests is responsible for UnitTesting Win32.cs Class
//
//
// Author(s):
// Egor Waken
//
// History:
// 08 May 2024	Egor Waken       Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//----------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace AudioWaveOutUnitTest.Tests
{
    [TestFixture]
    public class Win32Tests : BaseSetup
    {
        [Test]
        public void Test_waveOutGetNumDevs_ReturnsNonNegativeValue()
        {
            // Arrange
            uint numDevices;

            // Act
            numDevices = Win32.waveOutGetNumDevs();

            // Assert
            Assert.That(numDevices, Is.GreaterThanOrEqualTo(0), "Number of output devices should be non-negative");
        }

        [Test]
        public void Test_waveOutGetNumDevs_NoError()
        {
            // Arrange
            uint numDevices;

            // Act
            numDevices = Win32.waveOutGetNumDevs();

            // Assert
            Assert.That(numDevices, Is.Not.EqualTo((uint)Win32.MMSYSERR.MMSYSERR_NOERROR), "waveOutGetNumDevs failed with error");
        }

        /*
        [Test]
        public void Test_waveOutOpenAndClose()
        {
            IntPtr deviceHandle = IntPtr.Zero;
            Win32.MMRESULT result = Win32.waveOutOpen(out deviceHandle, Win32.WAVE_MAPPER, new Win32.WAVEFORMATEX(), IntPtr.Zero, 0, Win32.CALLBACK_NULL);
            Assert.That((int)result, Is.EqualTo(0), "waveOutOpen failed");
            Assert.That(deviceHandle, Is.Not.EqualTo(IntPtr.Zero), "Device handle should not be zero");

            result = Win32.waveOutClose(deviceHandle);
            Assert.That((int)result, Is.EqualTo(0), "waveOutClose failed");
        }

        
        [Test]
        public void Test_waveOutPrepareUnprepareHeader()
        {
            IntPtr deviceHandle = IntPtr.Zero; // Replace with a valid device handle
            Win32.WAVEHDR header = new Win32.WAVEHDR();
            header.lpData = IntPtr.Zero; // Replace with valid audio data buffer
            header.dwBufferLength = 0; // Replace with valid buffer length

            int result = Win32.waveOutPrepareHeader(deviceHandle, ref header, Marshal.SizeOf(typeof(Win32.WAVEHDR)));
            Assert.That(result, Is.EqualTo(0), "waveOutPrepareHeader failed");

            result = Win32.waveOutUnprepareHeader(deviceHandle, ref header, Marshal.SizeOf(typeof(Win32.WAVEHDR)));
            Assert.That(result, Is.EqualTo(0), "waveOutUnprepareHeader failed");
        }

        [Test]
        public void Test_waveOutWrite()
        {
            IntPtr deviceHandle = IntPtr.Zero; // Replace with a valid device handle
            Win32.WAVEHDR header = new Win32.WAVEHDR();
            header.lpData = IntPtr.Zero; // Replace with valid audio data buffer
            header.dwBufferLength = 0; // Replace with valid buffer length

            int result = Win32.waveOutWrite(deviceHandle, ref header, Marshal.SizeOf(typeof(Win32.WAVEHDR)));
            Assert.That(result, Is.EqualTo(0), "waveOutWrite failed");
        }
        */


    }
}
