//----------------------------------------------------------------------------
// File Name: Repeater.cs
// 
// Description: 
// Repeater is responsible for creating WaveIn/ WaveOut Headers, their memory
// allocation, and its release, and monitors changes in WaveIn / WaveOut Devices.
//
// Responsible for creating and behaving a stream for WaveIn / WaveOut devices,
// creating a playback buffer and copying and playing data from this buffer
//
//
// Author(s):
// Egor Waken
//
// History:
// 06 May 2024	Egor Waken       Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//----------------------------------------------------------------------------
using System.Runtime.InteropServices;

namespace AudioWaveOut
{
    unsafe public class Repeater
    {
        // Constructor
        public Repeater()
        {
            delegateWaveInProc = new Win32.DelegateWaveInProc(waveInProc);
            delegateWaveOutProc = new Win32.DelegateWaveOutProc(waveOutProc);
        }

        // Variables
        private LockerClass Locker = new LockerClass();
        private LockerClass LockerCopy = new LockerClass();
        private IntPtr hWaveIn = IntPtr.Zero;
        private IntPtr hWaveOut = IntPtr.Zero;
        private String WaveInDeviceName = "";
        private String WaveOutDeviceName = "";
        private bool IsWaveInOpened = false;
        private bool IsWaveOutOpened = false;
        private bool IsWaveInStarted = false;
        private bool IsThreadPlayWaveInRunning = false;
        private bool IsMute = false;
        private bool Stopped = false;
        private bool IsDataIncomming = false;
        private int SamplesPerSecond = 8000;
        private int BitsPerSample = 16;
        private int Channels = 1;
        private int BufferCount = 8;
        private int BufferSize = 1024;
        private Win32.WAVEHDR*[] WaveInHeaders;
        private Win32.WAVEHDR*[] WaveOutHeaders;
        private Win32.WAVEHDR* CurrentRecordedHeader;
        private Win32.DelegateWaveInProc delegateWaveInProc;
        private Win32.DelegateWaveOutProc delegateWaveOutProc;
        private System.Threading.Thread ThreadPlayWaveIn;
        private System.Threading.AutoResetEvent AutoResetEventDataRecorded = new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent AutoResetEventThreadPlayWaveInEnd = new System.Threading.AutoResetEvent(false);
        private Byte[] CopyDataBuffer;
        private GCHandle GCCopyDataBuffer;

        // Delegates And Events
        public delegate void DelegateStopped();
        public event DelegateStopped RepeaterStopped;

        // Started
        public bool Started
        {
            get
            {
                return IsWaveInStarted && IsWaveInOpened && IsWaveOutOpened && IsThreadPlayWaveInRunning;
            }
        }

        // IsMute
        public bool Mute
        {
            get
            {
                return IsMute;
            }
            set
            {
                IsMute = value;
            }
        }

        // CreateWaveInHeaders
        private bool CreateWaveInHeaders()
        {
            // Create buffer
            WaveInHeaders = new Win32.WAVEHDR*[BufferCount];
            int createdHeaders = 0;

            // For every buffer
            for (int i = 0; i < BufferCount; i++)
            {
                // Allocate headers
                WaveInHeaders[i] = (Win32.WAVEHDR*)Marshal.AllocHGlobal(sizeof(Win32.WAVEHDR));

                // Set header
                WaveInHeaders[i]->dwLoops = 0;
                WaveInHeaders[i]->dwUser = IntPtr.Zero;
                WaveInHeaders[i]->lpNext = IntPtr.Zero;
                WaveInHeaders[i]->reserved = IntPtr.Zero;
                WaveInHeaders[i]->lpData = Marshal.AllocHGlobal(BufferSize);
                WaveInHeaders[i]->dwBufferLength = (uint)BufferSize;
                WaveInHeaders[i]->dwBytesRecorded = 0;
                WaveInHeaders[i]->dwFlags = 0;

                // If the buffer could be prepared
                Win32.MMRESULT hr = Win32.waveInPrepareHeader(hWaveIn, WaveInHeaders[i], sizeof(Win32.WAVEHDR));
                if (hr == Win32.MMRESULT.MMSYSERR_NOERROR)
                {
                    // Add first header to recording
                    if (i == 0)
                    {
                        hr = Win32.waveInAddBuffer(hWaveIn, WaveInHeaders[i], sizeof(Win32.WAVEHDR));
                    }
                    createdHeaders++;
                }
            }

            // Ready
            return (createdHeaders == BufferCount);
        }

        // CreateWaveOutHeaders
        private bool CreateWaveOutHeaders()
        {
            // Create buffer
            WaveOutHeaders = new Win32.WAVEHDR*[BufferCount];
            int createdHeaders = 0;

            // For every buffer
            for (int i = 0; i < BufferCount; i++)
            {
                // Allocate headers
                WaveOutHeaders[i] = (Win32.WAVEHDR*)Marshal.AllocHGlobal(sizeof(Win32.WAVEHDR));

                // Set header
                WaveOutHeaders[i]->dwLoops = 0;
                WaveOutHeaders[i]->dwUser = IntPtr.Zero;
                WaveOutHeaders[i]->lpNext = IntPtr.Zero;
                WaveOutHeaders[i]->reserved = IntPtr.Zero;
                WaveOutHeaders[i]->lpData = Marshal.AllocHGlobal(BufferSize);
                WaveOutHeaders[i]->dwBufferLength = (uint)BufferSize;
                WaveOutHeaders[i]->dwBytesRecorded = 0;
                WaveOutHeaders[i]->dwFlags = 0;

                // If the buffer could be prepared
                Win32.MMRESULT hr = Win32.waveOutPrepareHeader(hWaveOut, WaveOutHeaders[i], sizeof(Win32.WAVEHDR));
                if (hr == Win32.MMRESULT.MMSYSERR_NOERROR)
                {
                    createdHeaders++;
                }
            }

            // Ready
            return (createdHeaders == BufferCount);
        }

        // FreeWaveInHeaders
        private void FreeWaveInHeaders()
        {
            try
            {
                if (WaveInHeaders != null)
                {
                    for (int i = 0; i < WaveInHeaders.Length; i++)
                    {
                        // Release handle
                        Win32.MMRESULT hr = Win32.waveInUnprepareHeader(hWaveIn, WaveInHeaders[i], sizeof(Win32.WAVEHDR));

                        // Wait until finished
                        int count = 0;
                        while (count <= 100 && (WaveInHeaders[i]->dwFlags & Win32.WaveHdrFlags.WHDR_INQUEUE) == Win32.WaveHdrFlags.WHDR_INQUEUE)
                        {
                            System.Threading.Thread.Sleep(20);
                            count++;
                        }

                        // When data is no longer in queue
                        if ((WaveInHeaders[i]->dwFlags & Win32.WaveHdrFlags.WHDR_INQUEUE) != Win32.WaveHdrFlags.WHDR_INQUEUE)
                        {
                            // Share data
                            if (WaveInHeaders[i]->lpData != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(WaveInHeaders[i]->lpData);
                                WaveInHeaders[i]->lpData = IntPtr.Zero;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
            }
        }

        // FreeWaveOutHeaders
        private void FreeWaveOutHeaders()
        {
            try
            {
                if (WaveOutHeaders != null)
                {
                    for (int i = 0; i < WaveOutHeaders.Length; i++)
                    {
                        // Release handles
                        Win32.MMRESULT hr = Win32.waveOutUnprepareHeader(hWaveOut, WaveOutHeaders[i], sizeof(Win32.WAVEHDR));

                        // Wait until finished playing
                        int count = 0;
                        while (count <= 100 && (WaveOutHeaders[i]->dwFlags & Win32.WaveHdrFlags.WHDR_INQUEUE) == Win32.WaveHdrFlags.WHDR_INQUEUE)
                        {
                            System.Threading.Thread.Sleep(20);
                            count++;
                        }

                        // When data is played
                        if ((WaveOutHeaders[i]->dwFlags & Win32.WaveHdrFlags.WHDR_INQUEUE) != Win32.WaveHdrFlags.WHDR_INQUEUE)
                        {
                            // Share data
                            if (WaveOutHeaders[i]->lpData != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(WaveOutHeaders[i]->lpData);
                                WaveOutHeaders[i]->lpData = IntPtr.Zero;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
            }
        }

        // StartThreadRecording
        private void StartThreadPlayWaveIn()
        {
            if (Started == false)
            {
                ThreadPlayWaveIn = new System.Threading.Thread(new System.Threading.ThreadStart(OnThreadPlayWaveIn));
                IsThreadPlayWaveInRunning = true;
                ThreadPlayWaveIn.Name = "PlayWaveIn";
                ThreadPlayWaveIn.Priority = System.Threading.ThreadPriority.Highest;
                ThreadPlayWaveIn.Start();
            }
        }

        // OpenWaveIn
        private bool OpenWaveIn()
        {
            if (hWaveIn == IntPtr.Zero)
            {
                // If not already open
                if (IsWaveInOpened == false)
                {
                    // Determine format
                    Win32.WAVEFORMATEX waveFormatEx = new Win32.WAVEFORMATEX();
                    waveFormatEx.wFormatTag = (ushort)Win32.WaveFormatFlags.WAVE_FORMAT_PCM;
                    waveFormatEx.nChannels = (ushort)Channels;
                    waveFormatEx.nSamplesPerSec = (ushort)SamplesPerSecond;
                    waveFormatEx.wBitsPerSample = (ushort)BitsPerSample;
                    waveFormatEx.nBlockAlign = (ushort)((waveFormatEx.wBitsPerSample * waveFormatEx.nChannels) >> 3);
                    waveFormatEx.nAvgBytesPerSec = (uint)(waveFormatEx.nBlockAlign * waveFormatEx.nSamplesPerSec);

                    // Determine WaveIn device
                    int deviceId = WinSound.GetWaveInDeviceIdByName(WaveInDeviceName);

                    // Open WaveIn device
                    Win32.MMRESULT hr = Win32.waveInOpen(ref hWaveIn, deviceId, ref waveFormatEx, delegateWaveInProc, 0, (int)Win32.WaveProcFlags.CALLBACK_FUNCTION);

                    // If not successful
                    if (hWaveIn == IntPtr.Zero)
                    {
                        IsWaveInOpened = false;
                        return false;
                    }

                    // Lock handle
                    GCHandle.Alloc(hWaveIn, GCHandleType.Pinned);
                }
            }

            IsWaveInOpened = true;
            return true;
        }

        // OpenWaveOut
        private bool OpenWaveOut()
        {
            if (hWaveOut == IntPtr.Zero)
            {
                // If not already open
                if (IsWaveOutOpened == false)
                {
                    // Determine format
                    Win32.WAVEFORMATEX waveFormatEx = new Win32.WAVEFORMATEX();
                    waveFormatEx.wFormatTag = (ushort)Win32.WaveFormatFlags.WAVE_FORMAT_PCM;
                    waveFormatEx.nChannels = (ushort)Channels;
                    waveFormatEx.nSamplesPerSec = (ushort)SamplesPerSecond;
                    waveFormatEx.wBitsPerSample = (ushort)BitsPerSample;
                    waveFormatEx.nBlockAlign = (ushort)((waveFormatEx.wBitsPerSample * waveFormatEx.nChannels) >> 3);
                    waveFormatEx.nAvgBytesPerSec = (uint)(waveFormatEx.nBlockAlign * waveFormatEx.nSamplesPerSec);

                    // Determine WaveOut device
                    int deviceId = WinSound.GetWaveOutDeviceIdByName(WaveOutDeviceName);

                    // Open WaveIn device
                    Win32.MMRESULT hr = Win32.waveOutOpen(ref hWaveOut, deviceId, ref waveFormatEx, delegateWaveOutProc, 0, (int)Win32.WaveProcFlags.CALLBACK_FUNCTION);

                    // If not successful
                    if (hr != Win32.MMRESULT.MMSYSERR_NOERROR)
                    {
                        IsWaveOutOpened = false;
                        return false;
                    }

                    // Lock handle
                    GCHandle.Alloc(hWaveOut, GCHandleType.Pinned);
                }
            }

            IsWaveOutOpened = true;
            return true;
        }

        // Start
        public bool Start(string waveInDeviceName, string waveOutDeviceName, int samplesPerSecond, int bitsPerSample, int channels, int bufferCount, int bufferSize)
        {
            try
            {
                lock (Locker)
                {
                    // If the thread is still running
                    if (IsThreadPlayWaveInRunning)
                    {
                        // Wait until thread ends
                        IsThreadPlayWaveInRunning = false;
                        AutoResetEventDataRecorded.Set();
                        AutoResetEventThreadPlayWaveInEnd.WaitOne(5000);
                    }

                    // If not already started
                    if (Started == false)
                    {

                        // take over data
                        WaveInDeviceName = waveInDeviceName;
                        WaveOutDeviceName = waveOutDeviceName;
                        SamplesPerSecond = samplesPerSecond;
                        BitsPerSample = bitsPerSample;
                        Channels = channels;
                        BufferCount = bufferCount;
                        BufferSize = bufferSize;
                        CopyDataBuffer = new Byte[BufferSize];
                        GCCopyDataBuffer = GCHandle.Alloc(CopyDataBuffer, GCHandleType.Pinned);

                        // WaveOut
                        if (StartWaveOut())
                        {
                            // WaveIn
                            return StartWaveIn();
                        }
                        // Error opening WaveOut
                        return false;
                    }

                    // Repeater is already running
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Start | {0}", ex.Message));
                return false;
            }
        }

        // ChangeWaveIn
        public bool ChangeWaveIn(string waveInDeviceName)
        {
            try
            {
                // Change
                this.WaveInDeviceName = waveInDeviceName;

                // Restart
                if (Started)
                {
                    CloseWaveIn();
                    return StartWaveIn();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("ChangeWaveIn() | {0}", ex.Message));
                return false;
            }
        }

        // ChangeWaveOut
        public bool ChangeWaveOut(string waveOutDeviceName)
        {
            try
            {
                // Change
                this.WaveOutDeviceName = waveOutDeviceName;

                // Restart
                if (Started)
                {
                    CloseWaveOut();
                    return StartWaveOut();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("ChangeWaveOut() | {0}", ex.Message));
                return false;
            }
        }

        // StartWaveIn
        private bool StartWaveIn()
        {
            // If WaveIn could become
            if (OpenWaveIn())
            {
                // If all buffers could be created
                if (CreateWaveInHeaders())
                {
                    // When recording could start
                    Win32.MMRESULT hr = Win32.waveInStart(hWaveIn);
                    if (hr == Win32.MMRESULT.MMSYSERR_NOERROR)
                    {
                        IsWaveInStarted = true;
                        Stopped = false;

                        // Start thread
                        StartThreadPlayWaveIn();
                        return true;
                    }
                    else
                    {
                        // Error starting
                        return false;
                    }
                }
            }
            // WaveIn could not be opened
            return false;
        }

        // OpenWaveOut
        private bool StartWaveOut()
        {
            if (OpenWaveOut())
            {
                return CreateWaveOutHeaders();
            }
            return false;
        }

        // Stop
        public bool Stop()
        {
            try
            {
                lock (Locker)
                {
                    // When started
                    if (GCCopyDataBuffer.IsAllocated)
                    {
                        // End WaveIn
                        CloseWaveIn();

                        // Quit WaveOut
                        CloseWaveOut();

                        // Free up memory
                        GCCopyDataBuffer.Free();

                        // Ready
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Stop | {0}", ex.Message));
                return false;
            }
        }

        // CloseWaveIn
        private void CloseWaveIn()
        {
            // Set as manually ended
            Stopped = true;
            IsThreadPlayWaveInRunning = false;
            AutoResetEventDataRecorded.Set();

            // Stop WaveIn
            Win32.MMRESULT hResult = Win32.waveInStop(hWaveIn);

            // Set buffer as processed
            int resetCount = 0;
            while (IsAnyWaveInHeaderInState(Win32.WaveHdrFlags.WHDR_INQUEUE) & resetCount < 20)
            {
                Win32.MMRESULT hr = Win32.waveInReset(hWaveIn);
                System.Threading.Thread.Sleep(50);
                resetCount++;
            }

            // Release header handles (before waveInClose)
            FreeWaveInHeaders();

            // Close
            while (Win32.waveInClose(hWaveIn) == Win32.MMRESULT.WAVERR_STILLPLAYING)
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        // CloseWaveOut
        private void CloseWaveOut()
        {
            // Stop
            IsWaveOutOpened = false;
            Win32.MMRESULT hr = Win32.waveOutReset(hWaveOut);

            // Wait until everything plays
            while (IsAnyWaveOutHeaderInState(Win32.WaveHdrFlags.WHDR_INQUEUE))
            {
                System.Threading.Thread.Sleep(50);
            }

            // Release header handles
            FreeWaveOutHeaders();

            // Close
            hr = Win32.waveOutClose(hWaveOut);
        }

        // IsAnyWaveInHeaderInState
        private bool IsAnyWaveInHeaderInState(Win32.WaveHdrFlags state)
        {
            for (int i = 0; i < WaveInHeaders.Length; i++)
            {
                if ((WaveInHeaders[i]->dwFlags & state) == state)
                {
                    return true;
                }
            }
            return false;
        }

        // IsAnyWaveOutHeaderInState
        private bool IsAnyWaveOutHeaderInState(Win32.WaveHdrFlags state)
        {
            for (int i = 0; i < WaveOutHeaders.Length; i++)
            {
                if ((WaveOutHeaders[i]->dwFlags & state) == state)
                {
                    return true;
                }
            }
            return false;
        }

        // waveOutProc
        private void waveOutProc(IntPtr hWaveOut, Win32.WOM_Messages msg, IntPtr dwInstance, Win32.WAVEHDR* pWaveHeader, IntPtr lParam)
        {
            switch (msg)
            {
                // Open
                case Win32.WOM_Messages.OPEN:
                    break;

                // Close
                case Win32.WOM_Messages.CLOSE:
                    IsWaveOutOpened = false;
                    AutoResetEventDataRecorded.Set();
                    this.hWaveOut = IntPtr.Zero;
                    break;
            }
        }

        // waveInProc
        private void waveInProc(IntPtr hWaveIn, Win32.WIM_Messages msg, IntPtr dwInstance, Win32.WAVEHDR* waveHeader, IntPtr lParam)
        {

            switch (msg)
            {
                // Open
                case Win32.WIM_Messages.OPEN:
                    break;

                // Data
                case Win32.WIM_Messages.DATA:

                    // Data has arrived
                    IsDataIncomming = true;

                    // Remember recorded buffer
                    CurrentRecordedHeader = waveHeader;

                    // Set event
                    AutoResetEventDataRecorded.Set();
                    break;

                //Close
                case Win32.WIM_Messages.CLOSE:
                    IsDataIncomming = false;
                    IsWaveInOpened = false;
                    Stopped = true;
                    AutoResetEventDataRecorded.Set();
                    this.hWaveIn = IntPtr.Zero;
                    break;
            }
        }

        // OnThreadRecording
        private void OnThreadPlayWaveIn()
        {
            while (IsThreadPlayWaveInRunning && !Stopped)
            {
                // Wait until recording is finished
                AutoResetEventDataRecorded.WaitOne();

                try
                {

                    if (IsThreadPlayWaveInRunning && IsDataIncomming && IsWaveOutOpened && IsMute == false)
                    {
                        // Determine the next free playback buffer
                        for (int i = 0; i < WaveOutHeaders.Length; i++)
                        {
                            if ((WaveOutHeaders[i]->dwFlags & Win32.WaveHdrFlags.WHDR_INQUEUE) == 0)
                            {
                                try
                                {
                                    // Copy data to playback buffer
                                    Marshal.Copy(CurrentRecordedHeader->lpData, CopyDataBuffer, 0, CopyDataBuffer.Length);
                                    Marshal.Copy(CopyDataBuffer, 0, WaveOutHeaders[i]->lpData, CopyDataBuffer.Length);
                                    
                                    // Play data
                                    Win32.MMRESULT hr = Win32.waveOutWrite(hWaveOut, WaveOutHeaders[i], sizeof(Win32.WAVEHDR));
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(ex.Message);
                                }
                            }
                        }
                    }

                    if (IsThreadPlayWaveInRunning && !Stopped)
                    {
                        // Keep recording
                        for (int i = 0; i < WaveInHeaders.Length; i++)
                        {
                            if ((WaveInHeaders[i]->dwFlags & Win32.WaveHdrFlags.WHDR_INQUEUE) == 0)
                            {
                                Win32.MMRESULT hr = Win32.waveInAddBuffer(hWaveIn, WaveInHeaders[i], sizeof(Win32.WAVEHDR));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }


            // Set variables
            IsWaveInStarted = false;
            IsThreadPlayWaveInRunning = false;
            AutoResetEventThreadPlayWaveInEnd.Set();

            // Send event
            if (RepeaterStopped != null)
            {
                try
                {
                    RepeaterStopped();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Repeater Stopped | {0}", ex.Message));
                }
            }
        }
    }
}
