using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

public static class UDPSender
{
    /// <summary>
    /// 毎回 UDP Client を作成して送信を行うパターン
    /// </summary>
    /// <param name="data">送信する文字列</param>
    /// <param name="ip">送信先 IP</param>
    /// <param name="port">送信先のポート番号</param>
    public static void SendUDP(string data, string ip = "127.0.0.1", int port = 10000)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            // バイト型に変換
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            // UDP を非同期送信
            udpClient.SendAsync(bytes, bytes.Length, ip, port);

            string log = $"Send UDP to: {ip}:{port} >> {data}";
            Console.WriteLine(log);
            global::LogWriter.AddLog(log);
        }
    }

    /// <summary>
    /// 作成した UDP Client を使用して送信するパターン
    /// </summary>
    /// <param name="udpClient"></param>
    /// <param name="data">送信する文字列</param>
    /// <param name="ip">送信先 IP</param>
    /// <param name="port">送信先のポート番号</param>
    public static void SendUDP(UdpClient udpClient, string data, string ip = "127.0.0.1", int port = 10000)
    {
        // バイト型に変換
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        // UDP を非同期送信
        udpClient.SendAsync(bytes, bytes.Length, ip, port);

        string log = $"Send UDP to: {ip}:{port} >> {data}";
        Console.WriteLine(log);
        global::LogWriter.AddLog(log);
    }
}