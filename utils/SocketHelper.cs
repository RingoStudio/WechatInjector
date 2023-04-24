using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RS_WX_INJECTOR.utils
{
    public class IPC
    {
        #region EVENTS
        /// <summary>
        /// 消息事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数类型</typeparam>
        /// <param name="sender">Bot Id</param>
        /// <param name="eventArgs">事件参数</param>
        public delegate void PipeMessageHandler(string message);

        public event PipeMessageHandler OnReceivedMessage;

        #endregion
        private static IPC instance = new IPC();
        public static IPC Instance() => instance;
        private string _host = "127.0.0.1";
        private int _port = 6000;
        private SocketConnection socketConnection;
        private bool _isConnected = false;
        public bool IsConnected { get => _isConnected; }
        public void Connect()
        {
            IPAddress ip = IPAddress.Parse(_host);
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketConnection = new SocketConnection(clientSocket);
            socketConnection.Connect(ip, _port);
            Console.WriteLine(">> Socket已经打开...");
            _isConnected = true;
            //string sendStr = "hello server";
            //socketConnection.Send(sendStr);
            //Console.WriteLine(">> Socket发送信息");
            //socketConnection.Dispose();
            socketConnection.OnReceivedMessage += OnMsgArrived;
            socketConnection.ReceiveData();
        }
        private void OnMsgArrived(string content) => OnReceivedMessage(content);
        public void Send(string content)
        {
            if (!IsConnected) return;
            try
            {
                socketConnection.Send(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public class ClientSession
    {
        public Socket ClientSocket { get; set; }
        public string IP;
        public ClientSession(Socket clientSocket)
        {
            this.ClientSocket = clientSocket;
            this.IP = GetIPStr();
        }
        public string GetIPStr()
        {
            string resStr = ((IPEndPoint)ClientSocket.RemoteEndPoint).Address.ToString();
            return resStr;
        }
    }
    public class SocketConnection : IDisposable
    {
        #region EVENTS
        /// <summary>
        /// 消息事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数类型</typeparam>
        /// <param name="sender">Bot Id</param>
        /// <param name="eventArgs">事件参数</param>
        public delegate void PipeMessageHandler(string message);

        public event PipeMessageHandler OnReceivedMessage;

        #endregion
        public Byte[] msgBuffer = new byte[1024];
        private Socket _clientSocket = null;
        public Socket ClientSocket
        {
            get { return this._clientSocket; }
        }
        #region 构造
        public SocketConnection(Socket sock)
        {
            this._clientSocket = sock;
        }
        #endregion
        #region 连接
        public void Connect(IPAddress ip, int port)
        {
            this.ClientSocket.BeginConnect(ip, port, ConnectCallback, this.ClientSocket);
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                handler.EndConnect(ar);
            }
            catch (SocketException ex)
            {

            }
        }
        #endregion
        #region 发送数据
        public void Send(string data)
        {
            Send(System.Text.Encoding.UTF8.GetBytes(data));
        }
        private void Send(byte[] byteData)
        {
            try
            {
                this.ClientSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), this.ClientSocket);
            }
            catch (SocketException ex)
            {

            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                handler.EndSend(ar);
            }
            catch (SocketException ex)
            {

            }
        }
        #endregion
        #region 接收数据
        public void ReceiveData()
        {
            ClientSocket.BeginReceive(msgBuffer, 0, msgBuffer.Length, 0, new AsyncCallback(ReceiveCallback), null);
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int REnd = ClientSocket.EndReceive(ar);
                if (REnd > 0)
                {
                    byte[] data = new byte[REnd];
                    Array.Copy(msgBuffer, 0, data, 0, REnd);

                    //在此处对数据进行处理
                    //在此处对数据进行处理
                    
                        OnReceivedMessage(Encoding.Default.GetString(data));

                    ClientSocket.BeginReceive(msgBuffer, 0, msgBuffer.Length, 0, new AsyncCallback(ReceiveCallback), null);
                }
                else
                {
                    Dispose();
                }
            }
            catch (SocketException ex)
            {

            }
        }
        public void Dispose()
        {
            try
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Close();
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
    }
}
