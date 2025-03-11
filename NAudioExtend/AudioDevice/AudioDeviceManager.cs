using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAudioExtend.AudioDevice
{
    /// <summary>
    /// Manages audio devices using NAudio's MMDeviceEnumerator.
    /// Provides methods to retrieve devices and subscribes to notifications.
    /// </summary>
    public class AudioDeviceManager : IDisposable
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, MMDevice> _devices = new();
        private readonly MMDeviceEnumerator _deviceEnumerator = new();
        private readonly AudioDeviceNotification _notificationClient = new();

        private DataFlow? _dataFlow = null;
        private bool _disposed = false;

        /// <summary>
        /// Gets or sets the DataFlow filter used for enumerating devices.
        /// Changing this property reloads the device list.
        /// </summary>
        public DataFlow? DataFlow
        {
            get
            {
                ThrowIfDisposed();
                lock (_syncRoot)
                {
                    return _dataFlow;
                }
            }
            set
            {
                ThrowIfDisposed();
                lock (_syncRoot)
                {
                    if (_dataFlow != value)
                    {
                        _dataFlow = value;
                        ReloadDevices();
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of device IDs currently managed.
        /// </summary>
        public IEnumerable<string> DeviceIds
        {
            get
            {
                ThrowIfDisposed();
                lock (_syncRoot)
                {
                    return _devices.Keys.ToList();
                }
            }
        }

        /// <summary>
        /// Gets a list of MMDevice instances currently managed.
        /// </summary>
        public IEnumerable<MMDevice> Devices
        {
            get
            {
                ThrowIfDisposed();
                lock (_syncRoot)
                {
                    return _devices.Values.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the notification client for receiving device events.
        /// </summary>
        public AudioDeviceNotification NotificationClient => _notificationClient;

        /// <summary>
        /// Initializes a new instance of the AudioDeviceManager.
        /// If a DataFlow is specified, only devices matching that DataFlow are enumerated;
        /// otherwise, both Render and Capture devices are enumerated.
        /// </summary>
        public AudioDeviceManager(DataFlow? dataFlow = null)
        {
            _deviceEnumerator.RegisterEndpointNotificationCallback(_notificationClient);
            _notificationClient.DeviceAdded += (deviceId) => { lock (_syncRoot) { AddDevice(deviceId); } };
            _notificationClient.DeviceRemoved += (deviceId) => { lock (_syncRoot) { RemoveDevice(deviceId); } };
            _notificationClient.DeviceStateChanged += (deviceId, deviceState) =>
            {
                if (deviceState == DeviceState.Active)
                    lock (_syncRoot) { AddDevice(deviceId); }
                else
                    lock (_syncRoot) { RemoveDevice(deviceId); }
            };

            lock (_syncRoot)
            {
                _dataFlow = dataFlow;
                ReloadDevices();
            }
        }

        /// <summary>
        /// Reloads the list of devices based on the current DataFlow setting.
        /// Removes invalid devices and adds new active ones.
        /// </summary>
        private void ReloadDevices()
        {
            // Remove devices that are no longer valid.
            foreach (var key in _devices.Keys.ToList())
            {
                if (!IsDeviceValid(_devices[key]))
                {
                    RemoveDevice(key);
                }
            }

            // Enumerate new devices.
            var deviceCollections = new List<MMDeviceCollection>();
            if (_dataFlow.HasValue)
            {
                deviceCollections.Add(_deviceEnumerator.EnumerateAudioEndPoints(_dataFlow.Value, DeviceState.Active));
            }
            else
            {
                deviceCollections.Add(_deviceEnumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Render, DeviceState.Active));
                deviceCollections.Add(_deviceEnumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, DeviceState.Active));
            }

            foreach (MMDeviceCollection collection in deviceCollections)
            {
                foreach (var device in collection)
                {
                    if (!_devices.ContainsKey(device.ID))
                    {
                        _devices.Add(device.ID, device);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to add a device by its ID.
        /// </summary>
        private void AddDevice(string deviceId)
        {
            if (!_devices.ContainsKey(deviceId))
            {
                MMDevice? newDevice = null;
                try
                {
                    newDevice = _deviceEnumerator.GetDevice(deviceId);
                    if (IsDeviceValid(newDevice))
                        _devices.Add(deviceId, newDevice);
                }
                catch (Exception ex)
                {
                    newDevice?.Dispose();
                    Debug.WriteLine($"Failed to add device: {deviceId}, Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Removes a device by its ID and disposes it.
        /// </summary>
        private void RemoveDevice(string deviceId)
        {
            if (_devices.TryGetValue(deviceId, out var device))
            {
                _devices.Remove(deviceId);
                device.Dispose();
            }
        }

        /// <summary>
        /// Checks if a device is valid based on the DataFlow filter (if set) and whether it is active.
        /// </summary>
        private bool IsDeviceValid(MMDevice mmDevice)
        {
            return (_dataFlow == null || mmDevice.DataFlow == _dataFlow) &&
                   mmDevice.State == DeviceState.Active;
        }

        /// <summary>
        /// Indexer to retrieve a device by its ID.
        /// </summary>
        public MMDevice this[string deviceId]
        {
            get
            {
                ThrowIfDisposed();
                lock (_syncRoot)
                {
                    return _devices[deviceId];
                }
            }
        }

        /// <summary>
        /// Attempts to get a device by its ID.
        /// </summary>
        public bool TryGetDevice(string deviceId, [NotNullWhen(true)] out MMDevice? device)
        {
            ThrowIfDisposed();
            lock (_syncRoot)
            {
                return _devices.TryGetValue(deviceId, out device);
            }
        }

        /// <summary>
        /// Attempts to get the default device for the specified DataFlow and Role.
        /// </summary>
        public bool TryGetDefaultDevice(DataFlow dataFlow, Role role, [NotNullWhen(true)] out MMDevice? device)
        {
            ThrowIfDisposed();
            device = null;
            lock (_syncRoot)
            {
                MMDevice? tempDefaultDevice = null;
                try
                {
                    tempDefaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, role);
                    return _devices.TryGetValue(tempDefaultDevice.ID, out device);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load default device: {tempDefaultDevice?.ID}, Error: {ex.Message}");
                }
                finally
                {
                    tempDefaultDevice?.Dispose();
                }
            }
            return false;
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
        /// Disposes the AudioDeviceManager, unregistering notifications and disposing all devices.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _deviceEnumerator.UnregisterEndpointNotificationCallback(_notificationClient);
                lock (_syncRoot)
                {
                    foreach (var device in _devices.Values)
                    {
                        device.Dispose();
                    }
                    _devices.Clear();
                }
                _disposed = true;
            }
        }
    }
}

