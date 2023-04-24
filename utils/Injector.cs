using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RS_WX_INJECTOR.utils
{

    public class Injector
    {
        /// <summary>
        /// DLL 文件名，从ini读取
        /// </summary>
        private static string _dllName = INIHelper.Read("progress", "dll_name", "3.2.1.121-0.0.0.015.dll");
        /// <summary>
        /// DLL 文件路径
        /// </summary>
        private static string _dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _dllName);
        /// <summary>
        /// 微信进程
        /// </summary>
        private static Process _WeChatProcess = null;
        /// <summary>
        /// 检测微信进程是否存在
        /// </summary>
        /// <param name="WeChatProcess"></param>
        /// <returns></returns>
        public static bool IsWxStartUp(out Process WeChatProcess)
        {
            Process[] processes = Process.GetProcesses();
            WeChatProcess = null;
            foreach (Process process in processes)
            {
                if (process.ProcessName.ToLower() == "WeChat".ToLower())
                {

                    WeChatProcess = process;
                    break;
                }
            }
            return WeChatProcess != null;
        }
        /// <summary>
        /// 启动微信机器人
        /// </summary>
        /// <param name="args"></param>
        public static void StartUpWechatRobot(string args="")
        {
            var process = new Process();
            var path = "RSvBot.exe";
            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = args;
            //process.StartInfo.Verb = "runas";
            process.Start();
        }
        /// <summary>
        /// 检测微信机器人是否启动
        /// </summary>
        /// <returns></returns>
        public static bool IsWechatRobotStartUp() => IsWechatRobotStartUp(out Process _);
        /// <summary>
        /// 检测微信机器人是否启动
        /// </summary>
        /// <param name="rsvbotProc"></param>
        /// <returns></returns>
        public static bool IsWechatRobotStartUp(out Process rsvbotProc)
        {
            Process[] processes = Process.GetProcesses();
            rsvbotProc = null;
            foreach (Process process in processes)
            {
                if (process.ProcessName.ToLower() == "rsvbot".ToLower())
                {

                    rsvbotProc = process;
                    break;
                }
            }

            return rsvbotProc != null;
        }
        /// <summary>
        /// 检测COM进程是否启动
        /// </summary>
        /// <param name="cwrPropcess"></param>
        /// <returns></returns>
        public static bool IsCWechatRobotStartUp(out Process cwrPropcess)
        {
            Process[] processes = Process.GetProcesses();
            cwrPropcess = null;
            foreach (Process process in processes)
            {
                if (process.ProcessName.ToLower() == "cwechatrobot".ToLower())
                {

                    cwrPropcess = process;
                    break;
                }
            }

            return cwrPropcess != null;
        }
    
        /// <summary>
        /// 检测微信状态
        /// </summary>
        /// <returns></returns>
        public static WeChatState CheckWxState()
        {
            Process process = null;
            if (!IsWxStartUp(out process)) return WeChatState.NotStart;
            var forms = FindAllForms();
            bool LoginForm = false, MainForm = false;
            foreach (var item in forms)
            {
                if (item.ClassName == "WeChatLoginWndForPC")
                {
                    LoginForm = true;
                    break;
                }
                if (item.ClassName == "WeChatMainWndForPC")
                {
                    MainForm = true;
                    break;
                }
            }
            if (LoginForm)
            {
                return WeChatState.Offline;
            }
            if (MainForm)
            {
                return WeChatState.Injected;
            }
            return WeChatState.Abnormal;
        }
        /// <summary>
        /// 检测是否注入
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static bool IsInjected(Process process = null)
        {

            if (process == null)
                if (!IsWxStartUp(out process))
                    return false;
            if (process == null) return false;

            if (!System.IO.File.Exists(_dllPath))
            {
                Console.WriteLine($"没有找到要注入的dll路径：{_dllPath}！");
                return false;
            }
            foreach (ProcessModule processModule in process.Modules)
            {
                // Console.WriteLine(">> MM " + processModule.ModuleName);
                if (processModule.ModuleName == _dllName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 注入进程
        /// </summary>
        /// <param name="dllPath"></param>
        /// <returns></returns>
        public static bool Inject()
        {
            if (!System.IO.File.Exists(_dllPath))
            {
                Console.WriteLine($"没有找到要注入的dll路径：{_dllPath}！");
                return false;
            }


            //1) 遍历系统中的进程，找到微信进程（CreateToolhelp32Snapshot、Process32Next）
            if (!IsWxStartUp(out _WeChatProcess))
            {
                Console.WriteLine("注入前请先启动微信！");
                return false;
            }

            if (IsInjected(_WeChatProcess))
            {
                Console.WriteLine("DLL文件“" + _dllPath + "”之前已注入!");
                return false;
            }

            //2) 打开微信进程，获得HANDLE（OpenProcess）。

            //3) 在微信进程中为DLL文件路径字符串申请内存空间（VirtualAllocEx）。

            //默认选择第一项

            int DllPathSize = _dllPath.Length * 2 + 1;
            int MEM_COMMIT = 0x00001000;
            int PAGE_READWRITE = 0x04;
            int DllAddress = VirtualAllocEx((int)_WeChatProcess.Handle, 0, DllPathSize, MEM_COMMIT, PAGE_READWRITE);
            if (DllAddress == 0)
            {
                Console.WriteLine("内存分配失败！");
                return false;
            }
            Console.WriteLine("内存地址:\t" + "0x" + DllAddress.ToString("X8") + Environment.NewLine);

            //4) 把DLL文件路径字符串写入到申请的内存中（WriteProcessMemory）
            if (WriteProcessMemory((int)_WeChatProcess.Handle, DllAddress, _dllPath, DllPathSize, 0) == false)
            {
                Console.WriteLine("内存写入失败！");
                return false;
            };


            //5) 从Kernel32.dll中获取LoadLibraryA的函数地址（GetModuleHandle、GetProcAddress）
            int module = GetModuleHandleA("Kernel32.dll");
            int LoadLibraryAddress = GetProcAddress(module, "LoadLibraryA");
            if (LoadLibraryAddress == 0)
            {
                Console.WriteLine("查找LoadLibraryA地址失败！");
                return false;
            }

            //6) 在微信中启动内存中指定了文件名路径的DLL（CreateRemoteThread）。
            if (CreateRemoteThread((int)_WeChatProcess.Handle, 0, 0, LoadLibraryAddress, DllAddress, 0, 0) == 0)
            {
                Console.WriteLine("执行远程线程失败！");
                return false;
            }

            Console.WriteLine("成功注入:\t" + _dllPath + Environment.NewLine);
            return true;
        }

        /// <summary>
        /// 检查并显示注入信息
        /// </summary>
        public static bool Update()
        {
            int WxId = 0;
            Process[] processes = Process.GetProcessesByName("WeChat");
            StringBuilder wxInfo = new StringBuilder();
            wxInfo.Append("刷新时间：\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
            wxInfo.Append("DLL位置：\t" + Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + Environment.NewLine);

            foreach (Process process in processes)
            {
                WxId = process.Id;
                wxInfo.Append("进程PID：\t" + process.Id + Environment.NewLine);
                wxInfo.Append("窗口标题：\t" + process.MainWindowTitle + Environment.NewLine);
                wxInfo.Append("启动时间：\t" + process.StartTime.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);

                //确定微信版本
                foreach (ProcessModule item in process.Modules)
                {
                    if (item.ModuleName.ToLower() != "WeChatWin.dll".ToLower()) continue;

                    wxInfo.Append("微信目录：\t" + System.IO.Path.GetDirectoryName(process.MainModule.FileName) + Environment.NewLine);
                    wxInfo.Append("微信版本：\t" + item.FileVersionInfo.FileVersion + Environment.NewLine);
                    wxInfo.Append("微信基址：\t" + "0x" + item.BaseAddress.ToString("X8") + Environment.NewLine);

                    break;
                }
                break;
            }
            Console.WriteLine(wxInfo.ToString());


            if (WxId == 0)
            {
                Console.WriteLine("错误信息：注入前请先启动微信！");
                return false;
            }
            return true;
        }
        /// <summary>
        /// 杀掉COM进程
        /// </summary>
        public static void KillCOM()
        {
            Process process = null;
            Console.WriteLine(">> 正在退出COM");
            if (IsCWechatRobotStartUp(out process))
            {
                process.Kill(true);
            }
        }
        /// <summary>
        /// 杀掉微信
        /// </summary>
        public static void KillBot()
        {
            Process process = null;
            Console.WriteLine(">> 正在退出Bot");
            if (IsWechatRobotStartUp(out process))
            {
                process.Kill(true);
            }
        }
        /// <summary>
        /// 重新启动微信
        /// </summary>
        public static void RestartWechat()
        {
            //Console.WriteLine(">> 0");

            //如果当前系统中，微信在运行，则重启微信
            String WxPath = "";
            Process process = null;
            if (IsWxStartUp(out process))
            {
                Console.WriteLine(">> 正在退出微信进程");
                WxPath = process.MainModule.FileName;
                process.Kill(true);
            }

            //启动微信
            if (WxPath == "")
            {
                try
                {
                    WxPath = @"C:\Program Files (x86)\Tencent\WeChat\[3.7.0.26]\WeChat.exe";//InstallPath.ToString() + "\\WeChat.exe";
                }
                catch (Exception ex)
                {
                    WxPath = "";
                }
            }

            if (WxPath != "")
            {
                Console.WriteLine(">> 正在重启微信");

                process = new Process();
                process.StartInfo.FileName = WxPath;
                //process.StartInfo.Verb = "runas";
                process.Start();
                Thread.Sleep(500);
                Update();
            }
            else
            {
                Console.WriteLine("在系统中未找到微信，请手动启动微信");
            }
        }
        /// <summary>
        /// 查找当前用户空间下所有符合条件的窗口。如果不指定条件，将仅查找可见窗口。
        /// </summary>
        /// <param name="match">过滤窗口的条件。如果设置为 null，将仅查找可见窗口。</param>
        /// <returns>找到的所有窗口信息。</returns>
        public static IReadOnlyList<WindowInfo> FindAllForms(Predicate<WindowInfo> match = null)
        {
            var windowList = new List<WindowInfo>();
            EnumWindows(OnWindowEnum, 0);
            return windowList.FindAll(match ?? DefaultPredicate);

            bool OnWindowEnum(IntPtr hWnd, int lparam)
            {
                // 仅查找顶层窗口。
                // 获取窗口类名。
                var lpString = new StringBuilder(512);
                GetClassName(hWnd, lpString, lpString.Capacity);
                var className = lpString.ToString();

                // 获取窗口标题。
                var lptrString = new StringBuilder(512);
                GetWindowText(hWnd, lptrString, lptrString.Capacity);
                var title = lptrString.ToString().Trim();

                // 获取窗口可见性。
                var isVisible = IsWindowVisible(hWnd);

                // 获取窗口位置和尺寸。
                LPRECT rect = default;
                GetWindowRect(hWnd, ref rect);
                var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

                // 添加到已找到的窗口列表。
                windowList.Add(new WindowInfo(hWnd, className, title, isVisible, bounds));
                //if (title != "微信") return true;
                //Console.WriteLine($"{title} - {className} - {hWnd} - {bounds.Width}x{bounds.Height}");
                return true;
            }
        }

        #region  WinApi

        [DllImport("Kernel32.dll")]
        //LPVOID VirtualAllocEx(
        //  HANDLE hProcess,
        //  LPVOID lpAddress,
        //  SIZE_T dwSize,
        //  DWORD flAllocationType,
        //  DWORD flProtect
        //);
        public static extern int VirtualAllocEx(int hProcess, int lpAddress, int dwSize, int flAllocationType, int flProtect);

        [DllImport("Kernel32.dll")]
        //BOOL WriteProcessMemory(
        //  HANDLE hProcess,
        //  LPVOID lpBaseAddress,
        //  LPCVOID lpBuffer,
        //  SIZE_T nSize,
        //  SIZE_T* lpNumberOfBytesWritten
        //);
        public static extern Boolean WriteProcessMemory(int hProcess, int lpBaseAddress, String lpBuffer, int nSize, int lpNumberOfBytesWritten);

        [DllImport("Kernel32.dll")]
        //HMODULE GetModuleHandleA(
        //  LPCSTR lpModuleName
        //);
        public static extern int GetModuleHandleA(String lpModuleName);

        [DllImport("Kernel32.dll")]
        //FARPROC GetProcAddress(
        //  HMODULE hModule,
        //  LPCSTR lpProcName
        //);
        public static extern int GetProcAddress(int hModule, String lpProcName);

        [DllImport("Kernel32.dll")]
        //HANDLE CreateRemoteThread(
        //  HANDLE hProcess,
        //  LPSECURITY_ATTRIBUTES lpThreadAttributes,
        //  SIZE_T dwStackSize,
        //  LPTHREAD_START_ROUTINE lpStartAddress,
        //  LPVOID lpParameter,
        //  DWORD dwCreationFlags,
        //  LPDWORD lpThreadId
        //);
        public static extern int CreateRemoteThread(int hProcess, int lpThreadAttributes, int dwStackSize, int lpStartAddress, int lpParameter, int dwCreationFlags, int lpThreadId);


        [DllImport("Kernel32.dll")]
        //BOOL VirtualFreeEx(
        //  HANDLE hProcess,
        //  LPVOID lpAddress,
        //  SIZE_T dwSize,
        //  DWORD dwFreeType
        //);
        public static extern Boolean VirtualFreeEx(int hProcess, int lpAddress, int dwSize, int dwFreeType);
        /// <summary>
        /// 默认的查找窗口的过滤条件。可见 + 非最小化 + 包含窗口标题。
        /// </summary>
        private static readonly Predicate<WindowInfo> DefaultPredicate = x => x.IsVisible && !x.IsMinimized && x.Title.Length > 0;

        private delegate bool WndEnumProc(IntPtr hWnd, int lParam);

        [DllImport("user32")]
        private static extern bool EnumWindows(WndEnumProc lpEnumFunc, int lParam);

        [DllImport("user32")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lptrString, int nMaxCount);

        [DllImport("user32")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32")]
        private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        [DllImport("user32")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref LPRECT rect);

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct LPRECT
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;
        }
    }
    public enum WeChatState
    {
        NotStart = 0,
        Online = 1,
        Injected = 2,
        Offline = 3,
        Abnormal = 4,
    }
    /// <summary>
    /// 获取 Win32 窗口的一些基本信息。
    /// </summary>
    public readonly struct WindowInfo
    {
        public WindowInfo(IntPtr hWnd, string className, string title, bool isVisible, Rectangle bounds) : this()
        {
            Hwnd = hWnd;
            ClassName = className;
            Title = title;
            IsVisible = isVisible;
            Bounds = bounds;
        }

        /// <summary>
        /// 获取窗口句柄。
        /// </summary>
        public IntPtr Hwnd { get; }

        /// <summary>
        /// 获取窗口类名。
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// 获取窗口标题。
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 获取当前窗口是否可见。
        /// </summary>
        public bool IsVisible { get; }

        /// <summary>
        /// 获取窗口当前的位置和尺寸。
        /// </summary>
        public Rectangle Bounds { get; }

        /// <summary>
        /// 获取窗口当前是否是最小化的。
        /// </summary>
        public bool IsMinimized => Bounds.Left == -32000 && Bounds.Top == -32000;
    }
    #endregion
}
