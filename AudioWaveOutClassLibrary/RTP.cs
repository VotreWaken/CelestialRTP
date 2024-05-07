//----------------------------------------------------------------------------
// File Name: RTP.cs
// 
// Description: 
// Implementing RTP Packet Class that w`ll be using at the OSI application level
//
// Responsible for parsing incoming data, checking their headers, deserializing the
// received data (since they arrive serialized in bits) and contains a method for
// serialization
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
    // RTPPacket
    public class RTPPacket
    {
        // Constructor
        public RTPPacket()
        {

        }

        // Constructor
        public RTPPacket(byte[] data)
        {
            Parse(data);
        }

        // Variables
        public static int MinHeaderLength = 12;
        public int HeaderLength = MinHeaderLength;
        public int Version = 0;
        public bool Padding = false;
        public bool Extension = false;
        public int CSRCCount = 0;
        public bool Marker = false;
        public int PayloadType = 0;
        public UInt16 SequenceNumber = 0;
        public uint Timestamp = 0;
        public uint SourceId = 0;
        public Byte[] Data;
        public UInt16 ExtensionHeaderId = 0;
        public UInt16 ExtensionLengthAsCount = 0;
        public Int32 ExtensionLengthInBytes = 0;

        // Parse
        private void Parse(Byte[] data)
        {
            if (data.Length >= MinHeaderLength)
            {
                Version = ValueFromByte(data[0], 6, 2);
                Padding = Convert.ToBoolean(ValueFromByte(data[0], 5, 1));
                Extension = Convert.ToBoolean(ValueFromByte(data[0], 4, 1));
                CSRCCount = ValueFromByte(data[0], 0, 4);
                Marker = Convert.ToBoolean(ValueFromByte(data[1], 7, 1));
                PayloadType = ValueFromByte(data[1], 0, 7);
                HeaderLength = MinHeaderLength + (CSRCCount * 4);

                //Sequence Number
                Byte[] seqNum = new Byte[2];
                seqNum[0] = data[3];
                seqNum[1] = data[2];
                SequenceNumber = System.BitConverter.ToUInt16(seqNum, 0);

                //TimeStamp
                Byte[] timeStmp = new Byte[4];
                timeStmp[0] = data[7];
                timeStmp[1] = data[6];
                timeStmp[2] = data[5];
                timeStmp[3] = data[4];
                Timestamp = System.BitConverter.ToUInt32(timeStmp, 0);

                //SourceId
                Byte[] srcId = new Byte[4];
                srcId[0] = data[8];
                srcId[1] = data[9];
                srcId[2] = data[10];
                srcId[3] = data[11];
                SourceId = System.BitConverter.ToUInt32(srcId, 0);

                // If Extension Header
                if (Extension)
                {
                    //ExtensionHeaderId
                    Byte[] extHeaderId = new Byte[2];
                    extHeaderId[1] = data[HeaderLength + 0];
                    extHeaderId[0] = data[HeaderLength + 1];
                    ExtensionHeaderId = System.BitConverter.ToUInt16(extHeaderId, 0);

                    //ExtensionHeaderLength
                    Byte[] extHeaderLength16 = new Byte[2];
                    extHeaderLength16[1] = data[HeaderLength + 2];
                    extHeaderLength16[0] = data[HeaderLength + 3];
                    ExtensionLengthAsCount = System.BitConverter.ToUInt16(extHeaderLength16.ToArray(), 0);

                    // Adjust header length (length times 4 bytes or Int32)
                    ExtensionLengthInBytes = ExtensionLengthAsCount * 4;
                    HeaderLength += ExtensionLengthInBytes + 4;
                }

                // Copy data
                Data = new Byte[data.Length - HeaderLength];
                Array.Copy(data, HeaderLength, this.Data, 0, data.Length - HeaderLength);
            }
        }

        // GetValueFromByte
        private Int32 ValueFromByte(Byte value, int startPos, int length)
        {
            Byte mask = 0;
            // Create mask
            for (int i = 0; i < length; i++)
            {
                mask = (Byte)(mask | 0x1 << startPos + i);
            }

            // Result
            Byte result = (Byte)((value & mask) >> startPos);

            // Ready
            return Convert.ToInt32(result);
        }

        // ToBytes
        public Byte[] ToBytes()
        {
            // Result
            Byte[] bytes = new Byte[this.HeaderLength + Data.Length];

            // Byte 0
            bytes[0] = (Byte)(Version << 6);
            bytes[0] |= (Byte)(Convert.ToInt32(Padding) << 5);
            bytes[0] |= (Byte)(Convert.ToInt32(Extension) << 4);
            bytes[0] |= (Byte)(Convert.ToInt32(CSRCCount));

            // Byte 1
            bytes[1] = (Byte)(Convert.ToInt32(Marker) << 7);
            bytes[1] |= (Byte)(Convert.ToInt32(PayloadType));

            // Byte 2 + 3
            Byte[] bytesSequenceNumber = BitConverter.GetBytes(SequenceNumber);
            bytes[2] = bytesSequenceNumber[1];
            bytes[3] = bytesSequenceNumber[0];

            // Byte 4 until 7
            Byte[] bytesTimeStamp = BitConverter.GetBytes(Timestamp);
            bytes[4] = bytesTimeStamp[3];
            bytes[5] = bytesTimeStamp[2];
            bytes[6] = bytesTimeStamp[1];
            bytes[7] = bytesTimeStamp[0];

            // Byte 8 until 11
            Byte[] bytesSourceId = BitConverter.GetBytes(SourceId);
            bytes[8] = bytesSourceId[3];
            bytes[9] = bytesSourceId[2];
            bytes[10] = bytesSourceId[1];
            bytes[11] = bytesSourceId[0];

            // Data
            Array.Copy(this.Data, 0, bytes, this.HeaderLength, this.Data.Length);

            // Ready
            return bytes;
        }
    }
}
