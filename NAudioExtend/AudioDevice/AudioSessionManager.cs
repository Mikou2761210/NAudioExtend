using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace NAudioExtend.AudioDevice
{
    public class AudioSessionManager
    {
        public class AudioSession : IDisposable
        {
            readonly AudioSessionControl _audioSessionControl;

            public string SessionIdentifier => _audioSessionControl.GetSessionIdentifier;
            public string SessionInstanceIdentifier => _audioSessionControl.GetSessionInstanceIdentifier;
            public uint ProcessID  => _audioSessionControl.GetProcessID;
            public string DisplayName { get => _audioSessionControl.DisplayName; set => _audioSessionControl.DisplayName = value; }
            public string IconPath { get => _audioSessionControl.IconPath; set => _audioSessionControl.IconPath = value; }
            public float Volume { get => _audioSessionControl.SimpleAudioVolume.Volume; set => _audioSessionControl.SimpleAudioVolume.Volume = value; }
            public bool Mute { get => _audioSessionControl.SimpleAudioVolume.Mute; set => _audioSessionControl.SimpleAudioVolume.Mute = value; }


            public readonly AudioSessionNotification AudioSessionNotification = new();
            public event Action<uint, nint, uint>? ChannelVolumeChanged {add => AudioSessionNotification.ChannelVolumeChanged += value; remove => AudioSessionNotification.ChannelVolumeChanged -= value; }
            public event Action<string>? DisplayNameChanged { add => AudioSessionNotification.DisplayNameChanged += value; remove => AudioSessionNotification.DisplayNameChanged -= value; }
            public event Action<Guid>? GroupingParamChanged { add => AudioSessionNotification.GroupingParamChanged += value; remove => AudioSessionNotification.GroupingParamChanged -= value; }
            public event Action<string>? IconPathChanged { add => AudioSessionNotification.IconPathChanged += value; remove => AudioSessionNotification.IconPathChanged -= value; }
            public event Action<AudioSessionDisconnectReason>? SessionDisconnected { add => AudioSessionNotification.SessionDisconnected += value; remove => AudioSessionNotification.SessionDisconnected -= value; }
            public event Action<AudioSessionState>? StateChanged { add => AudioSessionNotification.StateChanged += value; remove => AudioSessionNotification.StateChanged -= value; }
            public event Action<float, bool>? VolumeChanged { add => AudioSessionNotification.VolumeChanged += value; remove => AudioSessionNotification.VolumeChanged -= value; }

            public AudioSession(AudioSessionControl audioSessionControl)
            {
                _audioSessionControl = audioSessionControl;
                _audioSessionControl.RegisterEventClient(AudioSessionNotification);
                AudioSessionNotification.SessionDisconnected += (_) => Dispose();
            }
            public override int GetHashCode()
            {
                return ProcessID.GetHashCode();
            }

            public override bool Equals(object? obj)
            {
                if (obj is AudioSession other)
                {
                    return this.ProcessID == other.ProcessID;
                }
                return false;
            }

            ~AudioSession() => Dispose();

            bool _disposed = false;
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _audioSessionControl.UnRegisterEventClient(AudioSessionNotification);
                    _audioSessionControl.Dispose();
                }
            }
        }

        readonly Dictionary<uint,AudioSession> _sessions = [];
        public IEnumerable<AudioSession> Sessions => _sessions.Values;
        public IEnumerable<uint> SessionIDs => _sessions.Keys;
        public void AudioSessionControlsReload(MMDevice? device)
        {
            _sessions.Clear();
            try
            {
                if (device == null) device = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia) ?? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            }
            catch (Exception ex) { Debug.WriteLine(ex); device?.Dispose(); device = null; }
            if (device == null) return;

            SessionCollection sessionCollection = device.AudioSessionManager.Sessions;
            for (int i = 0; i < sessionCollection.Count; i++)
            {
                AudioSession newAudioSession = new(sessionCollection[i]);
                uint id = newAudioSession.ProcessID;

                if (!_sessions.ContainsKey(id))
                {
                    _sessions.Add(id, newAudioSession);
                    newAudioSession.SessionDisconnected += (_) => _sessions.Remove(id);
                }
                else
                    newAudioSession.Dispose();
            }
        }

        public bool TryGetSessionForCurrentProcess([MaybeNullWhen(false)] out AudioSession session)
        {
            uint currentProcessID = (uint)Environment.ProcessId;
            return TryGetSession(currentProcessID, out session);
        }
        public bool TryGetSession(uint sessionId, [MaybeNullWhen(false)] out AudioSession session) =>
            _sessions.TryGetValue(sessionId, out session);
    }
}
