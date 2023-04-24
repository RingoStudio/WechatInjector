using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RS_WX_INJECTOR.utils
{
    public class PipeHelper
    {
        private NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "RS_WX_PIPE", PipeDirection.InOut); //创建命名管道
        private static PipeHelper instance = new PipeHelper();
        public static PipeHelper Instance() => instance;
        private Queue<string> _sendQueue = new Queue<string>();
        private bool _Switch = false;
        public bool IsConnected { get => pipeClient.IsConnected; }
        private Thread connecting = null;

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
        public void DisConnect()
        {
            _Switch = false;
            Thread.Sleep(2500);
            if (IsConnected)
            {
                try
                {
                    pipeClient.Close();
                }
                catch (Exception ex)
                {

                    // return;
                }
            }
        }
        public void Connect()
        {
            _Switch = true;
            Thread thread = new Thread(Connecting);
            thread.IsBackground = true;
            thread.Start();
        }
        public void Send(string content)
        {
            //if (string.IsNullOrEmpty(content)) return;
            //_sendQueue.Enqueue(content);
            if (string.IsNullOrEmpty(content)) return;
            if (!IsConnected) return;
            using (StreamWriter sw = new StreamWriter(pipeClient))
            {
                sw.AutoFlush = true;
                sw.WriteLine(content);         //向客户端发送数据
                Console.WriteLine(">> Pipe 发送：" + content);
            }
        }

        private void Connecting()
        {
            var data = new byte[10240];
            string sendContent = null;
            while (_Switch)
            {
                try
                {
                    Console.WriteLine(">> Pipe Update");
                    Console.WriteLine(">> Pipe State " + pipeClient.IsConnected.ToString());
                    Thread.Sleep(1000);
                    if (!pipeClient.IsConnected)
                    {
                        Console.WriteLine(">> Pipe 正在连接");
                        pipeClient.Connect();
                        Console.WriteLine(">> Pipe 已连接");
                    }

                    //using (StreamWriter sw = new StreamWriter(pipeClient))
                    //{
                    //    _sendQueue.TryDequeue(out sendContent);
                    //    if (string.IsNullOrEmpty(sendContent)) sendContent = "0";
                    //    Console.WriteLine(">> Pipe 发送：" + sendContent);
                    //    sw.WriteLine(sendContent);         //向客户端发送数据
                    //    sw.Flush();
                    //}

                    var count = pipeClient.Read(data, 0, 10240);    //读取byte类型数据，返回读取的长度
                    if (count > 0) OnReceivedData(Encoding.Default.GetString(data, 0, count).Trim());

                }
                catch (Exception ex)
                {
                    Console.WriteLine(">> Pipe 错误：" + ex.ToString());
                    continue;
                }
            }
        }
        private void OnReceivedData(string content)
        {
            if (string.IsNullOrEmpty(content)) return;
            Console.WriteLine(">> Pipe 接收：" + content);
            OnReceivedMessage(content);
        }

        //pipeClient.Connect();							//连接管道
        //	StreamReader sr = new StreamReader(pipeClient);
        //var data = new byte[1024];
        //data = System.Text.Encoding.Default.GetBytes("send to server:");	//向服务器端发送byte数组
        //	pipeClient.Write(data, 0, data.Length);

        //	string temp = sr.ReadLine();        //从服务器读取数据
        //Console.WriteLine(temp);
        //	while(true)
        //	{
        //		StreamWriter writer = new StreamWriter(pipeClient);
        //Console.WriteLine("SendMessage:");
        //		var Input = Conole.ReadLine();      //输入要发送的字符串
        //writer.WriteLine(Input);			//向服务器端发送string字符串
        //		writer.Flush();						//清空发送buffer
        //		if (Input.Equals("quit"))			//输入quit时结束通信
        //		{
        //			break;
        //		}
        //		temp = sr.ReadLine();				//从服务器读取数据
        //		Console.WriteLine("Get Message:" + temp);
        //	}



    }
}
