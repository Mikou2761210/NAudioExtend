using NAudio.Wave;
namespace NAudioExtend.Provider
{
    /// <summary>
    /// Provides a switchable stream provider that delegates to an underlying IExtendProvider.
    /// Allows changing the current provider dynamically.
    /// </summary>
    public class StreamSwitchProvider : IExtendProvider, IDisposable
    {
        private readonly object _providerLock = new();
        private IExtendProvider? _currentProvider;

        /// <summary>
        /// Gets the current provider.
        /// </summary>
        public IExtendProvider? CurrentProvider
        {
            get
            {
                lock (_providerLock)
                {
                    return _currentProvider;
                }
            }
        }

        /// <summary>
        /// Gets the total duration from the current provider.
        /// </summary>
        public TimeSpan TotalDuration => _currentProvider?.TotalDuration ?? TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the current playback position.
        /// </summary>
        public TimeSpan CurrentPosition
        {
            get => _currentProvider?.CurrentPosition ?? TimeSpan.Zero;
            set
            {
                lock (_providerLock)
                {
                    if (_currentProvider != null)
                        _currentProvider.CurrentPosition = value;
                }
            }
        }

        /// <summary>
        /// Specifies the WaveFormat to use when the CurrentProvider is null. This ensures that StreamSwitchProvider has a defined format before a provider is set.
        /// </summary>
        public WaveFormat DefaultWaveFormat;

        /// <summary>
        /// Gets the WaveFormat from the current provider.
        /// </summary>
        public WaveFormat WaveFormat => _currentProvider?.WaveFormat ?? DefaultWaveFormat;

        /// <summary>
        /// Event raised when playback ends.
        /// </summary>
        public event Action? PlaybackEnded;

        private bool _hasPlaybackEnded = false;

        /// <summary>
        /// Initializes a new instance of StreamSwitchProvider with an optional initial provider.
        /// </summary>
        /// <param name="initialProvider">The initial provider to use.</param>
        /// <param name="defaultWaveFormat">Specifies the WaveFormat to use when the CurrentProvider is null. This ensures that StreamSwitchProvider has a defined format before a provider is set.</param>
        public StreamSwitchProvider(IExtendProvider? initialProvider, WaveFormat defaultWaveFormat)
        {
            DefaultWaveFormat = defaultWaveFormat;
            ChangeProvider(initialProvider);
        }

        /// <summary>
        /// Reads audio data from the current provider.
        /// </summary>
        public int Read(byte[] outputBuffer, int offset, int count)
        {
            if (_disposed)
                return 0;

            lock (_providerLock)
            {
                if (_currentProvider == null)
                {
                    Array.Clear(outputBuffer, offset, count);
                    return count;
                }
                count = _currentProvider.Read(outputBuffer, offset, count);
            }

            if (count <= 0)
            {
                if (!_hasPlaybackEnded)
                {
                    _hasPlaybackEnded = true;
                    PlaybackEnded?.Invoke();
                }

                Array.Clear(outputBuffer, offset, count);
                return count;
            }
            else
            {
                _hasPlaybackEnded = false;
            }

            return count;
        }

        /// <summary>
        /// Changes the underlying provider and disposes the old provider.
        /// </summary>
        public void ChangeProvider(IExtendProvider? extendProvider)
        {
            IExtendProvider? oldProvider;
            lock (_providerLock)
            {
                oldProvider = _currentProvider;
                _currentProvider = extendProvider;
            }
            oldProvider?.Dispose();
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                lock (_providerLock)
                {
                    _currentProvider?.Dispose();
                    _currentProvider = null;
                }
            }
        }
    }
}

