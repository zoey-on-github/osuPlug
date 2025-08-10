// See https://aka.ms/new-console-template for more information

using Buttplug.Client;
using System.Diagnostics;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels;
using ProcessMemoryDataFinder;

namespace osuPlug;


internal class Program {
    private static async Task WaitForKey() {
        Console.WriteLine("Press any key to continue.");
        while (!Console.KeyAvailable) {
            await Task.Delay(1);
        }
        Console.ReadKey(true);
    }
    private static readonly StructuredOsuMemoryReader osuReader = new(new ProcessTargetOptions("osu!"));

    private static async Task osuPlug() {
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
            while (true)
            {
                if (!client.Devices.Any())
                {
                    // Scan for devices before we get to the main menu.
                    await ScanForDevices();
                    Console.WriteLine("No devices available. Please scan for a device.");
                }
                else
                {
                    break;
                }
            }

            var options = new List<uint>();
            foreach (var dev in client.Devices) {
                Console.WriteLine($"{dev.Index}. {dev.Name}");
                options.Add(dev.Index);
            }

            Console.WriteLine("Choose a device: ");
            uint deviceChoice;
            while (true)
            {
                var userinput = Console.ReadLine();
                var validchoice = uint.TryParse(userinput, out deviceChoice);
                if (!validchoice || !options.Contains(deviceChoice))
                {
                    Console.WriteLine("Invalid choice, Try again:");
                }
                else
                {
                    var selectedDevice = client.Devices.First(d => d.Index == deviceChoice);
                    Console.WriteLine($"Device {selectedDevice.Name} selected");
                    break;
                }
            }


            var device = client.Devices.First(dev => dev.Index == deviceChoice);

            ushort previousMisscount = 0;
            ushort misscount = 0;
            var processList = Process.GetProcessesByName("osu!");
            var baseAddresses = new OsuBaseAddresses();
            StructuredOsuMemoryReader.GetInstance(new ProcessTargetOptions("osu!"));
            var badGirlWarning = false;
            while (true)
            {
                //i didn't want to put this sleep call here, so that a bunch of misses in quick succession could result in a bunch of vibrations
                // but if i didn't put it here, the misscount would go to 0 like i was being ratelimited or something
                //Console.WriteLine(processList[0]);
                if (processList.Length > 0)
                {
                    //i really wanted to keep this in, but this would just keep printing forever because it's inside the while loop
                    //*might* move it outside so i can still have it print to console
                    //Console.WriteLine("osu's open. good girl :3");
                    osuReader.TryRead(baseAddresses);
                    osuReader.TryRead(baseAddresses.Beatmap);
                    osuReader.TryRead(baseAddresses.Skin);
                    osuReader.TryRead(baseAddresses.GeneralData);
                    osuReader.TryRead(baseAddresses.BanchoUser);
                    osuReader.TryRead(baseAddresses.Player);
                    misscount = baseAddresses.Player.HitMiss;

                    if (misscount < previousMisscount)
                    {
                        // Reset detected (e.g. player retried song)
                        previousMisscount = 0;
                        Console.WriteLine($"misscount reset {misscount}");
                    }

                    if (previousMisscount < misscount)
                    {
                        Console.WriteLine($"misscount {misscount}");
                        Console.WriteLine($"previous misscount {previousMisscount}");
                        try
                        {
                            await device.VibrateAsync(0.2);
                            await Task.Delay(1000);
                            await device.VibrateAsync(0);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Problem vibrating: {e}");
                        }
                        previousMisscount = misscount;
                    }
                }
                else
                {
                    try
                    {
                        if (!badGirlWarning)
                        {
                            Console.WriteLine("osu is not open. bad girl");
                            await device.VibrateAsync(0.5);
                            await Task.Delay(5000);
                            await device.VibrateAsync(0);
                            Console.WriteLine("i hope this taught you a lesson >:3");
                            badGirlWarning = true;
                        }
                        processList = Process.GetProcessesByName("osu!");
                        baseAddresses = new OsuBaseAddresses();
                        StructuredOsuMemoryReader.GetInstance(new ProcessTargetOptions("osu!"));
                        //return so we dont vibrate forever
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Problem vibrating: {e}");
                    }
                }
            }

        }

        await ControlDevice();
    }
    private static void Main() {
        // Setup a client, and wait until everything is done before exiting.
        osuPlug().Wait();
    }
}
