// See https://aka.ms/new-console-template for more information

using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ApplicationExample {
    internal class Program {
        private static async Task WaitForKey() {
            Console.WriteLine("Press any key to continue.");
            while (!Console.KeyAvailable) {
                await Task.Delay(1);
            }
            Console.ReadKey(true);
        }

        private static async Task osuPlug() {
            // Now that we've seen all of the different parts of Buttplug, let's
            // put them together in a small program.
            //
            // This program will:
            // - Create an websocket connector
            // - Scan, this time using real Managers, so we'll see devices
            //   (assuming you have them hooked up)
            // - List the connected devices for the user
            // - Let the user select a device, and trigger some sort of event on
            //   that device (vibration, thrusting, etc...).

            // As usual, we start off with our connector setup. We really don't
            // need access to the connector this time, so we can just pass the
            // created connector directly to the client.
            var client = new ButtplugClient("osu!plug");

            // Whenever a client connects, it asks the server for a list of devices
            // that may already be connected. Therefore we'll want to set up our
            // device handlers before we connect, so we can see what devices may
            // already be connected to the server.
            void HandleDeviceAdded(object aObj, DeviceAddedEventArgs aArgs) {
                Console.WriteLine($"Device connected: {aArgs.Device.Name}");
            }

            client.DeviceAdded += HandleDeviceAdded;

            void HandleDeviceRemoved(object aObj, DeviceRemovedEventArgs aArgs) {
                Console.WriteLine($"Device connected: {aArgs.Device.Name}");
            }

            client.DeviceRemoved += HandleDeviceRemoved;

            // Now we can connect.
            await client.ConnectAsync(new ButtplugWebsocketConnector(new Uri("ws://127.0.0.1:12345")));


            // The structure here is gonna get a little weird now, because I'm
            // using method scoped functions. We'll be defining our scanning
            // function first, then running it just to find any devices up front.
            // Then we'll define our command sender. Finally, with all of that
            // done, we'll end up in our main menu

            // Here's the scanning part. Pretty simple, just scan until the user
            // hits a button. Any time a new device is found, print it so the
            // user knows we found it.
            async Task ScanForDevices() {
                Console.WriteLine("Scanning for devices until key is pressed.");
                Console.WriteLine("Found devices will be printed to console.");
                await client.StartScanningAsync();
                await WaitForKey();

                // Stop scanning now, 'cause we don't want new devices popping up anymore.
                await client.StopScanningAsync();
            }

            // Scan for devices before we get to the main menu.
            await ScanForDevices();

            // Now we define the device control menus. After we've scanned for
            // devices, the user can use this menu to select a device, then
            // select an action for that device to take.
            async Task ControlDevice() {
                // Controlling a device has 2 steps: selecting the device to
                // control, and choosing which command to send. We'll just list
                // the devices the client has available, then search the device
                // message capabilities once that's done to figure out what we
                // can send. Note that this is using the Device Index, which is
                // assigned by the device manager and may not be sequential
                // (which is why we can't just use an array index).

                // Of course, if we don't have any devices yet, that's not gonna work.
                if (!client.Devices.Any()) {
                    Console.WriteLine("No devices available. Please scan for a device.");
                    return;
                }

                var options = new List<uint>();

                foreach (var dev in client.Devices) {
                    Console.WriteLine($"{dev.Index}. {dev.Name}");
                    options.Add(dev.Index);
                }
                Console.WriteLine("Choose a device: ");
                if (!uint.TryParse(Console.ReadLine(), out var deviceChoice) ||
                    !options.Contains(deviceChoice)) {
                    Console.WriteLine("Invalid choice");
                    return;
                }

                var device = client.Devices.First(dev => dev.Index == deviceChoice);


                Process[] processList = Process.GetProcessesByName("osu!");

                if (processList.Length > 0) {
                    string currentProcessName = processList[0].MainWindowTitle;
                    if (currentProcessName == "osu!") {
                        try {
                            await device.VibrateAsync(0.5);
                            await Task.Delay(1000);
                            await device.VibrateAsync(0);
                        }
                        catch (Exception e) {
                            Console.WriteLine($"Problem vibrating: {e}");
                        }
                    }
                    else {
                        try {
                            Console.WriteLine("osu is not open. bad girl");
                            await device.VibrateAsync(2.0);
                            await Task.Delay(5000);
                            Console.WriteLine("i hope this taught you a lesson >:3");
                            await device.VibrateAsync(0);
                        }
                        catch (Exception e) {
                            Console.WriteLine($"Problem vibrating: {e}");
                        }
                    }

                    // We've got a device, and a command to take on that device.
                    // Let's do this thing. For each command we'll either run at a
                    // speed, then stop, or move to a position, then back again. To
                    // ensure that we don't have to deal with concurrent commands
                    // (again, for sake of example simplicity, real world situations
                    // are gonna be far more dynamic than this), we'll just block
                    // while this action is happening.
                    //
                    // We'll wrap each of our commands in a ButtplugDeviceException
                    // try block, as a device might be disconnected between the time
                    // we enter the command menu and send the command, and we don't
                    // want to crash when that happens.
                }


            }

            await ControlDevice();
        }

        private static void Main() {
            // Setup a client, and wait until everything is done before exiting.
            osuPlug().Wait();
        }
    }
}
