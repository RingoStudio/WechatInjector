## ����
#### ����[ComWechatRobotCsharp](https://github.com/RingoStudio/ComWechatRobotCsharp) ��ע����
#### ʹ��ǰ�밴��������Ŀ�������ã������л�����ǰ��ʹ�����´������б����򣬲�ʹ��һ��Socker Server�뱾�������ͨ�š�

```c#
    // new һ��socket server
    var ipc = Utils.IPC.Instance();
    ipc.OnReceivedMessage += OnRecvInjectorMessage;

    // ����ע�����
    Utils.ProcessHelper.RunProcess("RS_WX_INJECTOR.exe", false, "");
    do
    {
        Thread.Sleep(100);
        // �ȴ�ע��
        if (!ipc.IsConnected) continue;
        // ע��ɹ�������΢��     
        ipc.Send("start");
        break;
    } while (true);

    // ʹ��ComWechatRobotCsharp
    var vxSDK = RS_WXBOT_COM.sdk.WeChatSDK.Instance;
    vxSDK.OnRecvMessage += OnRecvMessage;
```

#### ʹ��ǰ�밴��������Ŀ�������ã������л�����ǰ��ʹ�����´������б����򣬲�ʹ��һ��Socker Server�뱾�������ͨ�š�