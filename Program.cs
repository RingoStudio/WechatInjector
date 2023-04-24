using RS_WX_INJECTOR.utils;
using System;
using System.Threading.Tasks;

namespace RS_WX_INJECTOR
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                foreach (var arg in args)
                {
                    switch (arg.ToLower())
                    {
                        case "debug":
                            Settings.DEBUG_MODE = true;
                            break;
                        case "release":
                            Settings.DEBUG_MODE = false;
                            break;
                    }
                }
            }
            var task = new task.MainTask();
            //task.Start();
            await Task.Delay(-1);
        }
    }
}
