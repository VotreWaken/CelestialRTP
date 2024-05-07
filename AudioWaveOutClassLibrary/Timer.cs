//----------------------------------------------------------------------------
// File Name: Timer.cs
// 
// Description: 
// Implement various types of timers, That based && defined into Win32.cs Class
// Timers: QueueTimer, EventTimer, Stopwatch
// Timer resolution - the smallest unit of time that can be accurately measured by
// that timer
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
    // QueueTimer
    public class QueueTimer
    {
        // Constructor
        public QueueTimer()
        {
            m_DelegateTimerProc = new global::AudioWaveOut.Win32.DelegateTimerProc(OnTimer);
        }

        // Variables
        private bool m_IsRunning = false;
        private uint m_Milliseconds = 20;
        private IntPtr m_HandleTimer = IntPtr.Zero;
        private GCHandle m_GCHandleTimer;
        private uint m_ResolutionInMilliseconds = 0;
        private IntPtr m_HandleTimerQueue;
        private GCHandle m_GCHandleTimerQueue;

        //Delegates And Events
        private global::AudioWaveOut.Win32.DelegateTimerProc m_DelegateTimerProc;
        public delegate void DelegateTimerTick();
        public event DelegateTimerTick TimerTick;

        // IsRunning
        public bool IsRunning
        {
            get
            {
                return m_IsRunning;
            }
        }

        // Milliseconds
        public uint Milliseconds
        {
            get
            {
                return m_Milliseconds;
            }
        }

        // ResolutionInMilliseconds
        public uint ResolutionInMilliseconds
        {
            get
            {
                return m_ResolutionInMilliseconds;
            }
        }

        // SetBestResolution
        public static void SetBestResolution()
        {
            // QueueTimer Determine resolution
            global::AudioWaveOut.Win32.TimeCaps tc = new global::AudioWaveOut.Win32.TimeCaps();
            global::AudioWaveOut.Win32.TimeGetDevCaps(ref tc, (uint)Marshal.SizeOf(typeof(global::AudioWaveOut.Win32.TimeCaps)));
            uint resolution = Math.Max(tc.wPeriodMin, 0);

            // QueueTimer Set resolution
            global::AudioWaveOut.Win32.TimeBeginPeriod(resolution);
        }

        // ResetResolution
        public static void ResetResolution()
        {
            // QueueTimer Determine resolution
            global::AudioWaveOut.Win32.TimeCaps tc = new global::AudioWaveOut.Win32.TimeCaps();
            global::AudioWaveOut.Win32.TimeGetDevCaps(ref tc, (uint)Marshal.SizeOf(typeof(global::AudioWaveOut.Win32.TimeCaps)));
            uint resolution = Math.Max(tc.wPeriodMin, 0);

            // QueueTimer Resolution deactivate
            global::AudioWaveOut.Win32.TimeBeginPeriod(resolution);
        }

        // Start
        public void Start(uint milliseconds, uint dueTimeInMilliseconds)
        {
            // Take Values
            m_Milliseconds = milliseconds;

            // Determine QueueTimer resolution
            global::AudioWaveOut.Win32.TimeCaps tc = new global::AudioWaveOut.Win32.TimeCaps();
            global::AudioWaveOut.Win32.TimeGetDevCaps(ref tc, (uint)Marshal.SizeOf(typeof(global::AudioWaveOut.Win32.TimeCaps)));
            m_ResolutionInMilliseconds = Math.Max(tc.wPeriodMin, 0);

            // Set QueueTimer Resolution
            global::AudioWaveOut.Win32.TimeBeginPeriod(m_ResolutionInMilliseconds);

            // QueueTimer Create queue
            m_HandleTimerQueue = global::AudioWaveOut.Win32.CreateTimerQueue();
            m_GCHandleTimerQueue = GCHandle.Alloc(m_HandleTimerQueue);

            // Try starting QueueTimer
            bool resultCreate = global::AudioWaveOut.Win32.CreateTimerQueueTimer(out m_HandleTimer, m_HandleTimerQueue, m_DelegateTimerProc, IntPtr.Zero, dueTimeInMilliseconds, m_Milliseconds, global::AudioWaveOut.Win32.WT_EXECUTEINTIMERTHREAD);
            if (resultCreate)
            {
                // Hold handle in memory
                m_GCHandleTimer = GCHandle.Alloc(m_HandleTimer, GCHandleType.Pinned);

                // QueueTimer has started
                m_IsRunning = true;
            }
        }

        // Stop
        public void Stop()
        {
            if (m_HandleTimer != IntPtr.Zero)
            {
                // End QueueTimer
                global::AudioWaveOut.Win32.DeleteTimerQueueTimer(IntPtr.Zero, m_HandleTimer, IntPtr.Zero);
                
                // End QueueTimer Resolution
                global::AudioWaveOut.Win32.TimeEndPeriod(m_ResolutionInMilliseconds);

                // QueueTimer Delete queue
                if (m_HandleTimerQueue != IntPtr.Zero)
                {
                    global::AudioWaveOut.Win32.DeleteTimerQueue(m_HandleTimerQueue);
                }

                // Release handles
                if (m_GCHandleTimer.IsAllocated)
                {
                    m_GCHandleTimer.Free();
                }
                if (m_GCHandleTimerQueue.IsAllocated)
                {
                    m_GCHandleTimerQueue.Free();
                }

                // Set variables
                m_HandleTimer = IntPtr.Zero;
                m_HandleTimerQueue = IntPtr.Zero;
                m_IsRunning = false;
            }
        }

        // OnTimer
        private void OnTimer(IntPtr lpParameter, bool TimerOrWaitFired)
        {
            if (TimerTick != null)
            {
                TimerTick();
            }
        }
    }

    // EventTimer
    public class EventTimer
    {
        // Constructor
        public EventTimer()
        {
            m_DelegateTimeEvent = new global::AudioWaveOut.Win32.TimerEventHandler(OnTimer);
        }

        // Variables
        private bool m_IsRunning = false;
        private uint m_Milliseconds = 20;
        private UInt32 m_TimerId = 0;
        private GCHandle m_GCHandleTimer;
        private UInt32 m_UserData = 0;
        private uint m_ResolutionInMilliseconds = 0;

        // Delegates And Events
        private global::AudioWaveOut.Win32.TimerEventHandler m_DelegateTimeEvent;
        public delegate void DelegateTimerTick();
        public event DelegateTimerTick TimerTick;

        // IsRunning
        public bool IsRunning
        {
            get
            {
                return m_IsRunning;
            }
        }

        // Milliseconds
        public uint Milliseconds
        {
            get
            {
                return m_Milliseconds;
            }
        }

        // ResolutionInMilliseconds
        public uint ResolutionInMilliseconds
        {
            get
            {
                return m_ResolutionInMilliseconds;
            }
        }

        // SetBestResolution
        public static void SetBestResolution()
        {
            // Determine QueueTimer resolution
            global::AudioWaveOut.Win32.TimeCaps tc = new global::AudioWaveOut.Win32.TimeCaps();
            global::AudioWaveOut.Win32.TimeGetDevCaps(ref tc, (uint)Marshal.SizeOf(typeof(global::AudioWaveOut.Win32.TimeCaps)));
            uint resolution = Math.Max(tc.wPeriodMin, 0);

            // Set QueueTimer Resolution
            global::AudioWaveOut.Win32.TimeBeginPeriod(resolution);
        }

        // ResetResolution
        public static void ResetResolution()
        {
            // Determine QueueTimer resolution
            global::AudioWaveOut.Win32.TimeCaps tc = new global::AudioWaveOut.Win32.TimeCaps();
            global::AudioWaveOut.Win32.TimeGetDevCaps(ref tc, (uint)Marshal.SizeOf(typeof(global::AudioWaveOut.Win32.TimeCaps)));
            uint resolution = Math.Max(tc.wPeriodMin, 0);

            // Disable QueueTimer Resolution
            global::AudioWaveOut.Win32.TimeEndPeriod(resolution);
        }

        // Start
        public void Start(uint milliseconds, uint dueTimeInMilliseconds)
        {
            // Take values
            m_Milliseconds = milliseconds;

            // Determine timer resolution
            global::AudioWaveOut.Win32.TimeCaps tc = new global::AudioWaveOut.Win32.TimeCaps();
            global::AudioWaveOut.Win32.TimeGetDevCaps(ref tc, (uint)Marshal.SizeOf(typeof(global::AudioWaveOut.Win32.TimeCaps)));
            m_ResolutionInMilliseconds = Math.Max(tc.wPeriodMin, 0);

            // Set timer resolution
            global::AudioWaveOut.Win32.TimeBeginPeriod(m_ResolutionInMilliseconds);

            // Try starting EventTimer
            m_TimerId = global::AudioWaveOut.Win32.TimeSetEvent(m_Milliseconds, m_ResolutionInMilliseconds, m_DelegateTimeEvent, ref m_UserData, (UInt32)Win32.TIME_PERIODIC);
            if (m_TimerId > 0)
            {
                // Hold handle in memory
                m_GCHandleTimer = GCHandle.Alloc(m_TimerId, GCHandleType.Pinned);

                // QueueTimer has started
                m_IsRunning = true;
            }
        }

        // Stop
        public void Stop()
        {
            if (m_TimerId > 0)
            {
                // End timer
                global::AudioWaveOut.Win32.TimeKillEvent(m_TimerId);

                // End timer resolution
                global::AudioWaveOut.Win32.TimeEndPeriod(m_ResolutionInMilliseconds);

                // Release handles
                if (m_GCHandleTimer.IsAllocated)
                {
                    m_GCHandleTimer.Free();
                }

                // Set variables
                m_TimerId = 0;
                m_IsRunning = false;
            }
        }

        // OnTimer
        private void OnTimer(UInt32 id, UInt32 msg, ref UInt32 userCtx, UInt32 rsv1, UInt32 rsv2)
        {
            if (TimerTick != null)
            {
                TimerTick();
            }
        }
    }

    // Stopwatch
    public class Stopwatch
    {
        // Constructor
        public Stopwatch()
        {
            // Check
            if (Win32.QueryPerformanceFrequency(out m_Frequency) == false)
            {
                throw new Exception("High Performance counter not supported");
            }
        }

        // Variables
        private long m_StartTime = 0;
        private long m_DurationTime = 0;
        private long m_Frequency;

        // Start
        public void Start()
        {
            Win32.QueryPerformanceCounter(out m_StartTime);
            m_DurationTime = m_StartTime;
        }

        // ElapsedMilliseconds
        public double ElapsedMilliseconds
        {
            get
            {
                Win32.QueryPerformanceCounter(out m_DurationTime);
                return (double)(m_DurationTime - m_StartTime) / (double)m_Frequency * 1000;
            }
        }
    }
}
