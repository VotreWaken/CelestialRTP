//----------------------------------------------------------------------------
// File Name: WaveFile.cs
// 
// Description: 
// The WAVE file format is a subset of Microsoft's RIFF specification for the storage
// of multimedia files. A RIFF file starts out with a file header followed by a sequence
// of data chunks
//
// For more detailed information, read the documentation
// http://soundfile.sapp.org/doc/WaveFormat/
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

using System.Text;

namespace AudioWaveOut
{
    // WaveFile
    public class WaveFile
    {
        // Constructor
        public WaveFile()
        {

        }

        // Const Data
        public const int WAVE_FORMAT_PCM = 1;

        // Create New WaveFile
        public static void Create(string fileName, uint samplesPerSecond, short bitsPerSample, short channels, Byte[] data)
        {
            // Delete existing file
            if (System.IO.File.Exists(fileName))
            {
                System.IO.File.Delete(fileName);
            }

            // Create header
            WaveFileHeader header = CreateNewWaveFileHeader(samplesPerSecond, bitsPerSample, channels, (uint)(data.Length), 44 + data.Length);
            
            // Write header
            WriteHeader(fileName, header);

            // Write data
            WriteData(fileName, header.DATAPos, data);
        }


        // Append Data To existing file
        public static void AppendData(string fileName, Byte[] data)
        {
            AppendData(fileName, data, false);
        }

        // Append Data To existing file
        public static void AppendData(string fileName, Byte[] data, bool forceWriting)
        {
            // Read header
            WaveFileHeader header = ReadHeader(fileName);

            // If data exists
            if (header.DATASize > 0 || forceWriting)
            {
                // Add data
                WriteData(fileName, (int)(header.DATAPos + header.DATASize), data);

                // Update headers
                header.DATASize += (uint)data.Length;
                header.RiffSize += (uint)data.Length;

                // Overwrite header
                WriteHeader(fileName, header);
            }
        }

        // Read
        public static WaveFileHeader Read(string fileName)
        {
            // Read headers
            WaveFileHeader header = ReadHeader(fileName);

            // Ready
            return header;
        }

        // CreateWaveFileHeader
        private static WaveFileHeader CreateNewWaveFileHeader(uint SamplesPerSecond, short BitsPerSample, short Channels, uint dataSize, long fileSize)
        {
            // Create header
            WaveFileHeader Header = new WaveFileHeader();

            // Set values
            Array.Copy("RIFF".ToArray<Char>(), Header.RIFF, 4);
            Header.RiffSize = (uint)(fileSize - 8);
            Array.Copy("WAVE".ToArray<Char>(), Header.RiffFormat, 4);
            Array.Copy("fmt ".ToArray<Char>(), Header.FMT, 4);
            Header.FMTSize = 16;
            Header.AudioFormat = WAVE_FORMAT_PCM;
            Header.Channels = (short)Channels;
            Header.SamplesPerSecond = (uint)SamplesPerSecond;
            Header.BitsPerSample = (short)BitsPerSample;
            Header.BlockAlign = (short)((BitsPerSample * Channels) >> 3);
            Header.BytesPerSecond = (uint)(Header.BlockAlign * Header.SamplesPerSecond);
            Array.Copy("data".ToArray<Char>(), Header.DATA, 4);
            Header.DATASize = dataSize;

            // Ready
            return Header;
        }

        // Read Header
        private static WaveFileHeader ReadHeader(string fileName)
        {
            // Result
            WaveFileHeader header = new WaveFileHeader();

            // If the file exists
            if (File.Exists(fileName))
            {
                // Open file
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                System.IO.BinaryReader rd = new System.IO.BinaryReader(fs, Encoding.UTF8);

                // To read
                if (fs.CanRead)
                {
                    // Chunk 1
                    header.RIFF = rd.ReadChars(4);
                    header.RiffSize = (uint)rd.ReadInt32();
                    header.RiffFormat = rd.ReadChars(4);

                    // Chunk 2
                    header.FMT = rd.ReadChars(4);
                    header.FMTSize = (uint)rd.ReadInt32();
                    header.FMTPos = fs.Position;
                    header.AudioFormat = (short)rd.ReadInt16();
                    header.Channels = (short)rd.ReadInt16();
                    header.SamplesPerSecond = (uint)rd.ReadInt32();
                    header.BytesPerSecond = (uint)rd.ReadInt32();
                    header.BlockAlign = (short)rd.ReadInt16();
                    header.BitsPerSample = (short)rd.ReadInt16();

                    // Go to the beginning of Chunk3
                    fs.Seek(header.FMTPos + header.FMTSize, SeekOrigin.Begin);

                    // Chunk 3
                    header.DATA = rd.ReadChars(4);
                    header.DATASize = (uint)rd.ReadInt32();
                    header.DATAPos = (int)fs.Position;

                    // If not DATA
                    if (new String(header.DATA).ToUpper() != "DATA")
                    {
                        uint DataChunkSize = header.DATASize + 8;
                        fs.Seek(DataChunkSize, SeekOrigin.Current);
                        header.DATASize = (uint)(fs.Length - header.DATAPos - DataChunkSize);
                    }

                    // Read payload
                    if (header.DATASize <= fs.Length - header.DATAPos)
                    {
                        header.Payload = rd.ReadBytes((int)header.DATASize);
                    }
                }

                // Close
                rd.Close();
                fs.Close();
            }

            // Ready
            return header;
        }

        // WriteHeader
        public static void WriteHeader(string fileName, WaveFileHeader header)
        {
            // Open file
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            System.IO.BinaryWriter wr = new System.IO.BinaryWriter(fs, Encoding.UTF8);

            // Chunk 1
            wr.Write(header.RIFF);
            wr.Write(Int32ToBytes((int)header.RiffSize));
            wr.Write(header.RiffFormat);

            // Chunk 2
            wr.Write(header.FMT);
            wr.Write(Int32ToBytes((int)header.FMTSize));
            wr.Write(Int16ToBytes(header.AudioFormat));
            wr.Write(Int16ToBytes(header.Channels));
            wr.Write(Int32ToBytes((int)header.SamplesPerSecond));
            wr.Write(Int32ToBytes((int)header.BytesPerSecond));
            wr.Write(Int16ToBytes((short)header.BlockAlign));
            wr.Write(Int16ToBytes((short)header.BitsPerSample));

            // Chunk 3
            wr.Write(header.DATA);
            wr.Write(Int32ToBytes((int)header.DATASize));

            // Close file
            wr.Close();
            fs.Close();
        }

        // WriteData
        public static void WriteData(string fileName, int pos, Byte[] data)
        {
            // Open file
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            System.IO.BinaryWriter wr = new System.IO.BinaryWriter(fs, Encoding.UTF8);

            // Go to writing position
            wr.Seek(pos, System.IO.SeekOrigin.Begin);

            // Write data
            wr.Write(data);

            //Ready
            wr.Close();
            fs.Close();
        }

        // BytesToInt32
        private static int BytesToInt32(ref Byte[] bytes)
        {
            int Int32 = 0;
            Int32 = (Int32 << 8) + bytes[3];
            Int32 = (Int32 << 8) + bytes[2];
            Int32 = (Int32 << 8) + bytes[1];
            Int32 = (Int32 << 8) + bytes[0];
            return Int32;
        }

        // BytesToInt16
        private static short BytesToInt16(ref Byte[] bytes)
        {
            short Int16 = 0;
            Int16 = (short)((Int16 << 8) + bytes[1]);
            Int16 = (short)((Int16 << 8) + bytes[0]);
            return Int16;
        }

        // Int32ToByte
        private static Byte[] Int32ToBytes(int value)
        {
            Byte[] bytes = new Byte[4];
            bytes[0] = (Byte)(value & 0xFF);
            bytes[1] = (Byte)(value >> 8 & 0xFF);
            bytes[2] = (Byte)(value >> 16 & 0xFF);
            bytes[3] = (Byte)(value >> 24 & 0xFF);
            return bytes;
        }

        // Int16ToBytes
        private static Byte[] Int16ToBytes(short value)
        {
            Byte[] bytes = new Byte[2];
            bytes[0] = (Byte)(value & 0xFF);
            bytes[1] = (Byte)(value >> 8 & 0xFF);
            return bytes;
        }
    }

    // WaveFileHeader
    public class WaveFileHeader
    {
        // Constructor
        public WaveFileHeader()
        {

        }

        // Chunk 1
        public Char[] RIFF = new Char[4];
        public uint RiffSize = 8;
        public Char[] RiffFormat = new Char[4];

        // Chunk 2
        public Char[] FMT = new Char[4];
        public uint FMTSize = 16;
        public short AudioFormat;
        public short Channels;
        public uint SamplesPerSecond;
        public uint BytesPerSecond;
        public short BlockAlign;
        public short BitsPerSample;

        // Chunk 3
        public Char[] DATA = new Char[4];
        public uint DATASize;

        // Data
        public Byte[] Payload = new Byte[0];

        // Header Length
        public int DATAPos = 44;
        // Position FormatSize
        public long FMTPos = 20;


        // Duration 
        public TimeSpan Duration
        {
            get
            {
                int blockAlign = ((BitsPerSample * Channels) >> 3);
                int bytesPerSec = (int)(blockAlign * SamplesPerSecond);
                double value = (double)Payload.Length / (double)bytesPerSec;

                // Ready
                return new TimeSpan(0, 0, (int)value);
            }
        }
    }
}
