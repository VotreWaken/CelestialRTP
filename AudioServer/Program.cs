using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AudioServer
{
    public class TCPServer
    {
        private IPEndPoint m_endpoint;
        private TcpListener m_tcpip;

        private CancellationTokenSource m_cts;
        private ListenerState m_State;

        private List<ServerThread> m_threads = new List<ServerThread>();

        public delegate void DelegateClientConnected(ServerThread st);
        public delegate void DelegateClientDisconnected(ServerThread st, string info);
        public delegate void DelegateDataReceived(ServerThread st, byte[] data);

        public event DelegateClientConnected ClientConnected;
        public event DelegateClientDisconnected ClientDisconnected;
        public event DelegateDataReceived DataReceived;

        public enum ListenerState
        {
            None,
            Started,
            Stopped,
            Error
        };

        public List<ServerThread> Clients => m_threads;

        public ListenerState State => m_State;

        public TcpListener Listener => m_tcpip;

        public void Start(string strIPAdress, int Port)
        {
            m_endpoint = new IPEndPoint(IPAddress.Parse(strIPAdress), Port);
            m_tcpip = new TcpListener(m_endpoint);
            m_cts = new CancellationTokenSource();

            try
            {
                m_tcpip.Start();

                m_State = ListenerState.Started;

                Task.Run(() => RunAsync(m_cts.Token), m_cts.Token);
            }
            catch (Exception ex)
            {
                m_tcpip.Stop();
                m_State = ListenerState.Error;

                throw ex;
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {

                    TcpClient client = await m_tcpip.AcceptTcpClientAsync();

                    ServerThread st = new ServerThread(client);

                    st.DataReceived += OnDataReceived;
                    st.ClientDisconnected += OnClientDisconnected;

                    OnClientConnected(st);

                    _ = Task.Run(() => st.Receive(cancellationToken), cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public int Send(byte[] data)
        {
            List<ServerThread> list = new List<ServerThread>(m_threads);

            foreach (ServerThread sv in list)
            {
                try
                {
                    if (data.Length > 0)
                    {
                        sv.Send(data);
                    }
                }
                catch (Exception)
                {
                }
            }

            return m_threads.Count;
        }

        private void OnDataReceived(ServerThread st, byte[] data)
        {
            if (DataReceived != null)
            {
                DataReceived(st, data);
            }
        }

        private void OnClientDisconnected(ServerThread st, string info)
        {
            m_threads.Remove(st);

            if (ClientDisconnected != null)
            {
                ClientDisconnected(st, info);
            }
        }

        private void OnClientConnected(ServerThread st)
        {
            if (!m_threads.Contains(st))
            {
                m_threads.Add(st);
            }
            // st.Send(Encoding.UTF8.GetBytes(ServerName));  // Отправить имя сервера
            // st.Send(ServerPhoto);  // Отправить фотографию сервера в виде byte[]
            if (ClientConnected != null)
            {
                ClientConnected(st);
            }
        }

        public void Stop()
        {
            try
            {
                if (m_cts != null)
                {
                    m_cts.Cancel();
                }

                foreach (ServerThread st in m_threads)
                {
                    st.Stop();

                    if (ClientDisconnected != null)
                    {
                        ClientDisconnected(st, "Verbindung wurde beendet");
                    }
                }

                if (m_tcpip != null)
                {
                    m_tcpip.Stop();
                    m_tcpip.Server.Close();
                }

                m_threads.Clear();
                m_State = ListenerState.Stopped;
            }
            catch (Exception)
            {
                m_State = ListenerState.Error;
            }
        }
    }

    public class ServerThread
    {
        private bool m_IsStopped = false;
        private TcpClient m_Connection = null;
        public byte[] ReadBuffer = new byte[1024];
        public bool IsMute = false;
        public string Name = "";

        public delegate void DelegateDataReceived(ServerThread st, byte[] data);
        public event DelegateDataReceived DataReceived;
        public delegate void DelegateClientDisconnected(ServerThread sv, string info);
        public event DelegateClientDisconnected ClientDisconnected;

        public TcpClient Client => m_Connection;

        public bool IsStopped => m_IsStopped;

        public ServerThread(TcpClient connection)
        {
            this.m_Connection = connection;
        }

        public void Receive(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && m_Connection.Client.Connected)
            {
                try
                {
                    int bytesRead = m_Connection.Client.Receive(ReadBuffer);

                    if (bytesRead > 0)
                    {
                        byte[] data = new byte[bytesRead];
                        Array.Copy(ReadBuffer, 0, data, 0, bytesRead);

                        DataReceived(this, data);
                    }
                    else
                    {
                        HandleDisconnection("Verbindung wurde beendet");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    HandleDisconnection(ex.Message);
                    break;
                }
            }
        }

        public void HandleDisconnection(string reason)
        {
            m_IsStopped = true;

            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, reason);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                if (!m_IsStopped && m_Connection.Client.Connected)
                {
                    NetworkStream ns = m_Connection.GetStream();

                    lock (ns)
                    {
                        ns.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                m_Connection.Close();
                m_IsStopped = true;

                if (ClientDisconnected != null)
                {
                    ClientDisconnected(this, ex.Message);
                }

                throw ex;
            }
        }

        public void Stop()
        {
            if (m_Connection.Client.Connected)
            {
                m_Connection.Client.Disconnect(false);
            }

            m_IsStopped = true;
        }
    }
}
