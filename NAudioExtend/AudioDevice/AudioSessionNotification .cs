using NAudio.CoreAudioApi.Interfaces;

namespace NAudioExtend.AudioDevice
{

    public class AudioSessionNotification : IAudioSessionEventsHandler
    {

        public event Action<uint, nint, uint>? ChannelVolumeChanged;
        public event Action<string>? DisplayNameChanged;
        /// <summary>
        /// ref
        /// </summary>
        public event Action<Guid>? GroupingParamChanged;
        public event Action<string>? IconPathChanged;
        public event Action<AudioSessionDisconnectReason>? SessionDisconnected;
        public event Action<AudioSessionState>? StateChanged;
        public event Action<float, bool>? VolumeChanged;



        public void OnChannelVolumeChanged(uint channelCount, nint newVolumes, uint channelIndex)
        {
            ChannelVolumeChanged?.Invoke(channelCount, newVolumes, channelIndex);
        }

        public void OnDisplayNameChanged(string displayName)
        {
            DisplayNameChanged?.Invoke(displayName);
        }

        public void OnGroupingParamChanged(ref Guid groupingId)
        {
            GroupingParamChanged?.Invoke(groupingId);
        }

        public void OnIconPathChanged(string iconPath)
        {
            IconPathChanged?.Invoke(iconPath);
        }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            SessionDisconnected?.Invoke(disconnectReason);
        }

        public void OnStateChanged(AudioSessionState state)
        {
            StateChanged?.Invoke(state);
        }

        public void OnVolumeChanged(float volume, bool isMuted)
        {
            VolumeChanged?.Invoke(volume, isMuted);
        }
    }

}
