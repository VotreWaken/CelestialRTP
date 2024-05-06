//----------------------------------------------------------------------------
// File Name: Protocols.cs
// 
// Description: 
// Determine the Transport protocols types on which the RTP protocol will be based.
//
// Responsible for serializing data into bytes and processing information retrieval.
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

using System.Text;

namespace AudioWaveOut
{
    // Protocols Types
    public enum ProtocolsTypes
    {
        TCP
    }

    // Protocol
    public class Protocol
    {
        // Constructor
        public Protocol(ProtocolsTypes type, Encoding encoding)
        {
            this.m_ProtocolType = type;
            this.m_Encoding = encoding;
        }

        // Variables
        private List<Byte> m_DataBuffer = new List<byte>();
        private const int m_MaxBufferLength = 10000;
        private ProtocolsTypes m_ProtocolType = ProtocolsTypes.TCP;
        private Encoding m_Encoding = Encoding.Default;
        public Object m_LockerReceive = new object();

        //Delegates And Events
        public delegate void DelegateDataComplete(Object sender, Byte[] data);
        public delegate void DelegateExceptionAppeared(Object sender, Exception ex);
        public event DelegateDataComplete DataComplete;
        public event DelegateExceptionAppeared ExceptionAppeared;


        // ToBytes
        public Byte[] ToBytes(Byte[] data)
        {
            try
            {
                // Bytes length
                Byte[] bytesLength = BitConverter.GetBytes(data.Length);

                // Putting it all together
                Byte[] allBytes = new Byte[bytesLength.Length + data.Length];
                Array.Copy(bytesLength, allBytes, bytesLength.Length);
                Array.Copy(data, 0, allBytes, bytesLength.Length, data.Length);

                // Ready
                return allBytes;
            }
            catch (Exception ex)
            {
                ExceptionAppeared(null, ex);
            }

            // Mistake
            return data;
        }

        // Receive_TCP_STX_ETX
        public void Receive_LH(Object sender, Byte[] data)
        {
            lock (m_LockerReceive)
            {
                try
                {
                    // Append data to buffer
                    m_DataBuffer.AddRange(data);

                    // Prevent buffer overflow
                    if (m_DataBuffer.Count > m_MaxBufferLength)
                    {
                        m_DataBuffer.Clear();
                    }

                    // Read bytes
                    Byte[] bytes = m_DataBuffer.Take(4).ToArray();

                    // Determine length
                    int length = (int)BitConverter.ToInt32(bytes.ToArray(), 0);

                    // Ensure maximum length
                    if (length > m_MaxBufferLength)
                    {
                        m_DataBuffer.Clear();
                    }

                    // As long as data is available
                    while (m_DataBuffer.Count >= length + 4)
                    {
                        // Extract data
                        Byte[] message = m_DataBuffer.Skip(4).Take(length).ToArray();

                        // Complete data notification
                        if (DataComplete != null)
                        {
                            DataComplete(sender, message);
                        }

                        // Remove data from buffer
                        m_DataBuffer.RemoveRange(0, length + 4);

                        // If further data is available
                        if (m_DataBuffer.Count > 4)
                        {
                            // Calculate new length
                            bytes = m_DataBuffer.Take(4).ToArray();
                            length = (int)BitConverter.ToInt32(bytes.ToArray(), 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Empty buffer
                    m_DataBuffer.Clear();
                    ExceptionAppeared(null, ex);
                }
            }
        }
    }
}
