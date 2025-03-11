# NAudioExtend

NAudioExtend is an open-source C# library that builds upon [NAudio](https://github.com/naudio/NAudio) to provide enhanced audio device management and stream switching capabilities. It is designed to simplify working with Windows audio devices, supporting dynamic default device changes, manual device selection, and runtime audio stream reconfiguration.  
  
## Features  
  
- **Advanced Audio Device Management**  
  Easily enumerate, monitor, and filter audio devices (render and capture) with thread-safe operations.  
  
- **Dynamic Device Selection**  
  Choose between automatic (system default) or manual device selection modes with real-time event notifications.  
  
- **Stream Switching Provider**  
  Seamlessly switch between different audio streams at runtime while handling playback state and errors.  
  
- **Reconfigurable WASAPI Output**  
  Create a configurable WASAPI output that allows dynamic device or configuration changes without restarting your application.  
  
## Getting Started
  
### Installation
  
1. Clone the repository:  
```C#
       git clone https://github.com/yourusername/NAudioExtend.git
```
2. Open the solution in Visual Studio and build the project.  
3. Add a reference to the built library in your project.  
  
### Usage Example
  
Below is a brief example of how to use the library to manage audio devices:  
```C#
    // Include the necessary namespace.
    using NAudioExtend.AudioDevice;
    
    // Initialize the device manager filtering for Render devices.
    using var deviceManager = new AudioDeviceManager(DataFlow.Render);
    
    // List all device IDs.
    foreach (var id in deviceManager.DeviceIds)
    {
        Console.WriteLine("Device ID: " + id);
    }
    
    // Try to get the default audio device.
    if (deviceManager.TryGetDefaultDevice(DataFlow.Render, Role.Multimedia, out var defaultDevice))
    {
        Console.WriteLine("Default Device: " + defaultDevice.FriendlyName);
    }
```
And an example of using the stream switch provider:  
```C#
    using NAudioExtend.Provider;
    using NAudio.Wave;
    
    // Assume you have an initial IExtendProvider (e.g., an audio file reader)
    IExtendProvider initialProvider = new YourExtendProviderImplementation();
    
    // Create a StreamSwitchProvider with the initial provider.
    using var streamSwitch = new StreamSwitchProvider(initialProvider);
    
    // Read from the provider.
    byte[] buffer = new byte[4096];
    int bytesRead = streamSwitch.Read(buffer, 0, buffer.Length);
    
    // Change provider when needed.
    IExtendProvider newProvider = new YourOtherProviderImplementation();
    streamSwitch.ChangeProvider(newProvider);
```
  
### StreamSwitchProvider Example:  
  
StreamSwitchProvider is designed to enable seamless switching of the audio source by passing it to WasapiOut’s Init method. Once WasapiOut is initialized with a StreamSwitchProvider instance, the provider can internally switch the data source without the need to reinitialize WasapiOut.  
By calling ChangeProvider on the StreamSwitchProvider, the audio data sent to WasapiOut is seamlessly updated to reflect the new source, minimizing interruptions and audible artifacts.  
  
Example usage:  
```C#
    // Create an initial IExtendProvider (for example, an audio file reader)
    IExtendProvider initialProvider = new YourInitialProvider();

    // Instantiate the StreamSwitchProvider with the initial provider
    var streamSwitchProvider = new StreamSwitchProvider(initialProvider);

    // Create a WasapiOut instance with the desired configuration parameters
    var wasapiOut = new WasapiOut(currentDevice, shareMode, useEventSync, latency);

    // Initialize WasapiOut with the StreamSwitchProvider. This enables internal source switching.
    wasapiOut.Init(streamSwitchProvider);

    // Start playback
    wasapiOut.Play();

    // When it’s time to switch the source, create a new IExtendProvider
    IExtendProvider newProvider = new YourNewProvider();

    // Change the provider inside the StreamSwitchProvider
    streamSwitchProvider.ChangeProvider(newProvider);

    // The audio data flowing to WasapiOut will now be seamlessly sourced from the new provider,
    // without reinitializing WasapiOut or causing playback interruptions.
```
This approach leverages the Init method of WasapiOut to handle dynamic source changes internally within StreamSwitchProvider, ensuring smooth and seamless transitions between different audio sources.  
  
  
## License  
  
This project is licensed under the MIT License - see the LICENSE file for details.  
