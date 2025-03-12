using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NAudioExtend
{
    /// <summary>
    /// Wraps a WasapiOut instance that can be reconfigured at runtime.
    /// Allows changing the output device and audio source.
    /// </summary>
    public class SwitchableWasapi : IDisposable
    {
        private readonly object _lock = new();
        private WasapiOut? _wasapiOut = null;
        private IWaveProvider? _currentProvider = null;
        private MMDevice? _currentDevice = null;

        /// <summary>
        /// Gets or sets the current audio device.
        /// </summary>
        public MMDevice? CurrentDevice
        {
            get => _currentDevice;
            set
            {
                lock (_lock)
                {
                    _currentDevice = value;
                    ResetWasapi();
                }
            }
        }

        // Configuration parameters.
        private AudioClientShareMode _shareMode;
        private bool _useEventSync;
        private int _latency;

        public AudioClientShareMode ShareMode
        {
            get => _shareMode;
            set
            {
                lock (_lock)
                {
                    _shareMode = value;
                    ResetWasapi();
                }
            }
        }

        public bool UseEventSync
        {
            get => _useEventSync;
            set
            {
                lock (_lock)
                {
                    _useEventSync = value;
                    ResetWasapi();
                }
            }
        }

        public int Latency
        {
            get => _latency;
            set
            {
                lock (_lock)
                {
                    _latency = value;
                    ResetWasapi();
                }
            }
        }

        /// <summary>
        /// Event raised when playback stops.
        /// </summary>
        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        /// <summary>
        /// Initializes a new instance of SwitchableWasapi with the specified configuration.
        /// </summary>
        public SwitchableWasapi(MMDevice? mMDevice, AudioClientShareMode shareMode, bool useEventSync, int latency)
        {
            lock (_lock)
            {
                _currentDevice = mMDevice;
                _shareMode = shareMode;
                _useEventSync = useEventSync;
                _latency = latency;
            }
        }

        /// <summary>
        /// Sets the audio source and initializes the WasapiOut instance.
        /// </summary>
        public bool SetAudioSource(IWaveProvider? waveProvider)
        {
            lock (_lock)
            {
                _currentProvider = waveProvider;
                return ResetWasapi();
            }
        }

        /// <summary>
        /// Reinitializes the WasapiOut instance.
        /// </summary>
        private bool ResetWasapi()
        {
            PlaybackState? oldState = _wasapiOut?.PlaybackState;
            _wasapiOut?.Dispose();
            if (_currentProvider != null && _currentDevice?.State == DeviceState.Active)
            {
                try
                {
                    _wasapiOut = new WasapiOut(_currentDevice, _shareMode, _useEventSync, _latency);
                    _wasapiOut.Init(_currentProvider);
                    _wasapiOut.PlaybackStopped += OnWasapiPlaybackStopped;

                    // Restore previous playback state.
                    if (oldState == PlaybackState.Playing)
                        _wasapiOut.Play();
                    else
                        _wasapiOut.Pause();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    _wasapiOut?.Dispose();
                    _wasapiOut = null;
                }
            }
            else
            {
                _wasapiOut = null;
            }
            return false;
        }

        /// <summary>
        /// Reloads the WasapiOut instance.
        /// </summary>
        public bool WasapiReload()
        {
            lock (_lock)
            {
                return ResetWasapi();
            }
        }

        /// <summary>
        /// Forwards the WasapiOut PlaybackStopped event.
        /// </summary>
        private void OnWasapiPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, e);
        }

        /// <summary>
        /// Updates the configuration and rebuilds the WasapiOut instance.
        /// </summary>
        public bool UpdateConfiguration(MMDevice? mMDevice, AudioClientShareMode shareMode, bool useEventSync, int latency)
        {
            lock (_lock)
            {
                _currentDevice = mMDevice;
                _shareMode = shareMode;
                _useEventSync = useEventSync;
                _latency = latency;

                return ResetWasapi();
            }
        }

        /// <summary>
        /// Gets the current playback state.
        /// </summary>
        public PlaybackState PlaybackState
        {
            get
            {
                lock (_lock)
                {
                    return _wasapiOut?.PlaybackState ?? PlaybackState.Stopped;
                }
            }
        }

        /// <summary>
        /// Starts playback.
        /// </summary>
        public void Play()
        {
            lock (_lock)
            {
                _wasapiOut?.Play();
            }
        }

        /// <summary>
        /// Pauses playback.
        /// </summary>
        public void Pause()
        {
            lock (_lock)
            {
                _wasapiOut?.Pause();
            }
        }

        /// <summary>
        /// Stops playback and disposes the WasapiOut instance.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _wasapiOut?.Stop();
                _wasapiOut?.Dispose();
                _wasapiOut = null;
            }
        }

        /// <summary>
        /// Gets the current playback position in bytes.
        /// </summary>
        public long GetPosition()
        {
            lock (_lock)
            {
                return _wasapiOut?.GetPosition() ?? 0;
            }
        }

        /// <summary>
        /// Gets the output WaveFormat.
        /// </summary>
        public WaveFormat? OutputWaveFormat
        {
            get
            {
                lock (_lock)
                {
                    return _wasapiOut?.OutputWaveFormat;
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume (0.0 to 1.0).
        /// </summary>
        public float Volume
        {
            get
            {
                lock (_lock)
                {
                    return _wasapiOut?.Volume ?? 0;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_wasapiOut != null)
                        _wasapiOut.Volume = value;
                }
            }
        }

        /// <summary>
        /// Disposes the SwitchableWasapi instance and releases resources.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _wasapiOut?.Stop();
                _wasapiOut?.Dispose();
                _wasapiOut = null;
                _currentDevice = null;
                _currentProvider = null;
            }
        }
    }
}

