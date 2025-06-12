/*
SpeakerMicWatcherでは：
AudioEndpointVolume.OnVolumeNotificationイベントでシステムレベルのミュート状態を監視
AudioEndpointVolume.Muteプロパティで物理的なミュート状態を取得

MicrophoneCaptureでは：
音声レベルを監視して仮想的なミュート状態（Zoomなどのソフトウェアミュート）を検知
*/

using NAudio.Wave;
using System;

namespace triggerCam.Audio
{
    /// <summary>
    /// マイク入力の取得開始・停止を管理するクラス
    /// </summary>
    internal class MicrophoneCapture
    {
        private WaveInEvent? waveIn;
        private readonly object lockObject = new object();

        // イベントの定義
        public event Action<bool>? OnRecording;
        public event Action<bool>? OnMute;

        // デバイスが入力を受け付けているか否か
        private bool _isRecording = false;
        private bool isRecording
        {
            get { return _isRecording; }
            set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    OnRecording?.Invoke(value);
                    Console.WriteLine($"Recording state changed: {value}");
                }
            }
        }

        // 入力ソースがありやなしや（仮想的なミュート状態）
        private bool _isMute = false;
        private bool isMute
        {
            get { return _isMute; }
            set
            {
                if (_isMute != value)
                {
                    _isMute = value;
                    OnMute?.Invoke(value);
                    Console.WriteLine($"Virtual mute state changed: {value}");
                }
            }
        }

        // 音声レベルの閾値
        private const float SILENCE_THRESHOLD = 0.0001f;
        private DateTime lastAudioDetected = DateTime.MinValue;
        private const int SILENCE_TIMEOUT_MS = 500; // 無音判定までの時間

        public MicrophoneCapture()
        {
            try
            {
                waveIn = new WaveInEvent();
                waveIn.DataAvailable += OnDataAvailable;
                waveIn.RecordingStopped += OnRecordingStopped;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing MicrophoneCapture: {ex.Message}");
            }
        }

        public void Initialize(string? targetDeviceName)
        {
            lock (lockObject)
            {
                try
                {
                    if (waveIn != null && !string.IsNullOrEmpty(targetDeviceName))
                    {
                        if (isRecording) StopCapturing();

                        waveIn.DeviceNumber = getDeviceNumber(targetDeviceName);
                        waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, モノラル
                        Console.WriteLine($"Initialized device: {targetDeviceName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing device: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                try
                {
                    if (waveIn != null)
                    {
                        waveIn.DataAvailable -= OnDataAvailable;
                        waveIn.RecordingStopped -= OnRecordingStopped;
                        waveIn.StopRecording();
                        waveIn.Dispose();
                        waveIn = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing MicrophoneCapture: {ex.Message}");
                }
            }
        }

        private int getDeviceNumber(string targetDeviceName)
        {
            try
            {
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var capabilities = WaveIn.GetCapabilities(i);
                    if (targetDeviceName.Contains(capabilities.ProductName))
                    {
                        Console.WriteLine($"Found device number: {i}");
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting device number: {ex.Message}");
            }

            Console.WriteLine("Device not found, using default (0)");
            return 0;
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            lock (lockObject)
            {
                if (isRecording)
                {
                    isRecording = false;
                }
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (lockObject)
            {
                try
                {
                    if (!isRecording)
                    {
                        isRecording = true;
                    }

                    // 音声レベルを分析
                    float maxLevel = 0;
                    var buffer = new WaveBuffer(e.Buffer);
                    
                    // 32ビット浮動小数点オーディオとして解釈
                    for (int index = 0; index < e.BytesRecorded / 4; index++)
                    {
                        var sample = buffer.FloatBuffer[index];
                        // 絶対値を取得
                        if (sample < 0) sample = -sample;
                        // 最大値を更新
                        if (sample > maxLevel) maxLevel = sample;
                    }

                    // 音声レベルに基づいて仮想的なミュート状態を更新
                    if (maxLevel > SILENCE_THRESHOLD)
                    {
                        lastAudioDetected = DateTime.Now;
                        if (isMute)
                        {
                            isMute = false;
                        }
                    }
                    else if (!isMute && (DateTime.Now - lastAudioDetected).TotalMilliseconds > SILENCE_TIMEOUT_MS)
                    {
                        isMute = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing audio data: {ex.Message}");
                }
            }
        }

        private void StartCapturing()
        {
            lock (lockObject)
            {
                try
                {
                    if (!isRecording && waveIn != null)
                    {
                        waveIn.StartRecording();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting capture: {ex.Message}");
                }
            }
        }

        private void StopCapturing()
        {
            lock (lockObject)
            {
                try
                {
                    if (isRecording && waveIn != null)
                    {
                        waveIn.StopRecording();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping capture: {ex.Message}");
                }
            }
        }

        public void SwitchCapturing(bool shouldStop)
        {
            lock (lockObject)
            {
                if (shouldStop && isRecording)
                {
                    StopCapturing();
                }
                else if (!shouldStop && !isRecording)
                {
                    StartCapturing();
                }
            }
        }
    }
}
