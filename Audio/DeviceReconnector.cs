using NAudio.CoreAudioApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace micNotifyUDP.Audio
{
    /// <summary>
    /// マイクデバイスの再接続を管理するクラス
    /// </summary>
    public class DeviceReconnector
    {
        private readonly string targetDeviceName;
        private readonly Action<MMDevice?> onDeviceReconnected;
        private readonly int reconnectInterval;
        private CancellationTokenSource? cancellationTokenSource;
        private Task? reconnectTask;
        private readonly object lockObject = new object();
        private bool isReconnecting = false;

        public DeviceReconnector(string deviceName, Action<MMDevice?> onReconnected, int reconnectIntervalMs = 5000)
        {
            targetDeviceName = deviceName;
            onDeviceReconnected = onReconnected;
            reconnectInterval = reconnectIntervalMs;
        }

        /// <summary>
        /// 再接続処理を開始
        /// </summary>
        public void StartReconnection()
        {
            lock (lockObject)
            {
                if (isReconnecting)
                {
                    return;
                }

                isReconnecting = true;
                cancellationTokenSource = new CancellationTokenSource();
                reconnectTask = Task.Run(ReconnectLoop, cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// 再接続処理を停止
        /// </summary>
        public void StopReconnection()
        {
            lock (lockObject)
            {
                if (!isReconnecting)
                {
                    return;
                }

                cancellationTokenSource?.Cancel();
                reconnectTask?.Wait();
                isReconnecting = false;
            }
        }

        /// <summary>
        /// 再接続ループ処理
        /// </summary>
        private async Task ReconnectLoop()
        {
            while (!cancellationTokenSource?.Token.IsCancellationRequested ?? false)
            {
                try
                {
                    var device = FindDevice();
                    if (device != null)
                    {
                        Console.WriteLine($"Device reconnected: {device.FriendlyName}");
                        onDeviceReconnected(device);
                        break;
                    }

                    await Task.Delay(reconnectInterval, cancellationTokenSource?.Token ?? CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in reconnection loop: {ex.Message}");
                    await Task.Delay(reconnectInterval, cancellationTokenSource?.Token ?? CancellationToken.None);
                }
            }

            lock (lockObject)
            {
                isReconnecting = false;
            }
        }

        /// <summary>
        /// デバイスを検索
        /// </summary>
        private MMDevice? FindDevice()
        {
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    // すべてのオーディオエンドポイントデバイスを列挙
                    var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    foreach (var device in devices)
                    {
                        if (device.FriendlyName.Contains(targetDeviceName))
                        {
                            return device;
                        }
                        device.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding device: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            StopReconnection();
            cancellationTokenSource?.Dispose();
        }
    }
}
