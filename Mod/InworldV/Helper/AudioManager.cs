using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace InworldV.Helper
{
    internal class AudioManager
    {
        private static readonly AudioManager instance = new AudioManager();
        private WaveInEvent waveSource;
        private WaveOutEvent _player;
        private bool isRecording;
        private AudioManager() { }
        private Action<byte[]> audioCallback;
        private List<RawSourceWaveStream> _streamQueue = new List<RawSourceWaveStream>();

        public static AudioManager Instance
        {
            get
            {
                return instance;
            }
        }


        private WaveOutEvent _soundtrackPlayer;
        private AudioFileReader _currentSoundtrack;
        private Thread _fadeThread;
        private readonly object _volumeLock = new object();

        public void StartSoundtrack(int trackId = 0)
        {
            // Dont crash if something happens here. Soundtrack isn't super important.
            try
            {
                _currentSoundtrack = new AudioFileReader(GetTrackPath(trackId));
                _currentSoundtrack.Volume = 0.4f;
                _soundtrackPlayer = new WaveOutEvent();
                _soundtrackPlayer.Init(_currentSoundtrack);
                _soundtrackPlayer.Play();
            }
            catch { }
        }

        public void StopSoundtrack()
        {
            try
            {
                _fadeThread = new Thread(FadeOutSoundtrack);
                _fadeThread.Start();
            }
            catch { }
        }

        private void FadeOutSoundtrack()
        {
            while (_currentSoundtrack?.Volume > 0)
            {
                lock (_volumeLock)
                {
                    _currentSoundtrack.Volume -= 0.1f;
                    Thread.Sleep(500);
                }
            }
            _soundtrackPlayer?.Stop();
        }

        public void SoundtrackState(bool isPaused)
        {
            if (isPaused)
            {
                _soundtrackPlayer?.Pause();
            }
            else
            {
                _soundtrackPlayer?.Play();
            }
        }

        private string GetTrackPath(int trackId)
        {
            var directory = Directory.GetCurrentDirectory();
            string inworldFolder = System.IO.Path.Combine(directory, "Inworld\\Soundtrack");
            switch (trackId)
            {
                case 0:
                    return Path.Combine(inworldFolder, "WBA Free Track - Avenger.mp3");
                case 1:
                    return Path.Combine(inworldFolder, "alex-productions-epic-cyberpunk.mp3");
                default:
                    return Path.Combine(inworldFolder, "WBA Free Track - Avenger.mp3");
            }
        }

        public void StartRecording(Action<byte[]> callback)
        {
            if (!isRecording)
            {
                audioCallback = callback;
                waveSource = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 1)
                };
                waveSource.DataAvailable += WaveSource_DataAvailable;
                waveSource.StartRecording();
                isRecording = true;
            }
        }

        public void StopRecording()
        {
            if (waveSource != null)
            {
                waveSource.StopRecording();
                waveSource.Dispose();
                waveSource = null;
            }
            isRecording = false;
        }

        WaveChannel32 activeChannel;
        public void TickSoundManager()
        {
            if (_streamQueue != null && _streamQueue.Count > 0)
            {
                var first = _streamQueue[0];

                if (_player == null)
                {
                    _player = new WaveOutEvent();
                    _player.Volume = 1f;
                }

                if (_player.PlaybackState == PlaybackState.Stopped)
                {
                    _streamQueue.RemoveAt(0);
                    activeChannel = new WaveChannel32(first);
                    activeChannel.PadWithZeroes = false;
                    activeChannel.Volume = lastVolume;
                    _player.Init(activeChannel);
                    _player.Play();
                }
            }
        }

        private float lastVolume = 1f;

        public void SetVolume(float volume)
        {
            if (activeChannel != null)
            {
                lastVolume = volume;
                activeChannel.Volume = volume;
            }
        }

        public bool IsTalking()
        {
            if (_player == null) return false;
            if (_player.PlaybackState == PlaybackState.Playing) return true;
            return false;
        }

        public void PushChunk(string chunk)
        {
            if (string.IsNullOrEmpty(chunk)) return;
            if (_streamQueue == null)
            {
                _streamQueue = new List<RawSourceWaveStream>();
            }

            var sampleRate = 44100;
            byte[] decodedBytes = Convert.FromBase64String(chunk);
            var ms = new MemoryStream(decodedBytes);
            var rs = new RawSourceWaveStream(ms, new WaveFormat(sampleRate, 16, 1));
            _streamQueue.Add(rs);
        }

        public void ClearQueue()
        {
            _streamQueue.Clear();
        }

        public void StopEverythingAbruptly()
        {
            if (_player != null)
                _player.Stop();
            _streamQueue.Clear();
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (audioCallback != null)
            {
                audioCallback.Invoke(e.Buffer);
            }
        }
    }
}
