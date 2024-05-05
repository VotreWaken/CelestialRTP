//----------------------------------------------------------------------------
// File Name: Utils.cs
// 
// Description: 
// Convert audio data from mu-law to linear and linear to mu-law
//
// Implementing an audio `sample' that represent is a single output value from an A/D converter,
// i.e., a small integer number (usually 8 or 16 bits), and audio data is just a series of such
// samples. It can be characterized by three parameters: the sampling rate (measured in samples
// per second or Hz, e.g., 8000 or 44100), the number of bits per sample (e.g., 8 or 16),
// and the number of channels (1 for mono, 2 for stereo, etc.)
//
// For more detailed information, read the Article
// https://www.dm.unibo.it/~achilles/calc/octave.html/Audio-Processing.html
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
    // Utils
    public class Utils
    {
        // Constructor
        public Utils()
        {

        }

        // Const Data
        const int SIGN_BIT = (0x80);
        const int QUANT_MASK = (0xf);
        const int NSEGS = (8);
        const int SEG_SHIFT = (4);
        const int SEG_MASK = (0x70);
        const int BIAS = (0x84);
        const int CLIP = 8159;
        static short[] seg_uend = new short[] { 0x3F, 0x7F, 0xFF, 0x1FF, 0x3FF, 0x7FF, 0xFFF, 0x1FFF };

        // GetBytesPerInterval
        public static int GetBytesPerInterval(uint SamplesPerSecond, int BitsPerSample, int Channels)
        {
            int blockAlign = ((BitsPerSample * Channels) >> 3);
            int bytesPerSec = (int)(blockAlign * SamplesPerSecond);
            uint sleepIntervalFactor = 1000 / 20; //20 Milliseconds
            int bytesPerInterval = (int)(bytesPerSec / sleepIntervalFactor);

            // Ready
            return bytesPerInterval;
        }

        // Mu-law To Linear
        // Convert audio data from mu-law to linear
        public static Int32 MulawToLinear(Int32 ulaw)
        {
            ulaw = ~ulaw;
            int t = ((ulaw & QUANT_MASK) << 3) + BIAS;
            t <<= (ulaw & SEG_MASK) >> SEG_SHIFT;
            return ((ulaw & SIGN_BIT) > 0 ? (BIAS - t) : (t - BIAS));
        }

        // Help Method to Search, Using with Convert the scaled magnitude to segment number
        static short Search(short val, short[] table, short size)
        {
            short i;
            int index = 0;
            for (i = 0; i < size; i++)
            {
                if (val <= table[index])
                {
                    return (i);
                }
                index++;
            }
            return (size);
        }

        // Linear To Mu-law.
        // Convert audio data from mu-law to linear
        public static Byte LinearToMulaw(short pcm_val)
        {
            short mask = 0;
            short seg = 0;
            Byte uval = 0;

            // Get the sign and the magnitude of the value. 
            pcm_val = (short)(pcm_val >> 2);
            if (pcm_val < 0)
            {
                pcm_val = (short)-pcm_val;
                mask = 0x7F;
            }
            else
            {
                mask = 0xFF;
            }

            // Clip the Magnitude 
            if (pcm_val > CLIP)
            {
                pcm_val = CLIP;
            }
            pcm_val += (BIAS >> 2);

            // Convert the scaled magnitude to segment number. 
            seg = Search(pcm_val, seg_uend, (short)8);

            
            // Combine the sign, segment, quantization bits;
            // and complement the code word.
            
            // Out of range, return maximum value.
            if (seg >= 8)
            {
                return (Byte)(0x7F ^ mask);
            }
            else
            {
                uval = (Byte)((seg << 4) | ((pcm_val >> (seg + 1)) & 0xF));
                return ((Byte)(uval ^ mask));
            }
        }

        // Mu-Law To Linear
        public static Byte[] MuLawToLinear(Byte[] bytes, int bitsPerSample, int channels)
        {
            // Number of tracks
            int blockAlign = channels * bitsPerSample / 8;

            // For every value
            Byte[] result = new Byte[bytes.Length * blockAlign];
            for (int i = 0, counter = 0; i < bytes.Length; i++, counter += blockAlign)
            {
                // Convert to bytes
                int value = MulawToLinear(bytes[i]);
                Byte[] values = BitConverter.GetBytes(value);

                switch (bitsPerSample)
                {
                    case 8:
                        switch (channels)
                        {
                            // 8 Bit 1 Channel
                            case 1:
                                result[counter] = values[0];
                                break;

                            // 8 Bit 2 Channel
                            case 2:
                                result[counter] = values[0];
                                result[counter + 1] = values[0];
                                break;
                        }
                        break;

                    case 16:
                        switch (channels)
                        {
                            // 16 Bit 1 Channel
                            case 1:
                                result[counter] = values[0];
                                result[counter + 1] = values[1];
                                break;

                            // 16 Bit 2 Channels
                            case 2:
                                result[counter] = values[0];
                                result[counter + 1] = values[1];
                                result[counter + 2] = values[0];
                                result[counter + 3] = values[1];
                                break;
                        }
                        break;
                }
            }

            // Ready
            return result;
        }

        /// Mu-Law To Linear with the number of bits per sample 32
        public static int[] MuLawToLinear32(Byte[] bytes, int bitsPerSample, int channels)
        {
            // Number of tracks
            int blockAlign = channels;

            // For every value
            int[] result = new int[bytes.Length * blockAlign];
            for (int i = 0, counter = 0; i < bytes.Length; i++, counter += blockAlign)
            {
                // Convert to Int32
                int value = MulawToLinear(bytes[i]);

                switch (bitsPerSample)
                {
                    case 8:
                        switch (channels)
                        {
                            // 8 Bit 1 Channel
                            case 1:
                                result[counter] = value;
                                break;

                            // 8 Bit 2 Channel
                            case 2:
                                result[counter] = value;
                                result[counter + 1] = value;
                                break;
                        }
                        break;

                    case 16:
                        switch (channels)
                        {
                            // 16 Bit 1 Channel
                            case 1:
                                result[counter] = value;
                                break;

                            // 16 Bit 2 Channels
                            case 2:
                                result[counter] = value;
                                result[counter + 1] = value;
                                break;
                        }
                        break;
                }
            }

            // Ready
            return result;
        }

        // Linear To Mu-Law
        public static Byte[] LinearToMulaw(Byte[] bytes, int bitsPerSample, int channels)
        {
            // Number of tracks
            int blockAlign = channels * bitsPerSample / 8;

            // Result
            Byte[] result = new Byte[bytes.Length / blockAlign];
            int resultIndex = 0;
            for (int i = 0; i < result.Length; i++)
            {
                // Depending on the resolution
                switch (bitsPerSample)
                {
                    case 8:
                        switch (channels)
                        {
                            // 8 Bit 1 Channel
                            case 1:
                                result[i] = LinearToMulaw(bytes[resultIndex]);
                                resultIndex += 1;
                                break;

                            // 8 Bit 2 Channel
                            case 2:
                                result[i] = LinearToMulaw(bytes[resultIndex]);
                                resultIndex += 2;
                                break;
                        }
                        break;

                    case 16:
                        switch (channels)
                        {
                            // 16 Bit 1 Channel
                            case 1:
                                result[i] = LinearToMulaw(BitConverter.ToInt16(bytes, resultIndex));
                                resultIndex += 2;
                                break;

                            // 16 Bit 2 Channels
                            case 2:
                                result[i] = LinearToMulaw(BitConverter.ToInt16(bytes, resultIndex));
                                resultIndex += 4;
                                break;
                        }
                        break;
                }
            }

            // Ready
            return result;
        }

        // Get Standard Derivation
        public static double GetStandardDerivation(System.Collections.Generic.List<double> list)
        {
            // Copy
            List<double> listCopy = new List<double>(list);

            // Calculate average
            double average = listCopy.Average();

            // Sums of squares of improvements
            double sum = 0;
            foreach (double value in listCopy)
            {
                double diff = average - value;
                sum += Math.Pow(diff, 2);
            }

            // Result
            return Math.Sqrt(sum / (listCopy.Count - 1));
        }
    }
}
