//----------------------------------------------------------------------------
// File Name: Player.cs
// 
// Description: 
// Responsible for playing data bytes and dividing them into equal parts,
// responsible for playing Wave files, as well as closing them
//
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
    // Player
    unsafe public class Player
    {
        // Constructor
        public Player()
        {
            delegateWaveOutProc = new Win32.DelegateWaveOutProc(waveOutProc);
        }

        // Variables
        private LockerClass Locker = new LockerClass();
        private LockerClass LockerCopy = new LockerClass();
        private IntPtr hWaveOut = IntPtr.Zero;
        private String WaveOutDeviceName = "";
        private bool IsWaveOutOpened = false;
        private bool IsThreadPlayWaveOutRunning = false;
        private bool IsClosed = false;
        private bool IsPaused = false;
        private bool IsStarted = false;
        private bool IsBlocking = false;
        private int SamplesPerSecond = 8000;
        private int BitsPerSample = 16;
        private int Channels = 1;
        private int BufferCount = 8;
        private int BufferLength = 1024;
        private Win32.WAVEHDR*[] WaveOutHeaders;
        private Win32.DelegateWaveOutProc delegateWaveOutProc;
        private System.Threading.Thread ThreadPlayWaveOut;
        private System.Threading.AutoResetEvent AutoResetEventDataPlayed = new System.Threading.AutoResetEvent(false);

        // Delegates And Events
        public delegate void DelegateStopped();
        public event DelegateStopped PlayerClosed;
        public event DelegateStopped PlayerStopped;

        // Paused
        public bool Paused
        {
            get
            {
                return IsPaused;
            }
        }

        // Opened
        public bool Opened
        {
            get
            {
                return IsWaveOutOpened & IsClosed == false;
            }
        }

        // Playing
        public bool Playing
        {
            get
            {
                if (Opened && IsStarted)
                {
                    foreach (Win32.WAVEHDR* pHeader in WaveOutHeaders)
                    {
                        if (IsHeaderInqueue(*pHeader))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
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
                WaveOutHeaders[i]->lpData = Marshal.AllocHGlobal(BufferLength);
                WaveOutHeaders[i]->dwBufferLength = (uint)BufferLength;
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
        private void StartThreadPlayWaveOut()
        {
            if (IsThreadPlayWaveOutRunning == false)
            {
                ThreadPlayWaveOut = new System.Threading.Thread(new System.Threading.ThreadStart(OnThreadPlayWaveOut));
                IsThreadPlayWaveOutRunning = true;
                ThreadPlayWaveOut.Name = "PlayWaveOut";
                ThreadPlayWaveOut.Priority = System.Threading.ThreadPriority.Highest;
                ThreadPlayWaveOut.Start();
            }
        }

        // PlayBytes. Divide bytes into equal pieces and play them individually
        private bool PlayBytes(Byte[] bytes)
        {
            if (bytes.Length > 0)
            {
                // Size of the byte pieces 
                int byteSize = bytes.Length / BufferCount;
                int currentPos = 0;

                // For every possible buffer
                for (int count = 0; count < BufferCount; count++)
                {
                    // Determine the next free buffer
                    int index = GetNextFreeWaveOutHeaderIndex();
                    if (index != -1)
                    {
                        try
                        {
                            // Copy part
                            Byte[] partByte = new Byte[byteSize];
                            Array.Copy(bytes, currentPos, partByte, 0, byteSize);
                            currentPos += byteSize;

                            // If different file size
                            if (WaveOutHeaders[index]->dwBufferLength != partByte.Length)
                            {
                                // Create new data storage
                                Marshal.FreeHGlobal(WaveOutHeaders[index]->lpData);
                                WaveOutHeaders[index]->lpData = Marshal.AllocHGlobal(partByte.Length);
                                WaveOutHeaders[index]->dwBufferLength = (uint)partByte.Length;
                            }

                            // Copy data
                            WaveOutHeaders[index]->dwUser = (IntPtr)index;
                            Marshal.Copy(partByte, 0, WaveOutHeaders[index]->lpData, partByte.Length);
                        }
                        catch (Exception ex)
                        {
                            // Error while copying
                            System.Diagnostics.Debug.WriteLine(String.Format("CopyBytesToFreeWaveOutHeaders() | {0}", ex.Message));
                            AutoResetEventDataPlayed.Set();
                            return false;
                        }

                        // If still open
                        if (hWaveOut != null)
                        {
                            // Play
                            Win32.MMRESULT hr = Win32.waveOutWrite(hWaveOut, WaveOutHeaders[index], sizeof(Win32.WAVEHDR));
                            if (hr != Win32.MMRESULT.MMSYSERR_NOERROR)
                            {
                                // Error while playing
                                AutoResetEventDataPlayed.Set();
                                return false;
                            }
                        }
                        else
                        {
                            // WaveOut invalid
                            return false;
                        }
                    }
                    else
                    {
                        // Not enough free buffers available
                        return false;
                    }
                }
                return true;
            }
            // No data available
            return false;
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

        // Open
        public bool Open(string waveOutDeviceName, int samplesPerSecond, int bitsPerSample, int channels, int bufferCount)
        {
            try
            {
                lock (Locker)
                {
                    // If not already open
                    if (Opened == false)
                    {

                        // Take over data
                        WaveOutDeviceName = waveOutDeviceName;
                        SamplesPerSecond = samplesPerSecond;
                        BitsPerSample = bitsPerSample;
                        Channels = channels;
                        BufferCount = Math.Max(bufferCount, 1);

                        // If WaveOut could be opened
                        if (OpenWaveOut())
                        {
                            // If all buffers could be created
                            if (CreateWaveOutHeaders())
                            {
                                // Start thread
                                StartThreadPlayWaveOut();
                                IsClosed = false;
                                return true;
                            }
                        }
                    }

                    // Already opened
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Start | {0}", ex.Message));
                return false;
            }
        }

        // PlayData
        public bool PlayData(Byte[] datas, bool isBlocking)
        {
            try
            {
                if (Opened)
                {
                    int index = GetNextFreeWaveOutHeaderIndex();
                    if (index != -1)
                    {
                        // Take values
                        this.IsBlocking = isBlocking;

                        // If different file size
                        if (WaveOutHeaders[index]->dwBufferLength != datas.Length)
                        {
                            // Create new data storage
                            Marshal.FreeHGlobal(WaveOutHeaders[index]->lpData);
                            WaveOutHeaders[index]->lpData = Marshal.AllocHGlobal(datas.Length);
                            WaveOutHeaders[index]->dwBufferLength = (uint)datas.Length;
                        }

                        // Copy data
                        WaveOutHeaders[index]->dwBufferLength = (uint)datas.Length;
                        WaveOutHeaders[index]->dwUser = (IntPtr)index;
                        Marshal.Copy(datas, 0, WaveOutHeaders[index]->lpData, datas.Length);

                        // Play
                        this.IsStarted = true;
                        Win32.MMRESULT hr = Win32.waveOutWrite(hWaveOut, WaveOutHeaders[index], sizeof(Win32.WAVEHDR));
                        if (hr == Win32.MMRESULT.MMSYSERR_NOERROR)
                        {
                            // If blocking
                            if (isBlocking)
                            {
                                AutoResetEventDataPlayed.WaitOne();
                                AutoResetEventDataPlayed.Set();
                            }
                            return true;
                        }
                        else
                        {
                            // Error while playing
                            AutoResetEventDataPlayed.Set();
                            return false;
                        }
                    }
                    else
                    {
                        // No free output buffer available
                        System.Diagnostics.Debug.WriteLine(String.Format("No free WaveOut Buffer found | {0}", DateTime.Now.ToLongTimeString()));
                        return false;
                    }
                }
                else
                {
                    // Not open
                    return false;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("PlayData | {0}", ex.Message));
                return false;
            }
        }

        // PlayFile (Wave Files)
        public bool PlayFile(string fileName, string waveOutDeviceName)
        {
            lock (Locker)
            {
                try
                {
                    // WaveFile Read
                    WaveFileHeader header = WaveFile.Read(fileName);

                    // If data exists
                    if (header.Payload.Length > 0)
                    {
                        // When open
                        if (Open(waveOutDeviceName, (int)header.SamplesPerSecond, (int)header.BitsPerSample, (int)header.Channels, 8))
                        {
                            int index = GetNextFreeWaveOutHeaderIndex();
                            if (index != -1)
                            {
                                // Bytes Partially played in output buffer
                                this.IsStarted = true;
                                return PlayBytes(header.Payload);
                            }
                            else
                            {
                                // No free output buffer available
                                AutoResetEventDataPlayed.Set();
                                return false;
                            }
                        }
                        else
                        {
                            // Not open
                            AutoResetEventDataPlayed.Set();
                            return false;
                        }
                    }
                    else
                    {
                        // Bad file
                        AutoResetEventDataPlayed.Set();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("PlayFile | {0}", ex.Message));
                    AutoResetEventDataPlayed.Set();
                    return false;
                }
            }
        }

        // Close
        public bool Close()
        {
            try
            {
                lock (Locker)
                {
                    // When open
                    if (Opened)
                    {
                        // Set as manual ended
                        IsClosed = true;

                        // Wait until all data has finished playing
                        int count = 0;
                        while (Win32.waveOutReset(hWaveOut) != Win32.MMRESULT.MMSYSERR_NOERROR && count <= 100)
                        {
                            System.Threading.Thread.Sleep(50);
                            count++;
                        }

                        // Share headers and data
                        FreeWaveOutHeaders();

                        // Wait until all data has finished playing
                        count = 0;
                        while (Win32.waveOutClose(hWaveOut) != Win32.MMRESULT.MMSYSERR_NOERROR && count <= 100)
                        {
                            System.Threading.Thread.Sleep(50);
                            count++;
                        }

                        // Set variables
                        IsWaveOutOpened = false;
                        AutoResetEventDataPlayed.Set();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Close | {0}", ex.Message));
                return false;
            }
        }

        // StartPause
        public bool StartPause()
        {
            try
            {
                lock (Locker)
                {
                    // When open
                    if (Opened)
                    {
                        // If not already paused
                        if (IsPaused == false)
                        {
                            // Pause
                            Win32.MMRESULT hr = Win32.waveOutPause(hWaveOut);
                            if (hr == Win32.MMRESULT.MMSYSERR_NOERROR)
                            {
                                // Save
                                IsPaused = true;
                                AutoResetEventDataPlayed.Set();
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("StartPause | {0}", ex.Message));
                return false;
            }
        }

        // EndPause
        public bool EndPause()
        {
            try
            {
                lock (Locker)
                {
                    // When open
                    if (Opened)
                    {
                        // When paused
                        if (IsPaused)
                        {
                            // Pause
                            Win32.MMRESULT hr = Win32.waveOutRestart(hWaveOut);
                            if (hr == Win32.MMRESULT.MMSYSERR_NOERROR)
                            {
                                // Save
                                IsPaused = false;
                                AutoResetEventDataPlayed.Set();
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("EndPause | {0}", ex.Message));
                return false;
            }
        }

        // GetNextFreeWaveOutHeaderIndex
        private int GetNextFreeWaveOutHeaderIndex()
        {
            for (int i = 0; i < WaveOutHeaders.Length; i++)
            {
                if (IsHeaderPrepared(*WaveOutHeaders[i]) && !IsHeaderInqueue(*WaveOutHeaders[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        // IsHeaderPrepared
        private bool IsHeaderPrepared(Win32.WAVEHDR header)
        {
            return (header.dwFlags & Win32.WaveHdrFlags.WHDR_PREPARED) > 0;
        }

        /// IsHeaderInqueue
        private bool IsHeaderInqueue(Win32.WAVEHDR header)
        {
            return (header.dwFlags & Win32.WaveHdrFlags.WHDR_INQUEUE) > 0;
        }

        // waveOutProc
        private void waveOutProc(IntPtr hWaveOut, Win32.WOM_Messages msg, IntPtr dwInstance, Win32.WAVEHDR* pWaveHeader, IntPtr lParam)
        {
            try
            {
                switch (msg)
                {
                    // Open
                    case Win32.WOM_Messages.OPEN:
                        break;

                    // Done
                    case Win32.WOM_Messages.DONE:

                        // No that data arrives
                        IsStarted = true;
                        AutoResetEventDataPlayed.Set();
                        break;

                    // Close
                    case Win32.WOM_Messages.CLOSE:
                        IsStarted = false;
                        IsWaveOutOpened = false;
                        IsPaused = false;
                        IsClosed = true;
                        AutoResetEventDataPlayed.Set();
                        this.hWaveOut = IntPtr.Zero;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Player.cs | waveOutProc() | {0}", ex.Message));
                AutoResetEventDataPlayed.Set();
            }
        }

        // OnThreadRecording
        private void OnThreadPlayWaveOut()
        {
            while (Opened && !IsClosed)
            {
                // Wait until recording is finished
                AutoResetEventDataPlayed.WaitOne();

                lock (Locker)
                {
                    if (Opened && !IsClosed)
                    {
                        // Set variables
                        IsThreadPlayWaveOutRunning = true;

                        // When no more data is played
                        if (!Playing)
                        {
                            // When data has been played
                            if (IsStarted)
                            {
                                IsStarted = false;

                                // Submit event
                                if (PlayerStopped != null)
                                {
                                    try
                                    {
                                        PlayerStopped();
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine(String.Format("Player Stopped | {0}", ex.Message));
                                    }
                                    finally
                                    {
                                        AutoResetEventDataPlayed.Set();
                                    }
                                }
                            }
                        }
                    }
                }

                // If blocking
                if (IsBlocking)
                {
                    AutoResetEventDataPlayed.Set();
                }
            }

            lock (Locker)
            {
                // Set variables
                IsThreadPlayWaveOutRunning = false;
            }

            // Send event
            if (PlayerClosed != null)
            {
                try
                {
                    PlayerClosed();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Player Closed | {0}", ex.Message));
                }
            }
        }
    }
}
