//----------------------------------------------------------------------------
// File Name: Win32.cs
// 
// Description: 
// WinSound provides an audio WaveIn Devices using API Calls, Show All Playback
// Names and WaveOut Devices By Name
//
// Author(s):
// Egor Waken
//
// History:
// 05 May 2024	Egor Waken       Created.
//
// License:
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//----------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Text;

namespace AudioWaveOut
{
    // LockerClass
    class LockerClass
    {

    }

    // WinSound
    public class WinSound
    {
        // Constructor
        public WinSound()
        {

        }

        // Show All Playback Names
        public static List<String> GetPlaybackNames()
        {
            // Result
            List<String> list = new List<string>();
            Win32.WAVEOUTCAPS waveOutCap = new Win32.WAVEOUTCAPS();

            // Number of devices
            uint num = Win32.waveOutGetNumDevs();
            for (int i = 0; i < num; i++)
            {
                uint hr = Win32.waveOutGetDevCaps(i, ref waveOutCap, Marshal.SizeOf(typeof(Win32.WAVEOUTCAPS)));
                if (hr == (int)Win32.HRESULT.S_OK)
                {
                    list.Add(waveOutCap.szPname);
                }
            }

            return list;
        }

        // View all recording devices
        public static List<String> GetRecordingNames()
        {
            // Result
            List<String> list = new List<string>();
            Win32.WAVEINCAPS waveInCap = new Win32.WAVEINCAPS();

            // Number of devices
            uint num = Win32.waveInGetNumDevs();
            for (int i = 0; i < num; i++)
            {
                uint hr = Win32.waveInGetDevCaps(i, ref waveInCap, Marshal.SizeOf(typeof(Win32.WAVEINCAPS)));
                if (hr == (int)Win32.HRESULT.S_OK)
                {
                    list.Add(waveInCap.szPname);
                }
            }

            return list;
        }

        // GetWaveInDeviceIdByName
        public static int GetWaveInDeviceIdByName(string name)
        {
            // Number of devices
            uint num = Win32.waveInGetNumDevs();

            // WaveIn structure
            Win32.WAVEINCAPS caps = new Win32.WAVEINCAPS();
            for (int i = 0; i < num; i++)
            {
                Win32.HRESULT hr = (Win32.HRESULT)Win32.waveInGetDevCaps(i, ref caps, Marshal.SizeOf(typeof(Win32.WAVEINCAPS)));
                if (hr == Win32.HRESULT.S_OK)
                {
                    if (caps.szPname == name)
                    {
                        // Found
                        return i;
                    }
                }
            }

            // Not found
            return Win32.WAVE_MAPPER;
        }

        // GetWaveOutDeviceIdByName
        public static int GetWaveOutDeviceIdByName(string name)
        {
            // Number of devices
            uint num = Win32.waveOutGetNumDevs();

            // WaveIn structure
            Win32.WAVEOUTCAPS caps = new Win32.WAVEOUTCAPS();
            for (int i = 0; i < num; i++)
            {
                Win32.HRESULT hr = (Win32.HRESULT)Win32.waveOutGetDevCaps(i, ref caps, Marshal.SizeOf(typeof(Win32.WAVEOUTCAPS)));
                if (hr == Win32.HRESULT.S_OK)
                {
                    if (caps.szPname == name)
                    {
                        // Found
                        return i;
                    }
                }
            }

            // Not found
            return Win32.WAVE_MAPPER;
        }

        // Flag To String
        public static String FlagToString(Win32.WaveHdrFlags flag)
        {
            StringBuilder sb = new StringBuilder();

            if ((flag & Win32.WaveHdrFlags.WHDR_PREPARED) > 0) sb.Append("PREPARED ");
            if ((flag & Win32.WaveHdrFlags.WHDR_BEGINLOOP) > 0) sb.Append("BEGINLOOP ");
            if ((flag & Win32.WaveHdrFlags.WHDR_ENDLOOP) > 0) sb.Append("ENDLOOP ");
            if ((flag & Win32.WaveHdrFlags.WHDR_INQUEUE) > 0) sb.Append("INQUEUE ");
            if ((flag & Win32.WaveHdrFlags.WHDR_DONE) > 0) sb.Append("DONE ");

            return sb.ToString();
        }
    }
}
