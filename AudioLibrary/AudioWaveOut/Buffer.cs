//----------------------------------------------------------------------------
// File Name: Buffer.cs
// 
// Description: 
// This element reorders and removes duplicate RTP packets as they are
// received from a network source. instantiates Number of packets in the buffer
// and Add Data to a buffer
//
// The element needs the clock-rate of the RTP payload in order to estimate the delay.
// Initialize and start Timer and provides Interval In Milliseconds
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
    // Buffer
    public class Buffer
    {
        // Constructor
        public Buffer(Object sender, uint maxRTPPackets, uint timerIntervalInMilliseconds)
        {
            // Maintain minimum number
            if (maxRTPPackets < 2)
            {
                throw new Exception("Wrong Arguments. Minimum maxRTPPackets is 2");
            }

            m_Sender = sender;
            m_MaxRTPPackets = maxRTPPackets;
            m_TimerIntervalInMilliseconds = timerIntervalInMilliseconds;

            Init();
        }

        // Variables
        private Object m_Sender = null;
        private uint m_MaxRTPPackets = 10;
        private uint m_TimerIntervalInMilliseconds = 20;
        private global::AudioWaveOut.EventTimer m_Timer = new global::AudioWaveOut.EventTimer();
        private System.Collections.Generic.Queue<RTPPacket> m_Buffer = new Queue<RTPPacket>();
        private RTPPacket m_LastRTPPacket = new RTPPacket();
        private bool m_Underflow = true;
        private bool m_Overflow = false;

        // Delegates And Event
        public delegate void DelegateDataAvailable(Object sender, RTPPacket packet);
        public event DelegateDataAvailable DataAvailable;

        // Number of packets in the buffer
        public int Length
        {
            get
            {
                return m_Buffer.Count;
            }
        }

        // Maximum number of RTP packets
        public uint Maximum
        {
            get
            {
                return m_MaxRTPPackets;
            }
        }

        // Interval In Milliseconds
        public uint IntervalInMilliseconds
        {
            get
            {
                return m_TimerIntervalInMilliseconds;
            }
        }

        // Init
        private void Init()
        {
            InitTimer();
        }

        // InitTimer
        private void InitTimer()
        {
            m_Timer.TimerTick += new EventTimer.DelegateTimerTick(OnTimerTick);
        }

        // Start
        public void Start()
        {
            m_Timer.Start(m_TimerIntervalInMilliseconds, 0);
            m_Underflow = true;
        }

        // Stop
        public void Stop()
        {
            m_Timer.Stop();
            m_Buffer.Clear();
        }

        // OnTimerTick
        private void OnTimerTick()
        {
            try
            {
                if (DataAvailable != null)
                {
                    // If data exists
                    if (m_Buffer.Count > 0)
                    {
                        // If overflow
                        if (m_Overflow)
                        {
                            // Wait until buffer is half empty
                            if (m_Buffer.Count <= m_MaxRTPPackets / 2)
                            {
                                m_Overflow = false;
                            }
                        }

                        // If underflow
                        if (m_Underflow)
                        {
                            // Wait until buffer is half full
                            if (m_Buffer.Count < m_MaxRTPPackets / 2)
                            {
                                return;
                            }
                            else
                            {
                                m_Underflow = false;
                            }
                        }

                        // Send data
                        m_LastRTPPacket = m_Buffer.Dequeue();
                        DataAvailable(m_Sender, m_LastRTPPacket);
                    }
                    else
                    {
                        // No overflow
                        m_Overflow = false;

                        // If buffer empty
                        if (m_LastRTPPacket != null && m_Underflow == false)
                        {
                            if (m_LastRTPPacket.Data != null)
                            {
                                // Underflow present
                                m_Underflow = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("JitterBuffer.cs | OnTimerTick() | {0}", ex.Message));
            }
        }

        // AddData
        public void AddData(RTPPacket packet)
        {
            try
            {
                // If no overflow
                if (m_Overflow == false)
                {
                    // No maximum size
                    if (m_Buffer.Count <= m_MaxRTPPackets)
                    {
                        m_Buffer.Enqueue(packet);
                    }
                    else
                    {
                        // Buffer overflow
                        m_Overflow = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("JitterBuffer.cs | AddData() | {0}", ex.Message));
            }
        }
    }
}
