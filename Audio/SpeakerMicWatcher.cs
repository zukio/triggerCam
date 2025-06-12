﻿/*
SpeakerMicWatcherでは：
AudioEndpointVolume.OnVolumeNotificationイベントでシステムレベルのミュート状態を監視
AudioEndpointVolume.Muteプロパティで物理的なミュート状態を取得

MicrophoneCaptureでは：
音声レベルを監視して仮想的なミュート状態（Zoomなどのソフトウェアミュート）を検知
*/
using NAudio.CoreAudioApi;
using System.Text.RegularExpressions;

namespace micNotifyUDP.Audio
{
    internal class WatcherManager
    {
        public SpeakerMicWatcher? micWatcher;
        MicrophoneCapture? microphoneCapture;
        private System.Timers.Timer? timer;

        public WatcherManager(string? deviceName)
        {
            micWatcher = new SpeakerMicWatcher(DataFlow.Capture, deviceName);
            microphoneCapture = new MicrophoneCapture();
            micWatcher.connectHook += microphoneCapture.Initialize;
        }

        public void Initialize()
        {
            if (micWatcher == null) return;
            timer = new System.Timers.Timer(500);
            timer.Elapsed += (sender, e) =>
            {
                micWatcher.CheckDeviceState();
            };
            timer.Start();
        }

        public void Dispose()
        {
            timer?.Stop();
            timer?.Dispose();
            timer = null;

            if (micWatcher != null)
            {
                if (microphoneCapture != null)
                {
                    micWatcher.connectHook -= microphoneCapture.Initialize;
                }
                micWatcher?.Dispose();
                micWatcher = null;
            }
            if (microphoneCapture != null)
            {
                microphoneCapture.Dispose();
                microphoneCapture = null;
            }
        }
    }

    internal class SpeakerMicWatcher
    {
        public string targetDeviceName { get; private set; } = "";
        public string? detectedDeviceName { get; private set; } = null;
        private readonly DataFlow dataFlow;
        private readonly MMDeviceEnumerator? enumerator = new MMDeviceEnumerator();
        private readonly Dictionary<string, string> detected_devices = new Dictionary<string, string>();
        public MMDevice? monitorDevice = null;
        private readonly object stateLock = new object();

        public event Action<string?>? connectHook;
        public event Action<bool>? muteHook;
        public event Action<string[]>? scanHook;

        private bool isActive = false;
        private bool? lastMuteState = null;
        private DateTime lastStateChange = DateTime.MinValue;
        private const int StateChangeThrottleMs = 100; // 状態変更の最小間隔

        public SpeakerMicWatcher(DataFlow EDataFlow, string? deviceName)
        {
            dataFlow = EDataFlow;
            if (!string.IsNullOrEmpty(deviceName))
            {
                targetDeviceName = deviceName;
            }
            else
            {
                try
                {
                    MMDevice? systemDevice = enumerator?.GetDefaultAudioEndpoint(EDataFlow, Role.Console);
                    if (systemDevice?.FriendlyName != null)
                    {
                        targetDeviceName = getName(systemDevice.FriendlyName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting default device: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            lock (stateLock)
            {
                enumerator?.Dispose();
                resetDetection();
                connectHook = null;
                muteHook = null;
                scanHook = null;
            }
        }

        public void reStart(string? deviceName)
        {
            lock (stateLock)
            {
                resetDetection();
                targetDeviceName = deviceName ?? "";
                Console.WriteLine($"Restart with device: {targetDeviceName}");
            }
        }

        private string deviceType(int index)
        {
            return dataFlow switch
            {
                DataFlow.Capture => new string[] { "Mic", "Input" }[index],
                DataFlow.Render => new string[] { "Speaker", "Output" }[index],
                _ => new string[] { dataFlow.ToString(), "Unknown" }[index]
            };
        }

        private string getName(string input)
        {
            string pattern = @"(.+?)\s?\(";
            Match match = Regex.Match(input, pattern);
            return match.Success ? match.Groups[1].Value : input;
        }

        private void resetDetection()
        {
            lock (stateLock)
            {
                if (monitorDevice != null)
                {
                    Console.WriteLine($"Removing {deviceType(0)}");
                    try
                    {
                        monitorDevice.AudioEndpointVolume.OnVolumeNotification -= EndpointVolume_OnNotify;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error removing volume notification: {ex.Message}");
                    }
                    monitorDevice = null;
                    connectHook?.Invoke(null);
                }
                detectedDeviceName = null;
                detected_devices.Clear();
                lastMuteState = null;
                isActive = false;
            }
        }

        private void setTarget(MMDevice device)
        {
            lock (stateLock)
            {
                try
                {
                    if (device?.FriendlyName == null) return;

                    detectedDeviceName = device.FriendlyName;
                    if (string.IsNullOrEmpty(targetDeviceName))
                    {
                        targetDeviceName = getName(device.FriendlyName);
                    }

                    if (detectedDeviceName.Contains(targetDeviceName))
                    {
                        Console.WriteLine($"Setting {deviceType(0)}: {device.FriendlyName}");
                        monitorDevice = device;
                        monitorDevice.AudioEndpointVolume.OnVolumeNotification += EndpointVolume_OnNotify;
                        connectHook?.Invoke(detectedDeviceName);

                        UpdateMuteState(device.AudioEndpointVolume.Mute);
                    }
                    else
                    {
                        resetDetection();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting target: {ex.Message}");
                    resetDetection();
                }
            }
        }

        private void UpdateMuteState(bool newMuteState)
        {
            lock (stateLock)
            {
                if (lastMuteState != newMuteState && 
                    (DateTime.Now - lastStateChange).TotalMilliseconds > StateChangeThrottleMs)
                {
                    Console.WriteLine($"{deviceType(0)} Mute state changed: {lastMuteState} -> {newMuteState}");
                    lastMuteState = newMuteState;
                    isActive = !newMuteState;
                    lastStateChange = DateTime.Now;
                    muteHook?.Invoke(newMuteState);
                }
            }
        }

        private void EndpointVolume_OnNotify(AudioVolumeNotificationData data)
        {
            UpdateMuteState(data.Muted);
        }

        private List<MMDevice> filter_devices(DataFlow EDataFlow, string[]? filter_keyword)
        {
            var filtered_devices = new List<MMDevice>();
            try
            {
                if (enumerator == null) return filtered_devices;

                var devices = enumerator.EnumerateAudioEndPoints(EDataFlow, DeviceState.Active);
                var current_devices = new Dictionary<string, string>();

                foreach (var device in devices)
                {
                    try
                    {
                        if (device?.FriendlyName != null)
                        {
                            if (filter_keyword == null || filter_keyword.Length == 0 || 
                                filter_keyword.Any(keyword => device.FriendlyName.Contains(keyword)))
                            {
                                filtered_devices.Add(device);
                            }
                            current_devices.Add(device.ID, device.FriendlyName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing device: {ex.Message}");
                    }
                }

                updateDevicesList(current_devices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering devices: {ex.Message}");
            }
            return filtered_devices;
        }

        private void updateDevicesList(Dictionary<string, string> current_devices)
        {
            lock (stateLock)
            {
                bool changedFlg = false;

                foreach (var pair in current_devices)
                {
                    if (!detected_devices.ContainsKey(pair.Key))
                    {
                        changedFlg = true;
                        break;
                    }
                }

                foreach (var pair in detected_devices)
                {
                    if (!current_devices.ContainsKey(pair.Key))
                    {
                        if (pair.Value.Contains(targetDeviceName))
                        {
                            current_devices.Add(pair.Key, pair.Value);
                        }
                        else
                        {
                            changedFlg = true;
                        }
                    }
                }

                if (changedFlg)
                {
                    detected_devices.Clear();
                    foreach (var pair in current_devices)
                    {
                        detected_devices.Add(pair.Key, pair.Value);
                    }
                    scanHook?.Invoke(detected_devices.Values.ToArray());
                }
            }
        }

        private MMDevice? getTheDevice(DataFlow EDataFlow, string[]? filter_keyword)
        {
            try
            {
                var input_devices = filter_devices(EDataFlow, filter_keyword);
                return input_devices.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting device: {ex.Message}");
                return null;
            }
        }

        public void CheckDeviceState()
        {
            try
            {
                var device = getTheDevice(dataFlow, new string[] { targetDeviceName });
                
                lock (stateLock)
                {
                    if (monitorDevice != null && device == null)
                    {
                        resetDetection();
                    }
                    else if (monitorDevice == null && device != null)
                    {
                        setTarget(device);
                    }
                    else if (device != null)
                    {
                        CheckTargetDeviceState(device);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking device state: {ex.Message}");
            }
        }

        private void CheckTargetDeviceState(MMDevice device)
        {
            try
            {
                if (device.State != DeviceState.Active)
                {
                    Console.WriteLine($"{deviceType(0)} is inactive: {device.FriendlyName}");
                    return;
                }

                // ミュート状態のみを監視
                bool isMuted = device.AudioEndpointVolume.Mute;
                UpdateMuteState(isMuted);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking target device state: {ex.Message}");
            }
        }
    }
}
