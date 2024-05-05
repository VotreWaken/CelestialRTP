//----------------------------------------------------------------------------
// File Name: Win32.cs
// 
// Description: 
// Win32 provides an audio handling using standart windows API WaveOut 
// define necessary Win32 API Calls and classes
//
// Author(s):
// Egor Waken
//
// History:
// 30 Apr 2024	Egor Waken       Created.
// 04 May 2024  Egor Waken       Added Necessary Calls and classes.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//----------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Text;

namespace AudioWaveOut
{
    // Win32 Class
    unsafe public class Win32
    {
        // Const Variables
        public const int WAVE_MAPPER = -1;
        public const int WT_EXECUTEDEFAULT = 0x00000000;
        public const int WT_EXECUTEINIOTHREAD = 0x00000001;
        public const int WT_EXECUTEINTIMERTHREAD = 0x00000020;
        public const int WT_EXECUTEINPERSISTENTTHREAD = 0x00000080;
        public const int TIME_ONESHOT = 0;
        public const int TIME_PERIODIC = 1;

        // Events
        public delegate void DelegateWaveOutProc(IntPtr hWaveOut, WOM_Messages msg, IntPtr dwInstance, Win32.WAVEHDR* pWaveHdr, IntPtr lParam);
        public delegate void DelegateWaveInProc(IntPtr hWaveIn, WIM_Messages msg, IntPtr dwInstance, Win32.WAVEHDR* pWaveHdr, IntPtr lParam);
        public delegate void DelegateTimerProc(IntPtr lpParameter, bool TimerOrWaitFired);
        public delegate void TimerEventHandler(UInt32 id, UInt32 msg, ref UInt32 userCtx, UInt32 rsv1, UInt32 rsv2);

        // Constructor
        public Win32()
        {
            //RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        }

        // This struct provides windows native waveOut implementation.
        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
        public struct WAVEOUTCAPS
        {
            /// <summary>
            /// Manufacturer identifier for the device driver for the device.
            /// </summary>
            public short wMid;
            /// <summary>
            /// Product identifier for the device.
            /// </summary>
            public short wPid;
            /// <summary>
            /// Version number of the device driver for the device.
            /// </summary>
            public int vDriverVersion;
            /// <summary>
            /// Product name in a null-terminated string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            /// <summary>
            /// Standard formats that are supported.
            /// </summary>
            public uint dwFormats;
            /// <summary>
            /// Number specifying whether the device supports mono (1) or stereo (2) output.
            /// </summary>
            public short wChannels;
            /// <summary>
            /// Packing.
            /// </summary>
            public short wReserved;
            /// <summary>
            /// Optional functionality supported by the device.
            /// </summary>
            public int dwSupport;
        }

        // Describes the capabilities of the audio input device.
        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
        public struct WAVEINCAPS
        {
            public short wMid;
            public short wPid;
            public int vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwFormats;
            public short wChannels;
            public short wReserved;
            public int dwSupport;
        }

        // This Struct represents WAVEFORMATEX structure.
        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            /// <summary>
            /// Number of channels in the waveform-audio data. Monaural data uses one channel and stereo data 
            /// uses two channels.
            /// </summary>
            public ushort nChannels;
            /// <summary>
            /// Sample rate, in samples per second (hertz). If wFormatTag is WAVE_FORMAT_PCM, then common 
            /// values for nSamplesPerSec are 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.
            /// </summary>
            public uint nSamplesPerSec;
            /// <summary>
            /// Required average data-transfer rate, in bytes per second, for the format tag. If wFormatTag 
            /// is WAVE_FORMAT_PCM, nAvgBytesPerSec should be equal to the product of nSamplesPerSec and nBlockAlign.
            /// </summary>
            public uint nAvgBytesPerSec;
            /// <summary>
            /// Block alignment, in bytes. The block alignment is the minimum atomic unit of data for the wFormatTag 
            /// format type. If wFormatTag is WAVE_FORMAT_PCM or WAVE_FORMAT_EXTENSIBLE, nBlockAlign must be equal 
            /// to the product of nChannels and wBitsPerSample divided by 8 (bits per byte).
            /// </summary>
            public ushort nBlockAlign;
            /// <summary>
            /// Bits per sample for the wFormatTag format type. If wFormatTag is WAVE_FORMAT_PCM, then 
            /// wBitsPerSample should be equal to 8 or 16.
            /// </summary>
            public ushort wBitsPerSample;
            /// <summary>
            /// Size, in bytes, of extra format information appended to the end of the WAVEFORMATEX structure.
            /// </summary>
            public ushort cbSize;
        }


        // This Struct represents MMRESULT structure.
        public enum MMRESULT : uint
        {
            MMSYSERR_NOERROR = 0,
            MMSYSERR_ERROR = 1,
            MMSYSERR_BADDEVICEID = 2,
            MMSYSERR_NOTENABLED = 3,
            MMSYSERR_ALLOCATED = 4,
            MMSYSERR_INVALHANDLE = 5,
            MMSYSERR_NODRIVER = 6,
            MMSYSERR_NOMEM = 7,
            MMSYSERR_NOTSUPPORTED = 8,
            MMSYSERR_BADERRNUM = 9,
            MMSYSERR_INVALFLAG = 10,
            MMSYSERR_INVALPARAM = 11,
            MMSYSERR_HANDLEBUSY = 12,
            MMSYSERR_INVALIDALIAS = 13,
            MMSYSERR_BADDB = 14,
            MMSYSERR_KEYNOTFOUND = 15,
            MMSYSERR_READERROR = 16,
            MMSYSERR_WRITEERROR = 17,
            MMSYSERR_DELETEERROR = 18,
            MMSYSERR_VALNOTFOUND = 19,
            MMSYSERR_NODRIVERCB = 20,
            WAVERR_BADFORMAT = 32,
            WAVERR_STILLPLAYING = 33,
            WAVERR_UNPREPARED = 34
        }

        // This struct provides windows native waveOut Errors.
        public enum MMSYSERR : uint
        {
            // Add MMSYSERR's here!

            MMSYSERR_BASE = 0x0000,
            MMSYSERR_NOERROR = 0x0000
        }

        [Flags]
        public enum WaveHdrFlags : uint
        {
            WHDR_DONE = 1,
            WHDR_PREPARED = 2,
            WHDR_BEGINLOOP = 4,
            WHDR_ENDLOOP = 8,
            WHDR_INQUEUE = 16
        }

        [Flags]
        public enum WaveProcFlags : int
        {
            CALLBACK_NULL = 0,
            CALLBACK_FUNCTION = 0x30000,
            CALLBACK_EVENT = 0x50000,
            CALLBACK_WINDOW = 0x10000,
            CALLBACK_THREAD = 0x20000,
            WAVE_FORMAT_QUERY = 1,
            WAVE_MAPPED = 4,
            WAVE_FORMAT_DIRECT = 8
        }

        [Flags]
        public enum HRESULT : long
        {
            S_OK = 0L,
            S_FALSE = 1L
        }

        [Flags]
        public enum WaveFormatFlags : int
        {
            WAVE_FORMAT_PCM = 0x0001
        }

        // This Struct represents WAVEHDR structure.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEHDR
        {
            /// <summary>
            /// Long pointer to the address of the waveform buffer.
            /// </summary>
            public IntPtr lpData; // pointer to locked data buffer
            /// <summary>
            /// Specifies the length, in bytes, of the buffer.
            /// </summary>
            public uint dwBufferLength; // length of data buffer
            /// <summary>
            /// When the header is used in input, this member specifies how much data is in the buffer. 
            /// When the header is used in output, this member specifies the number of bytes played from the buffer.
            /// </summary>
            public uint dwBytesRecorded; // used for input only
            /// <summary>
            /// Specifies user data.
            /// </summary>
            public IntPtr dwUser; // for client's use
            /// <summary>
            /// Specifies information about the buffer.
            /// </summary>
            public WaveHdrFlags dwFlags; // assorted flags (see defines)
            /// <summary>
            /// Specifies the number of times to play the loop.
            /// </summary>
            public uint dwLoops; // loop control counter
            /// <summary>
            /// Reserved. This member is used within the audio driver to maintain a first-in, first-out linked list of headers awaiting playback.
            /// </summary>
            public IntPtr lpNext; // PWaveHdr, reserved for driver
            /// <summary>
            /// Reserved.
            /// </summary>
            public IntPtr reserved; // reserved for driver
        }

        // TimeCaps
        [StructLayout(LayoutKind.Sequential)]
        public struct TimeCaps
        {
            public UInt32 wPeriodMin;
            public UInt32 wPeriodMax;
        };

        // WOM_Messages
        public enum WOM_Messages : int
        {
            OPEN = 0x03BB,
            CLOSE = 0x03BC,
            DONE = 0x03BD
        }

        // WIM_Messages
        public enum WIM_Messages : int
        {
            OPEN = 0x03BE,
            CLOSE = 0x03BF,
            DATA = 0x03C0
        }

        // Writes the number of cycles of the Performance Counter to a variable lpPerformanceCount
        /// <param name="lpPerformanceCount">Performance Counter</param>
        [DllImport("Kernel32.dll", EntryPoint = "QueryPerformanceCounter")]
        public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);


        // Writes the number of cycles of the Performance Frequency to a variable lpFrequency
        /// <param name="lpFrequency">Performance Frequency</param>
        [DllImport("Kernel32.dll", EntryPoint = "QueryPerformanceFrequency")]
        public static extern bool QueryPerformanceFrequency(out long lpFrequency);


        // The timeSetEvent function fires the specified media timer event.
        // The media timer runs in its own thread. Once an event is fired, it calls the
        // specified callback function or sets or causes the specified event to fire.
        /// <param name="msDelay">delay in ms</param>
        /// <param name="msResolution">resolution(0 is the maximum available for a given PC), set in ms.</param>
        /// <param name="handler">pointer to a procedure that will be called after a specified time interval has elapsed</param>
        /// <param name="userCtx">This parameter is passed to the lpTimeProc handler and can be used at the discretion of the programmer</param>
        /// <param name="eventType"> timer type.Two meanings possible</param>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
        public static extern UInt32 TimeSetEvent(UInt32 msDelay, UInt32 msResolution, TimerEventHandler handler, ref UInt32 userCtx, UInt32 eventType);


        // After finishing working with the timer, you need to delete it using the timeKillEvent function:
        /// <param name="timerId">number assigned to the timer when it was created using timeSetEvent</param>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
        public static extern UInt32 TimeKillEvent(UInt32 timerId);


        // Create Timer Queue 
        [DllImport("kernel32.dll", EntryPoint = "CreateTimerQueue")]
        public static extern IntPtr CreateTimerQueue();


        // Delete Timer Queue
        /// <param name="TimerQueue">number assigned to the Timer Queue when it was created using CreateTimerQueue</param>
        [DllImport("kernel32.dll", EntryPoint = "DeleteTimerQueue")]
        public static extern bool DeleteTimerQueue(IntPtr TimerQueue);


        // Create Timer Queue Timer
        /// <param name="phNewTimer">Pointer to a buffer that receives the timer queue's timer handle when returning</param>
        /// <param name="TimerQueue">Handle to the timer queue. This handle is returned by the CreateTimerQueue function.</param>
        /// <param name="Callback">Pointer to an application-defined function to execute when the timer expires</param>
        /// <param name="Parameter">The value of a single parameter that will be passed to the callback function.</param>
        /// <param name="DueTime">The time in milliseconds relative to the current time that must pass before the first timer signal.</param>
        /// <param name="Period">Timer period in milliseconds. If this parameter is zero, the timer receives the signal once</param>
        /// <param name="Flags">Timer Flags</param>
        [DllImport("kernel32.dll", EntryPoint = "CreateTimerQueueTimer")]
        public static extern bool CreateTimerQueueTimer(out IntPtr phNewTimer, IntPtr TimerQueue, DelegateTimerProc Callback, IntPtr Parameter, uint DueTime, uint Period, uint Flags);


        // Removes a timer from the timer queue and optionally waits for the current timer callbacks to complete before deleting the timer.
        /// <param name="TimerQueue">Handle to the timer queue. This handle is returned by the CreateTimerQueue function.</param>
        /// <param name="Timer">Queue timer handle. This handle is returned by the CreateTimerQueueTimer function</param>
        /// <param name="CompletionEvent">Handle to an event object that will signal when the system has canceled the timer and all callback functions have completed</param>
        [DllImport("kernel32.dll")]
        public static extern bool DeleteTimerQueueTimer(IntPtr TimerQueue, IntPtr Timer, IntPtr CompletionEvent);


        // function queries the timer device to determine its resolution.
        /// <param name="timeCaps">
        /// A pointer to a <see cref="TIMECAPS"/> structure.
        /// This structure is filled with information about the resolution of the timer device.
        /// </param>
        /// <param name="sizeTimeCaps">
        /// The size, in bytes, of the <see cref="TIMECAPS"/> structure.
        /// </param>
        /// <returns>
        /// Returns <see cref="MMSYSERR_NOERROR"/> if successful or an error code otherwise.
        /// Possible error codes include the following.
        /// <see cref="MMSYSERR_ERROR"/>: General error code.
        /// <see cref="TIMERR_NOCANDO"/>: The <paramref name="timeCaps"/> parameter is <see cref="NullRef{TIMECAPS}"/>,
        /// or the <paramref name="sizeTimeCaps"/> parameter is invalid, or some other error occurred.
        /// </returns>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeGetDevCaps")]
        public static extern MMRESULT TimeGetDevCaps(ref TimeCaps timeCaps, UInt32 sizeTimeCaps);


        // function requests a minimum resolution for periodic timers
        /// <param name="uPeriod"> Minimum timer resolution, in milliseconds, for the application or device driver. A lower value specifies a higher (more accurate) resolution.</param>
        /// <returns>
        /// Returns <see cref="TIMERR_NOERROR"/> if successful or <see cref="TIMERR_NOCANDO"/>
        /// if the resolution specified in <paramref name="uPeriod"/> is out of range.
        /// </returns>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeBeginPeriod")]
        public static extern MMRESULT TimeBeginPeriod(UInt32 uPeriod);


        // function clears a previously set minimum timer resolution.
        /// <param name="uPeriod">
        /// Minimum timer resolution specified in the previous call to the <see cref="timeBeginPeriod"/> function.
        /// </param>
        /// <returns>
        /// Returns <see cref="TIMERR_NOERROR"/> if successful or <see cref="TIMERR_NOCANDO"/>
        /// if the resolution specified in <paramref name="uPeriod"/> is out of range.
        /// </returns>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeEndPeriod")]
        public static extern MMRESULT TimeEndPeriod(UInt32 uPeriod);


        // The waveOutOpen function opens the given waveform-audio output device for playback.
        /// <param name="hWaveOut">Pointer to a buffer that receives a handle identifying the open waveform-audio output device. Use the handle to identify the device when calling other waveform-audio output functions. This parameter might be NULL if the WAVE_FORMAT_QUERY flag is specified for fdwOpen.</param>
        /// <param name="uDeviceID">Identifier of the waveform-audio output device to open. It can be either a device identifier or a handle of an open waveform-audio input device.</param>
        /// <param name="lpFormat">Pointer to a WAVEFORMATEX structure that identifies the format of the waveform-audio data to be sent to the device. You can free this structure immediately after passing it to waveOutOpen.</param>
        /// <param name="dwCallback">Pointer to a fixed callback function, an event handle, a handle to a window, or the identifier of a thread to be called during waveform-audio playback to process messages related to the progress of the playback. If no callback function is required, this value can be zero.</param>
        /// <param name="dwInstance">User-instance data passed to the callback mechanism.</param>
        /// <param name="dwFlags">Flags for opening the device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern MMRESULT waveOutOpen(ref IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, DelegateWaveOutProc dwCallBack, int dwInstance, int dwFlags);


        // The waveInOpen function opens the given waveform-audio input device for recording.
        /// <param name="hWaveOut">Pointer to a buffer that receives a handle identifying the open waveform-audio input device.</param>
        /// <param name="uDeviceID">Identifier of the waveform-audio input device to open. It can be either a device identifier or a handle of an open waveform-audio input device. You can use the following flag instead of a device identifier.</param>
        /// <param name="lpFormat">Pointer to a WAVEFORMATEX structure that identifies the desired format for recording waveform-audio data. You can free this structure immediately after waveInOpen returns.</param>
        /// <param name="dwCallback">Pointer to a fixed callback function, an event handle, a handle to a window, 
        /// or the identifier of a thread to be called during waveform-audio recording to process messages related 
        /// to the progress of recording. If no callback function is required, this value can be zero. 
        /// For more information on the callback function, see waveInProc.</param>
        /// <param name="dwInstance">User-instance data passed to the callback mechanism.</param>
        /// <param name="dwFlags">Flags for opening the device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInOpen(ref IntPtr hWaveIn, int deviceId, ref WAVEFORMATEX wfx, DelegateWaveInProc dwCallBack, int dwInstance, int dwFlags);


        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "waveInOpen")]
        public static extern MMRESULT waveInOpen2(ref IntPtr hWaveIn, int deviceId, ref WAVEFORMATEX wfx, Microsoft.Win32.SafeHandles.SafeWaitHandle callBackEvent, int dwInstance, int dwFlags);


        // Starts input on the given waveform-audio input device.
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT waveInStart(IntPtr hWaveIn);


        // Queries a specified waveform device to determine its capabilities.
        /// <param name="hwo">Identifier of the waveform-audio input device. It can be either a device identifier or a Handle to an open waveform-audio output device.</param>
        /// <param name="pwoc">Pointer to a WAVEOUTCAPS structure to be filled with information about the capabilities of the device.</param>
        /// <param name="cbwoc">Size, in bytes, of the WAVEOUTCAPS structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveInGetDevCaps(int index, ref WAVEINCAPS pwic, int cbwic);


        // Get the waveInGetNumDevs function returns the number of waveform-audio input devices present in the system.
        /// <returns>Returns the waveInGetNumDevs function returns the number of waveform-audio input devices present in the system.
        /// </returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveInGetNumDevs();


        // Queries a specified waveform device to determine its capabilities.
        /// <param name="hwo">Identifier of the waveform-audio output device. It can be either a device identifier or a Handle to an open waveform-audio output device.</param>
        /// <param name="pwoc">Pointer to a WAVEOUTCAPS structure to be filled with information about the capabilities of the device.</param>
        /// <param name="cbwoc">Size, in bytes, of the WAVEOUTCAPS structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint waveOutGetDevCaps(int index, ref WAVEOUTCAPS pwoc, int cbwoc);


        // Retrieves the number of waveform output devices present in the system.
        /// <returns>The number of devices indicates success. Zero indicates that no devices are present or that an error occurred.</returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint waveOutGetNumDevs();


        // Sends a data block to the specified waveform output device.
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure containing information about the data block.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern MMRESULT waveOutWrite(IntPtr hWaveOut, WAVEHDR* pwh, int cbwh);


        // Prepares a waveform data block for playback.
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the data block to be prepared. The buffer's base address must be aligned with the respect to the sample size.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "waveOutPrepareHeader", CharSet = CharSet.Auto)]
        public static extern MMRESULT waveOutPrepareHeader(IntPtr hWaveOut, WAVEHDR* lpWaveOutHdr, int uSize);


        // Cleans up the preparation performed by waveOutPrepareHeader.
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure identifying the data block to be cleaned up.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "waveOutUnprepareHeader", CharSet = CharSet.Auto)]
        public static extern MMRESULT waveOutUnprepareHeader(IntPtr hWaveOut, WAVEHDR* lpWaveOutHdr, int uSize);


        // Stops input on the given waveform-audio input device.
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", EntryPoint = "waveInStop", SetLastError = true)]
        public static extern MMRESULT waveInStop(IntPtr hWaveIn);


        // Stops input on a specified waveform output device and resets the current position to 0.
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", EntryPoint = "waveInReset", SetLastError = true)]
        public static extern MMRESULT waveInReset(IntPtr hWaveIn);


        // Stops playback on a specified waveform output device and resets the current position to 0.
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", EntryPoint = "waveOutReset", SetLastError = true)]
        public static extern MMRESULT waveOutReset(IntPtr hWaveOut);


        // Prepares a waveform data block for recording.
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the data block to be prepared. 
        /// The buffer's base address must be aligned with the respect to the sample size.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT waveInPrepareHeader(IntPtr hWaveIn, WAVEHDR* pwh, int cbwh);


        // Cleans up the preparation performed by waveInPrepareHeader.
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure identifying the data block to be cleaned up.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT waveInUnprepareHeader(IntPtr hWaveIn, WAVEHDR* pwh, int cbwh);


        // The waveInAddBuffer function sends an input buffer to the given waveform-audio input device. When the buffer is filled, the application is notified.
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the buffer.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", EntryPoint = "waveInAddBuffer", SetLastError = true)]
        public static extern MMRESULT waveInAddBuffer(IntPtr hWaveIn, WAVEHDR* pwh, int cbwh);


        // Closes the specified waveform input device.
        /// <param name="hWaveOut">Handle to the waveform-audio input device. If the function succeeds, the handle is no longer valid after this call.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern Win32.MMRESULT waveInClose(IntPtr hWaveIn);


        // Closes the specified waveform output device.
        /// <param name="hWaveOut">Handle to the waveform-audio output device. If the function succeeds, the handle is no longer valid after this call.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern Win32.MMRESULT waveOutClose(IntPtr hWaveOut);


        // Pauses playback on a specified waveform output device.
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
        public static extern Win32.MMRESULT waveOutPause(IntPtr hWaveOut);


        // Restarts a paused waveform output device.
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll", EntryPoint = "waveOutRestart", SetLastError = true)]
        public static extern Win32.MMRESULT waveOutRestart(IntPtr hWaveOut);


    }
}