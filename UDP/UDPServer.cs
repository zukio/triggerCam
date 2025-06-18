using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public class UDPServer : IDisposable // IDisposable インターフェースを実装
{
    UdpClient udp = new UdpClient();

    public string UDP_LocalAddress = "127.0.0.1";
    public int UDP_LocalPort = 10000;

    private ConcurrentQueue<UDP_DATA> q_UdpData
    {
        get
        {
            return UDPServer_Static.q_UdpData;
        }
    }

    public UDPServer(string ip = "127.0.0.1", int port = 10000)
    {
        UDP_LocalAddress = ip;
        UDP_LocalPort = port;

        Init();
    }

    private void Init()
    {
        udp = new UdpClient(UDP_LocalPort);

        // 非同期受信を開始
        udp.BeginReceive(ReceiveCallback, udp);

        global::LogWriter.AddLog($"UDP server started on {UDP_LocalAddress}:{UDP_LocalPort}");
    }

    public void Dispose()
    {
        // UdpClient インスタンスを解放
        udp?.Dispose();
        global::LogWriter.AddLog($"UDP server closed on port {UDP_LocalPort}");
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // データの受信
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            if (udp == null)
                return;

            // バイト型のデータを取得
            Byte[] rcvBytes = udp.EndReceive(ar, ref remoteEP);
            // 構造体の作成
            UDP_DATA data = new UDP_DATA
            {
                ip = remoteEP.Address.ToString(),
                port = remoteEP.Port,
                rcvBytes = rcvBytes
            };

            // キューに入れ込む
            q_UdpData.Enqueue(data);

            // UDP 受信を再開
            udp.BeginReceive(ReceiveCallback, udp);

            string log = $"UDP Received: IP {data.ip} Port {data.port} >> {data.rcvString}";
            Console.WriteLine(log);
            global::LogWriter.AddLog(log);
        }
        catch (Exception error)
        {
            Console.WriteLine(error.Message, "Receive UDP Data");
            global::LogWriter.AddLog($"UDP receive error: {error.Message}");
            global::LogWriter.AddErrorLog(error, nameof(ReceiveCallback));
        }
    }
}


public static class UDPServer_Static
{
    private static ConcurrentQueue<UDP_DATA> _q_UdpData;
    public static ConcurrentQueue<UDP_DATA> q_UdpData
    {
        get
        {
            if (_q_UdpData == null)
                _q_UdpData = new ConcurrentQueue<UDP_DATA>();
            return _q_UdpData;
        }
    }
}



public class UDP_DATA
{
    public string ip = "127.0.0.1";
    public int port = 10000;
    public byte[]? rcvBytes;

    public string rcvString
    {
        get
        {
            return System.Text.Encoding.UTF8.GetString(rcvBytes);
        }
        set
        {
            rcvBytes = System.Text.Encoding.UTF8.GetBytes(value);
        }
    }
}