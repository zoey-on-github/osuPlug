// See https://aka.ms/new-console-template for more information

using Buttplug.Client;
using System.Diagnostics;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels;
using ProcessMemoryDataFinder;

namespace osuPlug;

public partial class MainForm : Form
{
    private ButtplugClient? client;
    private StructuredOsuMemoryReader osuReader = new(new ProcessTargetOptions("osu!"));
    private bool isConnected = false;
    private bool isMonitoring = false;

    // UI Controls
    private Button btnConnect = null!;
    private Button btnScan = null!;
    private Button btnStartMonitoring = null!;
    private ComboBox cmbDevices = null!;
    private ListBox lstLog = null!;
    private Label lblStatus = null!;
    private Label lblOsuStatus = null!;
    private Label lblMissCount = null!;
    private Label lblCombo = null!;
    private Label lblSliderBreaks = null!;
    private CheckBox chkMissVibration = null!;
    private CheckBox chkSliderBreakVibration = null!;
    private CheckBox chkModVibration = null!;
    private NumericUpDown numVibrationIntensity = null!;
    private NumericUpDown numVibrationDuration = null!;

    public MainForm()
    {
        InitializeUI();
        // Auto-connect to Intiface on startup
        _ = Task.Run(async () => await AutoConnectAsync());
    }

    private void InitializeUI()
    {
        this.Text = "osuPlug - Buttplug.io Integration";
        this.Size = new Size(700, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 240, 240);
        this.Font = new Font("Segoe UI", 9F);

        // Create a modern panel for the main content
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = Color.White
        };

        // Header section
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(255, 102, 170), // osu! pink
            Padding = new Padding(20, 15, 20, 15)
        };

        var titleLabel = new Label
        {
            Text = "osuPlug",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 15),
            AutoSize = true
        };

        var subtitleLabel = new Label
        {
            Text = "Buttplug.io Integration for osu!",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(200, 200, 200),
            Location = new Point(20, 45),
            AutoSize = true
        };

        headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });

        // Control buttons panel
        var buttonPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(248, 249, 250),
            Padding = new Padding(20, 10, 20, 10)
        };

        btnConnect = CreateModernButton("Connect to Intiface", new Point(20, 10), new Size(140, 35));
        btnConnect.Click += BtnConnect_Click;

        btnScan = CreateModernButton("Scan for Devices", new Point(170, 10), new Size(140, 35));
        btnScan.Enabled = false;
        btnScan.Click += BtnScan_Click;

        btnStartMonitoring = CreateModernButton("Start Monitoring", new Point(320, 10), new Size(140, 35));
        btnStartMonitoring.Enabled = false;
        btnStartMonitoring.Click += BtnStartMonitoring_Click;

        buttonPanel.Controls.AddRange(new Control[] { btnConnect, btnScan, btnStartMonitoring });

        // Status panel
        var statusPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        lblStatus = CreateStatusLabel("Status: Disconnected", new Point(20, 10), Color.FromArgb(231, 76, 60));
        lblOsuStatus = CreateStatusLabel("osu! Status: Not Running", new Point(200, 10), Color.FromArgb(231, 76, 60));

        statusPanel.Controls.AddRange(new Control[] { lblStatus, lblOsuStatus });

        // Device selection
        var devicePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        var deviceLabel = new Label
        {
            Text = "Selected Device:",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(20, 15),
            AutoSize = true
        };

        cmbDevices = new ComboBox
        {
            Location = new Point(120, 12),
            Size = new Size(400, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F)
        };

        devicePanel.Controls.AddRange(new Control[] { deviceLabel, cmbDevices });

        // Main content panel
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = Color.White
        };

        // Game stats - Modern card design
        var statsGroup = CreateModernCard("Game Statistics", new Point(0, 0), new Size(320, 140));
        
        lblMissCount = CreateStatLabel("Misses: 0", new Point(20, 40));
        lblCombo = CreateStatLabel("Combo: 0", new Point(20, 65));
        lblSliderBreaks = CreateStatLabel("Slider Breaks: 0", new Point(20, 90));

        statsGroup.Controls.AddRange(new Control[] { lblMissCount, lblCombo, lblSliderBreaks });

        // Settings - Modern card design
        var settingsGroup = CreateModernCard("Vibration Settings", new Point(340, 0), new Size(320, 200));

        chkMissVibration = CreateModernCheckBox("Vibrate on Miss", new Point(20, 40), true);
        chkSliderBreakVibration = CreateModernCheckBox("Vibrate on Slider Break", new Point(20, 65), true);
        chkModVibration = CreateModernCheckBox("Vibrate on Mods", new Point(20, 90), true);

        var lblIntensity = new Label
        {
            Text = "Intensity:",
            Font = new Font("Segoe UI", 9F),
            Location = new Point(20, 120),
            AutoSize = true
        };

        numVibrationIntensity = new NumericUpDown
        {
            Location = new Point(100, 118),
            Size = new Size(80, 25),
            DecimalPlaces = 1,
            Increment = 0.1m,
            Minimum = 0.0m,
            Maximum = 1.0m,
            Value = 0.5m,
            Font = new Font("Segoe UI", 9F)
        };

        var lblDuration = new Label
        {
            Text = "Duration (ms):",
            Font = new Font("Segoe UI", 9F),
            Location = new Point(20, 155),
            AutoSize = true
        };

        numVibrationDuration = new NumericUpDown
        {
            Location = new Point(100, 153),
            Size = new Size(80, 25),
            Minimum = 100,
            Maximum = 10000,
            Value = 1000,
            Font = new Font("Segoe UI", 9F)
        };

        settingsGroup.Controls.AddRange(new Control[] { 
            chkMissVibration, chkSliderBreakVibration, chkModVibration,
            lblIntensity, numVibrationIntensity, lblDuration, numVibrationDuration
        });

        // Log - Modern card design
        var logGroup = CreateModernCard("Activity Log", new Point(0, 160), new Size(660, 200));
        
        lstLog = new ListBox
        {
            Location = new Point(20, 40),
            Size = new Size(620, 140),
            Font = new Font("Consolas", 8.5F),
            BackColor = Color.FromArgb(248, 249, 250),
            BorderStyle = BorderStyle.None
        };

        logGroup.Controls.Add(lstLog);

        contentPanel.Controls.AddRange(new Control[] { statsGroup, settingsGroup, logGroup });

        // Add all panels to main panel
        mainPanel.Controls.Add(contentPanel);
        mainPanel.Controls.Add(devicePanel);
        mainPanel.Controls.Add(statusPanel);
        mainPanel.Controls.Add(buttonPanel);
        mainPanel.Controls.Add(headerPanel);

        this.Controls.Add(mainPanel);
    }

    private Button CreateModernButton(string text, Point location, Size size)
    {
        return new Button
        {
            Text = text,
            Location = location,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 102, 170), // osu! pink
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
    }

    private Label CreateStatusLabel(string text, Point location, Color color)
    {
        return new Label
        {
            Text = text,
            Location = location,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = color,
            AutoSize = true
        };
    }

    private Label CreateStatLabel(string text, Point location)
    {
        return new Label
        {
            Text = text,
            Location = location,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(52, 73, 94),
            AutoSize = true
        };
    }

    private CheckBox CreateModernCheckBox(string text, Point location, bool isChecked)
    {
        return new CheckBox
        {
            Text = text,
            Location = location,
            Font = new Font("Segoe UI", 9F),
            Checked = isChecked,
            AutoSize = true
        };
    }

    private Panel CreateModernCard(string title, Point location, Size size)
    {
        var card = new Panel
        {
            Location = location,
            Size = size,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        // Add subtle shadow effect
        card.Paint += (sender, e) =>
        {
            var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(52, 73, 94),
            Location = new Point(0, 10),
            AutoSize = true
        };

        card.Controls.Add(titleLabel);
        return card;
    }

    private void LogMessage(string message)
    {
        if (lstLog.InvokeRequired)
        {
            lstLog.Invoke(new Action<string>(LogMessage), message);
        }
        else
        {
            lstLog.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            lstLog.SelectedIndex = lstLog.Items.Count - 1;
        }
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        // Don't try to connect if already connected
        if (isConnected && client != null)
        {
            LogMessage("Already connected to Intiface Central!");
            return;
        }

        try
        {
            btnConnect.Enabled = false;
            LogMessage("Connecting to Intiface Central...");

            client = new ButtplugClient("osu!plug");
            client.DeviceAdded += HandleDeviceAdded;
            client.DeviceRemoved += HandleDeviceRemoved;

            await client.ConnectAsync(new ButtplugWebsocketConnector(new Uri("ws://127.0.0.1:12345")));

            isConnected = true;
            lblStatus.Text = "Status: Connected";
            lblStatus.ForeColor = Color.FromArgb(46, 204, 113); // Green
            btnScan.Enabled = true;
            LogMessage("Connected to Intiface Central successfully!");
        }
        catch (Exception ex)
        {
            LogMessage($"Connection failed: {ex.Message}");
            lblStatus.Text = "Status: Connection Failed";
            lblStatus.ForeColor = Color.FromArgb(231, 76, 60); // Red
            btnConnect.Enabled = true;
        }
    }

    private void HandleDeviceAdded(object? aObj, DeviceAddedEventArgs aArgs)
    {
        LogMessage($"Device connected: {aArgs.Device.Name}");
        AddDeviceToComboBox(aArgs.Device);
    }

    private void AddDeviceToComboBox(ButtplugClientDevice device)
    {
        if (cmbDevices.InvokeRequired)
        {
            cmbDevices.Invoke(new Action<ButtplugClientDevice>(AddDeviceToComboBox), device);
            return;
        }

        cmbDevices.Items.Add($"{device.Index}. {device.Name}");
        
        // Auto-select if this is the only device
        if (cmbDevices.Items.Count == 1)
        {
            cmbDevices.SelectedIndex = 0;
            btnStartMonitoring.Enabled = true;
            
            // Auto-start monitoring if osu! is running
            if (Process.GetProcessesByName("osu!").Length > 0)
            {
                LogMessage("Auto-starting monitoring since osu! is running...");
                _ = Task.Run(async () => await StartMonitoringAsync());
            }
        }
    }

    private void HandleDeviceRemoved(object? aObj, DeviceRemovedEventArgs aArgs)
    {
        LogMessage($"Device disconnected: {aArgs.Device.Name}");
    }

    private async void BtnScan_Click(object? sender, EventArgs e)
    {
        if (client == null) return;

        try
        {
            btnScan.Enabled = false;
            LogMessage("Scanning for devices...");
            await client.StartScanningAsync();
            await Task.Delay(5000); // Scan for 5 seconds
            await client.StopScanningAsync();
            LogMessage("Device scan completed.");
            btnScan.Enabled = true;
            
            // Auto-select and start if only one device found
            await CheckAndStartMonitoring();
        }
        catch (Exception ex)
        {
            LogMessage($"Scan failed: {ex.Message}");
            btnScan.Enabled = true;
        }
    }

    private async void BtnStartMonitoring_Click(object? sender, EventArgs e)
    {
        await StartMonitoringAsync();
    }

    private async Task StartMonitoringAsync()
    {
        if (client == null || cmbDevices.SelectedItem == null) return;

        try
        {
            isMonitoring = true;
            btnStartMonitoring.Enabled = false;
            LogMessage("Starting osu! monitoring...");

            var selectedDeviceText = cmbDevices.SelectedItem.ToString();
            var deviceIndex = uint.Parse(selectedDeviceText!.Split('.')[0]);
            var device = client.Devices.First(d => d.Index == deviceIndex);

            // Start monitoring without cancellation token (runs continuously like original)
            _ = Task.Run(() => MonitorOsu(device, CancellationToken.None));
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to start monitoring: {ex.Message}");
            isMonitoring = false;
            btnStartMonitoring.Enabled = true;
        }
    }

    private async Task MonitorOsu(ButtplugClientDevice device, CancellationToken cancellationToken)
    {
        ushort previousMisscount = 0;
        ushort misscount = 0;
        ushort maxCombo = 0;
        ushort previousCombo = 0;
        ushort Combo = 0;
        ushort sliderBreak = 0;
        ushort previousSB = 0;
        var processList = Process.GetProcessesByName("osu!");
        var baseAddresses = new OsuBaseAddresses();
        StructuredOsuMemoryReader.GetInstance(new ProcessTargetOptions("osu!"));

        while (true) // Run continuously like the original app
        {
            try
            {
                if (processList.Length > 0 && !processList[0].HasExited)
                {
                    UpdateOsuStatus(true);
                    osuReader.TryRead(baseAddresses);
                    osuReader.TryRead(baseAddresses.Beatmap);
                    osuReader.TryRead(baseAddresses.Skin);
                    osuReader.TryRead(baseAddresses.GeneralData);
                    osuReader.TryRead(baseAddresses.BanchoUser);
                    osuReader.TryRead(baseAddresses.Player);

                    misscount = baseAddresses.Player.HitMiss;
                    Combo = baseAddresses.Player.Combo;
                    maxCombo = baseAddresses.Player.MaxCombo;

                    UpdateGameStats(misscount, Combo, sliderBreak);

                    if (misscount < previousMisscount)
                    {
                        previousMisscount = 0;
                        LogMessage("Miss count reset (song retry)");
                    }

                    if (previousMisscount < misscount && chkMissVibration.Checked)
                    {
                        LogMessage($"Miss detected! Count: {misscount}");
                        await VibrateDevice(device);
                        previousMisscount = misscount;
                    }

                    if (previousCombo < Combo)
                    {
                        previousCombo = Combo;
                    }
                    if (previousCombo > maxCombo)
                    {
                        previousCombo = 0;
                    }
                    if (previousCombo > Combo && misscount == previousMisscount)
                    {
                        previousCombo = Combo;
                        sliderBreak += 1;
                        LogMessage($"Slider break detected! Count: {sliderBreak}");
                    }

                    if (previousSB < sliderBreak && chkSliderBreakVibration.Checked)
                    {
                        await VibrateDevice(device);
                        previousSB = sliderBreak;
                    }
                    if (sliderBreak < previousSB)
                    {
                        previousSB = 0;
                    }

                    if (baseAddresses.GeneralData.Mods.Equals(2048) && 
                        baseAddresses.GeneralData.OsuStatus == OsuMemoryStatus.Playing && 
                        chkModVibration.Checked)
                    {
                        LogMessage("Mod detected - vibrating!");
                        await VibrateDevice(device);
                        await Task.Delay(100);
                    }
                }
                else
                {
                    UpdateOsuStatus(false);
                    processList = Process.GetProcessesByName("osu!");
                    baseAddresses = new OsuBaseAddresses();
                    StructuredOsuMemoryReader.GetInstance(new ProcessTargetOptions("osu!"));
                }

                await Task.Delay(10);
            }
            catch (Exception ex)
            {
                LogMessage($"Monitoring error: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    private async Task VibrateDevice(ButtplugClientDevice device)
    {
        try
        {
            var intensity = (double)numVibrationIntensity.Value;
            var duration = (int)numVibrationDuration.Value;
            
            await device.VibrateAsync(intensity);
            await Task.Delay(duration);
            await device.VibrateAsync(0);
        }
        catch (Exception ex)
        {
            LogMessage($"Vibration error: {ex.Message}");
        }
    }

    private async Task AutoConnectAsync()
    {
        try
        {
            LogMessage("Auto-connecting to Intiface Central...");
            
            client = new ButtplugClient("osu!plug");
            client.DeviceAdded += HandleDeviceAdded;
            client.DeviceRemoved += HandleDeviceRemoved;

            await client.ConnectAsync(new ButtplugWebsocketConnector(new Uri("ws://127.0.0.1:12345")));

            isConnected = true;
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => {
                    lblStatus.Text = "Status: Connected";
                    lblStatus.ForeColor = Color.FromArgb(46, 204, 113); // Green
                }));
            }
            else
            {
                lblStatus.Text = "Status: Connected";
                lblStatus.ForeColor = Color.FromArgb(46, 204, 113); // Green
            }
            
            btnScan.Enabled = true;
            LogMessage("Auto-connected to Intiface Central successfully!");
            
            // Auto-scan for devices
            await Task.Delay(1000);
            await AutoScanAsync();
        }
        catch (Exception ex)
        {
            LogMessage($"Auto-connection failed: {ex.Message}");
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => {
                    lblStatus.Text = "Status: Connection Failed";
                    lblStatus.ForeColor = Color.FromArgb(231, 76, 60); // Red
                }));
            }
            else
            {
                lblStatus.Text = "Status: Connection Failed";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60); // Red
            }
        }
    }

    private async Task AutoScanAsync()
    {
        if (client == null) return;

        try
        {
            LogMessage("Auto-scanning for devices...");
            await client.StartScanningAsync();
            await Task.Delay(3000); // Scan for 3 seconds
            await client.StopScanningAsync();
            LogMessage("Auto-scan completed.");
            
            // Auto-select and start if only one device found
            if (cmbDevices.InvokeRequired)
            {
                cmbDevices.Invoke(new Action(async () => await CheckAndStartMonitoring()));
            }
            else
            {
                await CheckAndStartMonitoring();
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Auto-scan failed: {ex.Message}");
        }
    }

    private async Task CheckAndStartMonitoring()
    {
        if (cmbDevices.Items.Count == 1)
        {
            cmbDevices.SelectedIndex = 0;
            btnStartMonitoring.Enabled = true;
            
            // Auto-start monitoring if osu! is running
            if (Process.GetProcessesByName("osu!").Length > 0)
            {
                LogMessage("Auto-starting monitoring since osu! is running...");
                await StartMonitoringAsync();
            }
        }
        else if (cmbDevices.Items.Count > 0)
        {
            btnStartMonitoring.Enabled = true;
        }
    }

    private void UpdateOsuStatus(bool isRunning)
    {
        if (lblOsuStatus.InvokeRequired)
        {
            lblOsuStatus.Invoke(new Action<bool>(UpdateOsuStatus), isRunning);
        }
        else
        {
            lblOsuStatus.Text = isRunning ? "osu! Status: Running" : "osu! Status: Not Running";
            lblOsuStatus.ForeColor = isRunning ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60); // Green : Red
        }
    }

    private void UpdateGameStats(ushort misses, ushort combo, ushort sliderBreaks)
    {
        if (lblMissCount.InvokeRequired)
        {
            lblMissCount.Invoke(new Action<ushort, ushort, ushort>(UpdateGameStats), misses, combo, sliderBreaks);
        }
        else
        {
            lblMissCount.Text = $"Misses: {misses}";
            lblCombo.Text = $"Combo: {combo}";
            lblSliderBreaks.Text = $"Slider Breaks: {sliderBreaks}";
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        isMonitoring = false;
        client?.DisconnectAsync();
        base.OnFormClosing(e);
    }
}

internal class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
