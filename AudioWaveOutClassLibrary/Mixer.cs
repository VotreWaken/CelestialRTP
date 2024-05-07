//----------------------------------------------------------------------------
// File Name: Mixer.cs
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

namespace AudioWaveOut
{
    // Mixer
    public class Mixer
    {
        // MixBytes
        public static List<Byte> MixBytes(List<List<Byte>> listList, int BitsPerSample)
        {
            // Result
            List<Int32> list16 = new List<Int32>();
            List<Int32> list16Abs = new List<Int32>();
            int maximum = 0;

            // Ready
            return MixBytes_Intern(listList, BitsPerSample, out list16, out list16Abs, out maximum);
        }

        // MixBytes 
        public static List<Byte> MixBytes(List<List<Byte>> listList, int BitsPerSample, out List<Int32> listLinear, out List<Int32> listLinearAbs, out int maximum)
        {
            // Ready
            return MixBytes_Intern(listList, BitsPerSample, out listLinear, out listLinearAbs, out maximum);
        }

        // MixBytes_Intern
        private static List<Byte> MixBytes_Intern(List<List<Byte>> listList, int BitsPerSample, out List<Int32> listLinear, out List<Int32> listLinearAbs, out int maximum)
        {

            // Set default value
            listLinear = new List<Int32>();
            listLinearAbs = new List<Int32>();
            maximum = 0;

            // Determine maximum number of bytes for mixing
            int maxBytesCount = 0;
            foreach (List<Byte> l in listList)
            {
                if (l.Count > maxBytesCount)
                {
                    maxBytesCount = l.Count;
                }
            }

            // If data exists
            if (listList.Count > 0 && maxBytesCount > 0)
            {

                // Depending on BitsPerSample
                switch (BitsPerSample)
                {
                    // 8 Bits
                    case 8:
                        return MixBytes_8Bit(listList, maxBytesCount, out listLinear, out listLinearAbs, out maximum);

                    // 16 Bits
                    case 16:
                        return MixBytes_16Bit(listList, maxBytesCount, out listLinear, out listLinearAbs, out maximum);
                }
            }

            // Mistake
            return new List<Byte>();
        }

        // MixBytes_16Bit
        private static List<Byte> MixBytes_16Bit(List<List<Byte>> listList, int maxBytesCount, out List<Int32> listLinear, out List<Int32> listLinearAbs, out int maximum)
        {
            // Result
            maximum = 0;

            // Create array with linear and byte values
            int linearCount = maxBytesCount / 2;
            Int32[] bytesLinear = new Int32[linearCount];
            Int32[] bytesLinearAbs = new Int32[linearCount];
            Byte[] bytesRaw = new Byte[maxBytesCount];

            // For each byte list
            for (int v = 0; v < listList.Count; v++)
            {
                // Convert to array
                Byte[] bytes = listList[v].ToArray();

                // For every 16bit value
                for (int i = 0, a = 0; i < linearCount; i++, a += 2)
                {
                    // If there are values to mix
                    if (i < bytes.Length && a < bytes.Length - 1)
                    {
                        // Determine value
                        Int16 value16 = BitConverter.ToInt16(bytes, a);
                        int value32 = bytesLinear[i] + value16;

                        // Add value (catch overflows)
                        if (value32 < Int16.MinValue)
                        {
                            value32 = Int16.MinValue;
                        }
                        else if (value32 > Int16.MaxValue)
                        {
                            value32 = Int16.MaxValue;
                        }

                        // Set values
                        bytesLinear[i] = value32;
                        bytesLinearAbs[i] = Math.Abs(value32);
                        Int16 mixed16 = Convert.ToInt16(value32);
                        Array.Copy(BitConverter.GetBytes(mixed16), 0, bytesRaw, a, 2);

                        // Calculate maximum
                        if (value32 > maximum)
                        {
                            maximum = value32;
                        }
                    }
                    else
                    {
                        // Leave silent
                    }
                }
            }

            // Out result
            listLinear = new List<int>(bytesLinear);
            listLinearAbs = new List<int>(bytesLinearAbs);

            // Ready
            return new List<Byte>(bytesRaw);
        }

        // MixBytes_8Bit
        private static List<Byte> MixBytes_8Bit(List<List<Byte>> listList, int maxBytesCount, out List<Int32> listLinear, out List<Int32> listLinearAbs, out int maximum)
        {
            // Result
            maximum = 0;

            // Create array with linear and byte values
            int linearCount = maxBytesCount;
            Int32[] bytesLinear = new Int32[linearCount];
            Byte[] bytesRaw = new Byte[maxBytesCount];

            // For each byte list
            for (int v = 0; v < listList.Count; v++)
            {
                // Convert to array
                Byte[] bytes = listList[v].ToArray();

                // For every 8 bit value
                for (int i = 0; i < linearCount; i++)
                {
                    // If there are values to mix
                    if (i < bytes.Length)
                    {
                        // Determine value
                        Byte value8 = bytes[i];
                        int value32 = bytesLinear[i] + value8;

                        // Add value (catch overflows)
                        if (value32 < Byte.MinValue)
                        {
                            value32 = Byte.MinValue;
                        }
                        else if (value32 > Byte.MaxValue)
                        {
                            value32 = Byte.MaxValue;
                        }

                        // Set values
                        bytesLinear[i] = value32;
                        bytesRaw[i] = BitConverter.GetBytes(value32)[0];

                        // Calculate maximum
                        if (value32 > maximum)
                        {
                            maximum = value32;
                        }
                    }
                    else
                    {
                        // Leave silent
                    }
                }
            }

            // Out results
            listLinear = new List<int>(bytesLinear);
            listLinearAbs = new List<int>(bytesLinear);

            // Ready
            return new List<Byte>(bytesRaw);
        }

        // SubsctractBytes_16Bit
        public static List<Byte> SubsctractBytes_16Bit(List<Byte> listSource, List<Byte> listToSubstract)
        {
            // Result
            List<Byte> list = new List<byte>(listSource.Count);

            // Create array with linear values (16bit)
            int value16Count = listSource.Count / 2;
            List<Int16> list16Mixed = new List<Int16>(new Int16[value16Count]);

            // Convert to array
            Byte[] bytesSource = listSource.ToArray();
            Byte[] bytesSubstract = listToSubstract.ToArray();

            // For every 16bit value
            for (int i = 0, a = 0; i < value16Count; i++, a += 2)
            {
                // If values exist
                if (i < bytesSource.Length && a < bytesSource.Length - 1)
                {
                    // Determine values
                    Int16 value16Source = BitConverter.ToInt16(bytesSource, a);
                    Int16 value16Substract = BitConverter.ToInt16(bytesSubstract, a);
                    int value32 = value16Source - value16Substract;

                    // Add value (catch overflows)
                    if (value32 < Int16.MinValue)
                    {
                        value32 = Int16.MinValue;
                    }
                    else if (value32 > Int16.MaxValue)
                    {
                        value32 = Int16.MaxValue;
                    }

                    // Set value
                    list16Mixed[i] = Convert.ToInt16(value32);
                }
            }

            // For every value
            foreach (Int16 v16 in list16Mixed)
            {
                // Convert integers to bytes
                Byte[] bytes = BitConverter.GetBytes(v16);
                list.AddRange(bytes);

            }

            // Ready
            return list;
        }
    }
}
