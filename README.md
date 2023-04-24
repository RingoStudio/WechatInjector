## 描述
#### 用于[ComWechatRobotCsharp](https://github.com/RingoStudio/ComWechatRobotCsharp) 的注入器
#### 使用前请按照上述项目进行配置，在运行机器人前，使用以下代码运行本程序，并使用一个Socker Server与本程序进行通信。

```c#
    // new 一个socket server
    var ipc = Utils.IPC.Instance();
    ipc.OnReceivedMessage += OnRecvInjectorMessage;

    // 开启注入进程
    Utils.ProcessHelper.RunProcess("RS_WX_INJECTOR.exe", false, "");
    do
    {
        Thread.Sleep(100);
        // 等待注入
        if (!ipc.IsConnected) continue;
        // 注入成功，启动微信     
        ipc.Send("start");
        break;
    } while (true);

    // 使用ComWechatRobotCsharp
    var vxSDK = RS_WXBOT_COM.sdk.WeChatSDK.Instance;
    vxSDK.OnRecvMessage += OnRecvMessage;
```

#### 使用前请按照上述项目进行配置，在运行机器人前，使用以下代码运行本程序，并使用一个Socker Server与本程序进行通信。