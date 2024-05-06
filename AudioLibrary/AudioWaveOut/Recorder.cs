//----------------------------------------------------------------------------
// File Name: Recorder.cs
// 
// Description: 
// Recorder is responsible for creating WaveIn Headers, their allocation,
// initialization, and releasing WaveIn headers, creating a stream for recording,
// opening WaveIn, defining the format, WaveIn device.
//
// Initializing and opening OpenWaveIn and its starting, stopping and closing
// Responsible for creating and behaving a stream for WaveIn devices, creating a
// playback buffer and copying and playing data from this buffer
// and monitors changes in WaveIn Devices.
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
    unsafe public class Recorder
    {
        // Constructor 
        public Recorder()
        {
            delegateWaveInProc = new Win32.DelegateWaveInProc(waveInProc);
        }

        // Variables
        private LockerClass Locker = new LockerClass();
        private LockerClass LockerCopy = new LockerClass();
        private IntPtr hWaveIn = IntPtr.Zero;
        private String WaveInDeviceName = "";
        private bool IsWaveInOpened = false;
        private bool IsWaveInStarted = false;
        private bool IsThreadRecordingRunning = false;
        private bool IsDataIncomming = false;
        private bool Stopped = false;
        private int SamplesPerSecond = 8000;
        private int BitsPerSample = 16;
        private int Channels = 1;
        private int BufferCount = 8;
        private int BufferSize = 1024;
        private Win32.WAVEHDR*[] WaveInHeaders;
        private Win32.WAVEHDR* CurrentRecordedHeader;
        private Win32.DelegateWaveInProc delegateWaveInProc;
        private System.Threading.Thread ThreadRecording;
        private System.Threading.AutoResetEvent AutoResetEventDataRecorded = new System.Threading.AutoResetEvent(false);

        // Delegates And Events
        public delegate void DelegateStopped();
        public delegate void DelegateDataRecorded(Byte[] bytes);
        public event DelegateStopped RecordingStopped;
        public event DelegateDataRecorded DataRecorded;

        // Started
        public bool Started
        {
            get
            {
                return IsWaveInStarted && IsWaveInOpened && IsThreadRecordingRunning;
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

        // StartThreadRecording
        private void StartThreadRecording()
        {
            if (Started == false)
            {
                ThreadRecording = new System.Threading.Thread(new System.Threading.ThreadStart(OnThreadRecording));
                IsThreadRecordingRunning = true;
                ThreadRecording.Name = "Recording";
                ThreadRecording.Priority = System.Threading.ThreadPriority.Highest;
                ThreadRecording.Start();
            }
        }

        // StartWaveIn
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

        // Start
        public bool Start(string waveInDeviceName, int samplesPerSecond, int bitsPerSample, int channels, int bufferCount, int bufferSize)
        {
            try
            {
                lock (Locker)
                {
                    // If not already started
                    if (Started == false)
                    {

                        // Take over data
                        WaveInDeviceName = waveInDeviceName;
                        SamplesPerSecond = samplesPerSecond;
                        BitsPerSample = bitsPerSample;
                        Channels = channels;
                        BufferCount = bufferCount;
                        BufferSize = bufferSize;

                        // If WaveIn could be opened
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

                                    // Start thread
                                    StartThreadRecording();
                                    Stopped = false;
                                    return true;
                                }
                                else
                                {
                                    // Error starting
                                    return false;
                                }
                            }
                        }
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

        // Stop
        public bool Stop()
        {
            try
            {
                lock (Locker)
                {
                    // When started
                    if (Started)
                    {
                        // Set as manual ended
                        Stopped = true;
                        IsThreadRecordingRunning = false;

                        // Close WaveIn
                        CloseWaveIn();

                        // Set variables
                        AutoResetEventDataRecorded.Set();
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
            // Set buffer as processed
            Win32.MMRESULT hr = Win32.waveInStop(hWaveIn);

            int resetCount = 0;
            while (IsAnyWaveInHeaderInState(Win32.WaveHdrFlags.WHDR_INQUEUE) & resetCount < 20)
            {
                hr = Win32.waveInReset(hWaveIn);
                System.Threading.Thread.Sleep(50);
                resetCount++;
            }

            // Release header handles (before waveInClose)
            FreeWaveInHeaders();

            // Close
            hr = Win32.waveInClose(hWaveIn);
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

        // waveInProc
        private void waveInProc(IntPtr hWaveIn, Win32.WIM_Messages msg, IntPtr dwInstance, Win32.WAVEHDR* pWaveHdr, IntPtr lParam)
        {
            switch (msg)
            {
                // Open
                case Win32.WIM_Messages.OPEN:
                    break;

                // Data
                case Win32.WIM_Messages.DATA:

                    // No incoming data
                    IsDataIncomming = true;

                    // Remember recorded buffer
                    CurrentRecordedHeader = pWaveHdr;

                    // Set event
                    AutoResetEventDataRecorded.Set();
                    break;

                // Close
                case Win32.WIM_Messages.CLOSE:
                    IsDataIncomming = false;
                    IsWaveInOpened = false;
                    AutoResetEventDataRecorded.Set();
                    this.hWaveIn = IntPtr.Zero;
                    break;
            }
        }

        // OnThreadRecording
        private void OnThreadRecording()
        {
            while (Started && !Stopped)
            {
                // Wait until recording is finished
                AutoResetEventDataRecorded.WaitOne();

                try
                {
                    // If active
                    if (Started && !Stopped)
                    {
                        // If data exists
                        if (CurrentRecordedHeader->dwBytesRecorded > 0)
                        {
                            // When data is requested
                            if (DataRecorded != null && IsDataIncomming)
                            {
                                try
                                {
                                    // Copy data
                                    Byte[] bytes = new Byte[CurrentRecordedHeader->dwBytesRecorded];
                                    Marshal.Copy(CurrentRecordedHeader->lpData, bytes, 0, (int)CurrentRecordedHeader->dwBytesRecorded);

                                    // Submit event
                                    DataRecorded(bytes);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(String.Format("Recorder.cs | OnThreadRecording() | {0}", ex.Message));
                                }
                            }

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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }


            lock (Locker)
            {
                // Set variables
                IsWaveInStarted = false;
                IsThreadRecordingRunning = false;
            }

            // Send event
            if (RecordingStopped != null)
            {
                try
                {
                    RecordingStopped();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Recording Stopped | {0}", ex.Message));
                }
            }
        }
    }
}
