using NAudio.Wave;

namespace NAudioExtend.Provider
{
    public interface IExtendProvider : IWaveProvider, IDisposable
    {
        public TimeSpan TotalDuration { get; }

        public TimeSpan CurrentPosition { get; set; }
    }
}
