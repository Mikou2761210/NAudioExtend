using NAudio.CoreAudioApi;

namespace NAudioExtend.AudioDevice
{
    namespace NAudioExtend.AudioDevice
    {
        /// <summary>
        /// Enumeration for device selection mode.
        /// Auto: Automatically follow the system default.
        /// Manual: User-controlled device selection.
        /// </summary>
        public enum AudioDeviceSelectorMode
        {
            Auto,
            Manual
        }

        /// <summary>
        /// Manages the selection of an audio device.
        /// In Auto mode, the selector follows default device changes.
        /// In Manual mode, the user can change the selection.
        /// </summary>
        public class AudioDeviceSelector : IDisposable
        {
            private readonly object _syncRoot = new();
            private readonly AudioDeviceManager _audioDeviceManager;
            private AudioDeviceSelectorMode _selectorMode = AudioDeviceSelectorMode.Manual;
            private MMDevice? _selectedDevice = null;
            private readonly DataFlow _dataFlow;
            private readonly Role _role;
            private bool _disposed = false;
            private readonly SynchronizationContext? syncContext;

            /// <summary>
            /// Event raised when the selected device changes.
            /// </summary>
            public event Action<MMDevice?>? SelectedDeviceChanged;

            /// <summary>
            /// Gets the currently selected audio device.
            /// </summary>
            public MMDevice? SelectedDevice
            {
                get
                {
                    ThrowIfDisposed();
                    lock (_syncRoot)
                    {
                        return _selectedDevice;
                    }
                }
            }

            /// <summary>
            /// Gets the DataFlow used by this selector.
            /// </summary>
            public DataFlow DataFlow
            {
                get
                {
                    ThrowIfDisposed();
                    lock (_syncRoot)
                    {
                        return _dataFlow;
                    }
                }
            }

            /// <summary>
            /// Gets the Role used by this selector.
            /// </summary>
            public Role Role
            {
                get
                {
                    ThrowIfDisposed();
                    lock (_syncRoot)
                    {
                        return _role;
                    }
                }
            }

            /// <summary>
            /// Gets or sets the selector mode.
            /// Changing the mode to Auto will update the selection to the default device.
            /// </summary>
            public AudioDeviceSelectorMode SelectorMode
            {
                get
                {
                    ThrowIfDisposed();
                    lock (_syncRoot)
                    {
                        return _selectorMode;
                    }
                }
                set
                {
                    ThrowIfDisposed();
                    bool changed = false;
                    lock (_syncRoot)
                    {
                        if (_selectorMode != value)
                        {
                            _selectorMode = value;
                            if (_selectorMode == AudioDeviceSelectorMode.Auto)
                            {
                                if (_audioDeviceManager.TryGetDefaultDevice(_dataFlow, _role, out var defaultDevice))
                                {
                                    changed = SelectDeviceInternal(defaultDevice.ID);
                                }
                            }
                        }
                    }
                    if (changed)
                    {
                        SelectedDeviceChanged?.Invoke(_selectedDevice);
                    }
                }
            }

            /// <summary>
            /// Initializes a new instance of the AudioDeviceSelector.
            /// </summary>
            /// <param name="selectorMode">The initial selector mode.</param>
            /// <param name="dataFlow">The data flow (Render or Capture) to filter devices.</param>
            /// <param name="role">The role (e.g. Multimedia) for device selection.</param>
            /// <param name="_syncContext">The synchronization context to use for event dispatching.</param>
            public AudioDeviceSelector(AudioDeviceSelectorMode selectorMode, DataFlow dataFlow, Role role, SynchronizationContext? _syncContext = null)
            {
                _audioDeviceManager = new AudioDeviceManager(dataFlow);
                _dataFlow = dataFlow;
                _role = role;

                syncContext = _syncContext ?? SynchronizationContext.Current;
                // Subscribe to device notification events.
                _audioDeviceManager.NotificationClient.DefaultDeviceChanged += OnDefaultDeviceChanged;
                _audioDeviceManager.NotificationClient.DeviceStateChanged += OnDeviceStateChanged;

                SelectorMode = selectorMode;
            }

            /// <summary>
            /// Handles the DefaultDeviceChanged event.
            /// Updates the selected device if the mode is Auto.
            /// </summary>
            private void OnDefaultDeviceChanged(DataFlow dataFlow, Role role, string deviceId)
            {
                bool changed = false;
                lock (_syncRoot)
                {
                    if (_selectorMode == AudioDeviceSelectorMode.Auto && dataFlow == _dataFlow && role == _role)
                    {
                        changed = SelectDeviceInternal(deviceId);
                    }
                }
                if (changed)
                {
                    SelectedDeviceChanged?.Invoke(_selectedDevice);
                }
            }

            /// <summary>
            /// Handles the DeviceStateChanged event.
            /// If the current device is no longer active, clears the selection.
            /// </summary>
            private void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                bool changed = false;
                lock (_syncRoot)
                {
                    if (_selectedDevice?.ID == deviceId && newState != DeviceState.Active)
                    {
                        changed = SelectDeviceInternal(null);
                    }
                }
                if (changed)
                {
                    SelectedDeviceChanged?.Invoke(_selectedDevice);
                }
            }

            /// <summary>
            /// Internally attempts to update the selected device.
            /// If deviceId is not null and valid, sets it as selected.
            /// Otherwise, clears the selection.
            /// Returns true if the selection changed.
            /// </summary>
            private bool SelectDeviceInternal(string? deviceId)
            {
                if(syncContext != null)
                {
                    bool result = false;
                    syncContext.Send(_ =>
                    {
                        result = SelectDeviceCore();
                    }, null);
                    return result;
                }
                else
                {
                    return SelectDeviceCore();
                }

                bool SelectDeviceCore()
                {
                    bool changed = false;
                    if (deviceId != null && _audioDeviceManager.TryGetDevice(deviceId, out var device))
                    {
                        if (_selectedDevice?.ID != device.ID)
                        {
                            _selectedDevice = device;
                            changed = true;
                        }
                    }
                    else
                    {
                        if (_selectedDevice != null)
                        {
                            _selectedDevice = null;
                            changed = true;
                        }
                    }

                    return changed;
                }
            }

            /// <summary>
            /// Attempts to change the selected device manually.
            /// Only allowed in Manual mode.
            /// </summary>
            /// <param name="deviceId">The device ID to select, or null to clear selection.</param>
            /// <returns>True if the selection changed; otherwise, false.</returns>
            public bool TryChangeDevice(string? deviceId)
            {
                ThrowIfDisposed();
                if (_selectorMode == AudioDeviceSelectorMode.Auto)
                    return false; // In Auto mode manual changes are ignored.

                bool changed;
                lock (_syncRoot)
                {
                    changed = SelectDeviceInternal(deviceId);
                }
                if (changed)
                {
                    SelectedDeviceChanged?.Invoke(_selectedDevice);
                }
                return changed;
            }

            /// <summary>
            /// Throws an ObjectDisposedException if this instance has been disposed.
            /// </summary>
            private void ThrowIfDisposed()
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().Name);
            }

            /// <summary>
            /// Disposes the AudioDeviceSelector and unsubscribes from notifications.
            /// </summary>
            public void Dispose()
            {
                lock (_syncRoot)
                {
                    if (!_disposed)
                    {
                        _audioDeviceManager.NotificationClient.DefaultDeviceChanged -= OnDefaultDeviceChanged;
                        _audioDeviceManager.NotificationClient.DeviceStateChanged -= OnDeviceStateChanged;
                        _audioDeviceManager.Dispose();
                        _disposed = true;
                    }
                }
            }
        }
    }

}
