using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;
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
        readonly HashSet<AudioSession> _sessions = [];
        public IEnumerable<AudioSession> Sessions => _sessions;
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
                //If addition fails, Dispose
                if (!_sessions.Add(newAudioSession))
                    newAudioSession.Dispose();
                else
                    newAudioSession.SessionDisconnected += (_) => _sessions.Remove(newAudioSession);
            }
        }
    }
}
