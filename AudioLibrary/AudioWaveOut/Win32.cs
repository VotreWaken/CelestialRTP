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

        [DllImport("winmm.dll")]
        public static extern long mciSendString(string strCommand,
                StringBuilder strReturn, int iReturnLength, IntPtr oCallback);
    }
}