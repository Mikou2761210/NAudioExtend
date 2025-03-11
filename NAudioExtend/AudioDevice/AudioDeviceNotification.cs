using NAudio.CoreAudioApi.Interfaces;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAudioExtend.AudioDevice
{
    /// <summary>
    /// Implements IMMNotificationClient to handle audio device notifications.
    /// </summary>
    public class AudioDeviceNotification : IMMNotificationClient
    {
        /// <summary>
        /// Event raised when a device's state changes.
        /// </summary>
        public event Action<string, DeviceState>? DeviceStateChanged;
        public void OnDeviceStateChanged(string deviceId, DeviceState newState) =>
            DeviceStateChanged?.Invoke(deviceId, newState);

        /// <summary>
        /// Event raised when a device is added.
        /// </summary>
        public event Action<string>? DeviceAdded;
        public void OnDeviceAdded(string deviceId) =>
            DeviceAdded?.Invoke(deviceId);

        /// <summary>
        /// Event raised when a device is removed.
        /// </summary>
        public event Action<string>? DeviceRemoved;
        public void OnDeviceRemoved(string deviceId) =>
            DeviceRemoved?.Invoke(deviceId);

        /// <summary>
        /// Event raised when the default device changes.
        /// </summary>
        public event Action<DataFlow, Role, string>? DefaultDeviceChanged;
        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) =>
            DefaultDeviceChanged?.Invoke(flow, role, defaultDeviceId);

        /// <summary>
        /// Event raised when a device's property value changes.
        /// </summary>
        public event Action<string, PropertyKey>? PropertyValueChanged;
        public void OnPropertyValueChanged(string deviceId, PropertyKey key) =>
            PropertyValueChanged?.Invoke(deviceId, key);
    }
}
