using RS_WX_INJECTOR.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RS_WX_INJECTOR.task
{
    public class MainTask
    {
        private IPC ipc = IPC.Instance();
        private int interval_broken;
        private int interval_normal;
        private int interval_waitnext = 10000;
        private int interval_reboot = 60000;
        private WeChatState lastStat = WeChatState.NotStart;
        public MainTask()
        {
            Console.WriteLine(">> 守护进程开始运行");
            interval_broken = Convert.ToInt32(utils.INIHelper.Read("progress", "interval_reconnect", "10000"));
            interval_normal = Convert.ToInt32(utils.INIHelper.Read("progress", "interval_connected", "30000"));
            interval_waitnext = Convert.ToInt32(utils.INIHelper.Read("progress", "interval_waitnext", "10000"));
            interval_reboot = Convert.ToInt32(utils.INIHelper.Read("progress", "interval_reboot", "60000"));
            if (!Settings.DEBUG_MODE)
            {
                ipc.OnReceivedMessage += OnPipeRecv;
                ipc.Connect();
            }
        }
        public void Start()
        {
            Settings.REBOOTING = false;
            if (Switch_Running)
            {
                Console.WriteLine(">> 守护进程已经在运行");
                return;
            }
            Switch_Running = true;
            Thread task = new Thread(Updating);
            task.Name = "MainTask";
            task.IsBackground = false;
            task.Start();
            Console.WriteLine(">> 守护进程开启检测");
        }
        public void Stop()
        {
            Console.WriteLine(">> 守护进程停止检测");
            Switch_Running = false;
            Thread.Sleep(4000);
        }
        /// <summary>
        /// 由bot发起的重启指令
        /// </summary>
        public void RebootBot()
        {
            Console.WriteLine(">> 等待BOT重启");
            Settings.REBOOTING = true;
            Stop();

            Thread.Sleep(interval_reboot);

            int wait_times = 0;
            while (Injector.IsWechatRobotStartUp())
            {
                wait_times++;
                Thread.Sleep(10000);
                if (wait_times > 15)
                {
                    Injector.KillBot();
                    Thread.Sleep(10000);
                    break;
                }
            }

            Injector.KillCOM();
            Thread.Sleep(interval_reboot);
            Console.WriteLine(">> 正在重新启动BOT");
            Injector.StartUpWechatRobot();
            Environment.Exit(0);
        }
        private void OnPipeRecv(string content)
        {
            Console.WriteLine(">> 获得指令 " + content);
            switch (content)
            {
                case "exit":
                    Environment.Exit(0);
                    break;
                case "start":
                    Start();
                    break;
                case "stop":
                    Stop();
                    break;
                case "restart":
                    Stop();
                    Start();
                    break;
                case "reboot":
                    RebootBot();
                    break;
                default:
                    break;
            }
        }

        private bool Switch_Running = false;
        private bool Send_Email = false;
        private bool Checked = false;

        private void Updating()
        {
            do
            {
                if (!Settings.REBOOTING && !Settings.DEBUG_MODE && !utils.Injector.IsWechatRobotStartUp())
                {
                    //如果主进程退出，杀掉COM
                    Injector.KillCOM();
                    Environment.Exit(0);
                }
                try
                {
                    switch (Injector.CheckWxState())
                    {
                        case WeChatState.Injected:
                            if (lastStat != WeChatState.Injected)
                            {
                                Console.WriteLine(">> 微信注入完成");
                                if (!Settings.DEBUG_MODE) ipc.Send("injected");
                                Checked = true;
                            }
                            System.Threading.Thread.Sleep(interval_normal);
                            lastStat = WeChatState.Injected;
                            break;
                        case WeChatState.NotStart:
                            Injector.RestartWechat();
                            Console.WriteLine(">> 重新启动微信");
                            System.Threading.Thread.Sleep(interval_waitnext);
                            lastStat = WeChatState.NotStart;
                            Checked = false;
                            break;
                        case WeChatState.Offline:
                            if (lastStat != WeChatState.Offline)
                            {
                                Console.WriteLine(">> 微信需要重新登录");
                                WhenWxOffline();
                                System.Threading.Thread.Sleep(interval_broken);
                            }
                            Checked = false;
                            lastStat = WeChatState.Offline;
                            break;
                        case WeChatState.Online:
                            Console.WriteLine(">> 微信需要注入");
                            if (!Settings.DEBUG_MODE) ipc.Send("broken");
                            WhenWxOnline();
                            System.Threading.Thread.Sleep(interval_normal);
                            lastStat = WeChatState.Online;
                            break;
                        case WeChatState.Abnormal:
                            Console.WriteLine(">> 微信状态异常，需要重启");
                            Injector.RestartWechat();
                            System.Threading.Thread.Sleep(interval_normal);
                            Checked = false;
                            lastStat = WeChatState.Abnormal;
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + ex.StackTrace);
                }
            } while (Switch_Running);
        }
        private void WhenWxOffline()
        {
            if (!Send_Email)
            {
                if (utils.EmailHelper.SendMail("RSRobot Need Login!!!",
                  utils.INIHelper.Read("notifier", "notice_msg_wxbroken", "RSRobot 需要登录!") + "\r\n" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"))) ;
                {
                    Send_Email = true;
                    Console.WriteLine(">> 已发送邮件提示");
                }
            }
        }
        private void WhenWxOnline()
        {
            Send_Email = false;
            return;
            if (Injector.IsInjected())
            {
                return;
            }
            else
            {
                Console.WriteLine(">> 开始注入");
                if (Injector.Inject())
                {
                    Checked = true;
                    if (!Settings.DEBUG_MODE) ipc.Send("injected");
                }
            }
        }

    }
}

