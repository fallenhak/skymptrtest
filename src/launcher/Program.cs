using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;

public class Program {
    [STAThread]
    public static void Main() {
        App app = new App();
        app.Run();
    }
}

public class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        LauncherWindow win = new LauncherWindow();
        win.Show();
    }
}

public class LauncherWindow {
    private Window window;
    private TextBox txtSourcePath;
    private TextBox txtDestPath;
    private TextBox txtServerIp;
    private TextBlock lblServerStatus;
    private TextBlock lblProfileBadge;
    private Button btnResetProfile;
    private Button btnCheckServer;
    private TextBlock lblVersionBadge;
    private TextBlock lblStatusText;
    private ProgressBar progressBar;
    private Button btnMainAction;
    private Button btnBrowseSource;
    private Button btnBrowseDest;
    private Button btnReinstall;
    private CheckBox chkEnableVoice;
    private TextBlock lblMicState;
    private TextBlock lblVoiceStatus;
    private VoiceManager voiceManager;

    private bool isInstalling = false;
    private bool updateRequired = false;
    private string detectedVersion = "";
    private bool isExactVersion = false;

    private int assignedProfileId = 1;
    private int serverModpackVersion = 0;
    private string serverModpackHash = "";
    private long serverModpackSizeBytes = 0;
    private int localModpackVersion = 0;
    private string localModpackHash = "";

    public LauncherWindow() {
        InitializeComponent();
        DetectSourceGame();
        EnsurePlayerIdentity();
        CheckInstallStatus();
    }

    public void Show() {
        window.Show();
    }

    private void InitializeComponent() {
        string xaml = @"
<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Title='SkyMP TR - Cok Oyunculu Baslatici ve Otomatik Guncelleyici'
        Height='640' Width='940'
        WindowStartupLocation='CenterScreen'
        Background='#0b0c12'
        ResizeMode='CanMinimize'>
    <Window.Resources>
        <Style TargetType='TextBlock'>
            <Setter Property='FontFamily' Value='Segoe UI, Arial'/>
            <Setter Property='Foreground' Value='#f0f2f5'/>
        </Style>
        <Style TargetType='TextBox'>
            <Setter Property='Background' Value='#161924'/>
            <Setter Property='Foreground' Value='#f0f2f5'/>
            <Setter Property='BorderBrush' Value='#2d3247'/>
            <Setter Property='BorderThickness' Value='1'/>
            <Setter Property='Padding' Value='8,6'/>
            <Setter Property='FontFamily' Value='Segoe UI, Arial'/>
            <Setter Property='FontSize' Value='13'/>
        </Style>
    </Window.Resources>
    
    <Grid Margin='24'>
        <Grid.RowDefinitions>
            <RowDefinition Height='Auto'/>
            <RowDefinition Height='*'/>
            <RowDefinition Height='Auto'/>
        </Grid.RowDefinitions>

        <!-- Baslik ve Banner -->
        <Border Grid.Row='0' Background='#131520' BorderBrush='#2d3247' BorderThickness='1' CornerRadius='8' Padding='18,14' Margin='0,0,0,16'>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width='*'/>
                    <ColumnDefinition Width='Auto'/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column='0'>
                    <StackPanel Orientation='Horizontal'>
                        <TextBlock Text='SKYMP' FontSize='24' FontWeight='Bold' Foreground='#d4af37' Margin='0,0,6,0'/>
                        <TextBlock Text='TR' FontSize='24' FontWeight='Bold' Foreground='#ffffff'/>
                    </StackPanel>
                    <TextBlock Text='The Elder Scrolls V: Skyrim Multiplayer • Otomatik Guncelleyici &amp; Stock Game' FontSize='12' Foreground='#8d93a8' Margin='0,2,0,0'/>
                </StackPanel>
                <Border Grid.Column='1' Background='#1e2233' BorderBrush='#3b425e' BorderThickness='1' CornerRadius='6' Padding='12,6' VerticalAlignment='Center'>
                    <TextBlock Text='Pinned SSE 1.6.1170' FontSize='12' FontWeight='SemiBold' Foreground='#d4af37'/>
                </Border>
            </Grid>
        </Border>

        <!-- Ana Kartlar -->
        <ScrollViewer Grid.Row='1' VerticalScrollBarVisibility='Auto' Margin='0,0,0,16'>
            <StackPanel>
                <!-- 1. Kart: Orijinal Skyrim SE Tespiti -->
                <Border Background='#131520' BorderBrush='#25293d' BorderThickness='1' CornerRadius='8' Padding='16' Margin='0,0,0,12'>
                    <StackPanel>
                        <TextBlock Text='1. Orijinal Skyrim Special Edition Konumu' FontSize='14' FontWeight='SemiBold' Foreground='#d4af37' Margin='0,0,0,4'/>
                        <TextBlock Text='Kullanici bilgisayarindaki orijinal Skyrim klasoru. Orijinal oyun dosyalariniza kesinlikle dokunulmaz.' FontSize='11' Foreground='#8d93a8' Margin='0,0,0,8'/>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width='*'/>
                                <ColumnDefinition Width='90'/>
                            </Grid.ColumnDefinitions>
                            <TextBox x:Name='txtSourcePath' Grid.Column='0' Margin='0,0,8,0'/>
                            <Button x:Name='btnBrowseSource' Grid.Column='1' Content='Gozat...' Background='#222638' Foreground='#ffffff' BorderBrush='#3b425e' Cursor='Hand'/>
                        </Grid>
                        <TextBlock x:Name='lblVersionBadge' Text='Surum araniyor...' FontSize='12' FontWeight='SemiBold' Margin='0,8,0,0'/>
                    </StackPanel>
                </Border>

                <!-- 2. Kart: Stock Game Kurulum Konumu -->
                <Border Background='#131520' BorderBrush='#25293d' BorderThickness='1' CornerRadius='8' Padding='16' Margin='0,0,0,12'>
                    <StackPanel>
                        <TextBlock Text='2. SkyMP TR Kurulum Konumu (Stock Game)' FontSize='14' FontWeight='SemiBold' Foreground='#d4af37' Margin='0,0,0,4'/>
                        <TextBlock Text='SkyMP TR bu klasore bagimsiz bir Stock Game olarak kurulur. Modlar, SKSE ve guncellemeler burada calisir.' FontSize='11' Foreground='#8d93a8' Margin='0,0,0,8'/>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width='*'/>
                                <ColumnDefinition Width='90'/>
                            </Grid.ColumnDefinitions>
                            <TextBox x:Name='txtDestPath' Grid.Column='0' Margin='0,0,8,0' Text='C:\SkyMPTR'/>
                            <Button x:Name='btnBrowseDest' Grid.Column='1' Content='Degistir...' Background='#222638' Foreground='#ffffff' BorderBrush='#3b425e' Cursor='Hand'/>
                        </Grid>
                    </StackPanel>
                </Border>

                <!-- 3. Kart: Baglanti ve Otomatik Kimlik Ayarlari -->
                <Border Background='#131520' BorderBrush='#25293d' BorderThickness='1' CornerRadius='8' Padding='16'>
                    <StackPanel>
                        <TextBlock Text='3. Sunucu Baglantisi ve Oyuncu Kimligi' FontSize='14' FontWeight='SemiBold' Foreground='#d4af37' Margin='0,0,0,4'/>
                        <TextBlock Text='Sunucu IP adresini (Radmin VPN IP) girin. Oyuncu profil kimliginiz otomatik olarak kalici atanir.' FontSize='11' Foreground='#8d93a8' Margin='0,0,0,8'/>
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width='*'/>
                                <ColumnDefinition Width='16'/>
                                <ColumnDefinition Width='260'/>
                            </Grid.ColumnDefinitions>
                            
                            <!-- Sol: Sunucu IP & Durum -->
                            <StackPanel Grid.Column='0'>
                                <TextBlock Text='Sunucu IP Adresi (Radmin VPN):' FontSize='11' Foreground='#8d93a8' Margin='0,0,0,4'/>
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width='*'/>
                                        <ColumnDefinition Width='80'/>
                                    </Grid.ColumnDefinitions>
                                    <TextBox x:Name='txtServerIp' Grid.Column='0' Text='127.0.0.1' Margin='0,0,6,0'/>
                                    <Button x:Name='btnCheckServer' Grid.Column='1' Content='Yenile' Background='#222638' Foreground='#ffffff' BorderBrush='#3b425e' Cursor='Hand'/>
                                </Grid>
                                <TextBlock x:Name='lblServerStatus' Text='Sunucu kontrol ediliyor...' FontSize='11' Margin='0,6,0,0'/>
                            </StackPanel>

                            <!-- Sag: Otomatik Oyuncu Kimligi -->
                            <Border Grid.Column='2' Background='#161924' BorderBrush='#2d3247' BorderThickness='1' CornerRadius='6' Padding='12,8'>
                                <Grid>
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height='Auto'/>
                                        <RowDefinition Height='Auto'/>
                                        <RowDefinition Height='Auto'/>
                                    </Grid.RowDefinitions>
                                    <Grid Grid.Row='0'>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width='*'/>
                                            <ColumnDefinition Width='Auto'/>
                                        </Grid.ColumnDefinitions>
                                        <TextBlock Grid.Column='0' Text='Oyuncu Kimligi (Otomatik):' FontSize='10' Foreground='#8d93a8'/>
                                        <Button x:Name='btnResetProfile' Grid.Column='1' Content='Yeni ID Al' Background='Transparent' Foreground='#3498db' BorderThickness='0' Cursor='Hand' FontSize='10'/>
                                    </Grid>
                                    <TextBlock x:Name='lblProfileBadge' Grid.Row='1' Text='ID: Ataniyor...' FontSize='14' FontWeight='Bold' Margin='0,4,0,2'/>
                                    <TextBlock Grid.Row='2' Text='Kalici profil. Manuel numara girisi gerekmez.' FontSize='9' Foreground='#8d93a8'/>
                                </Grid>
                            </Border>
                        </Grid>
                    </StackPanel>
                </Border>

                <!-- 4. Kart: 3D Yakinlik Sesli Sohbet (Voice Chat) -->
                <Border Background='#131520' BorderBrush='#25293d' BorderThickness='1' CornerRadius='8' Padding='16' Margin='0,0,0,12'>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width='*'/>
                            <ColumnDefinition Width='Auto'/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column='0'>
                            <TextBlock Text='4. 3 Boyutlu Yakinlik Sesli Sohbet (Proximity Voice Chat)' FontSize='14' FontWeight='SemiBold' Foreground='#d4af37' Margin='0,0,0,4'/>
                            <TextBlock Text='Oyun acikken [V] tusuna basili tutarak konusabilirsiniz. Ses yakindaki oyunculara 3D uzamsal olarak iletilir.' FontSize='11' Foreground='#8d93a8' Margin='0,0,0,8'/>
                            <StackPanel Orientation='Horizontal'>
                                <CheckBox x:Name='chkEnableVoice' Content='Sesli Sohbeti Etkinlestir' Foreground='#ffffff' IsChecked='True' VerticalAlignment='Center' Margin='0,0,16,0'/>
                                <TextBlock x:Name='lblVoiceStatus' Text='[PTT: V Tusu]' FontSize='11' Foreground='#2ecc71' VerticalAlignment='Center'/>
                            </StackPanel>
                        </StackPanel>
                        <Border Grid.Column='1' Background='#161924' BorderBrush='#2d3247' BorderThickness='1' CornerRadius='6' Padding='16,8' VerticalAlignment='Center'>
                            <StackPanel HorizontalAlignment='Center'>
                                <TextBlock Text='Mikrofon' FontSize='10' Foreground='#8d93a8' HorizontalAlignment='Center'/>
                                <TextBlock x:Name='lblMicState' Text='Hazir' FontSize='12' FontWeight='Bold' Foreground='#2ecc71' HorizontalAlignment='Center' Margin='0,2,0,0'/>
                            </StackPanel>
                        </Border>
                    </Grid>
                </Border>
            </StackPanel>
        </ScrollViewer>

        <!-- Alt Durum ve Aksiyon Paneli -->
        <Border Grid.Row='2' Background='#131520' BorderBrush='#2d3247' BorderThickness='1' CornerRadius='8' Padding='18,14'>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width='*'/>
                    <ColumnDefinition Width='Auto'/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column='0' VerticalAlignment='Center' Margin='0,0,20,0'>
                    <TextBlock x:Name='lblStatusText' Text='Hazir.' FontSize='13' Margin='0,0,0,6'/>
                    <ProgressBar x:Name='progressBar' Height='8' Minimum='0' Maximum='100' Value='0' Background='#1c1f2e' Foreground='#d4af37' BorderThickness='0'/>
                </StackPanel>
                <StackPanel Grid.Column='1' Orientation='Horizontal' VerticalAlignment='Center'>
                    <Button x:Name='btnReinstall' Content='Yeniden Kur' Background='Transparent' Foreground='#8d93a8' BorderThickness='0' Cursor='Hand' Margin='0,0,12,0' Visibility='Collapsed'/>
                    <Button x:Name='btnMainAction' Content='KURULUMU YAP VE OYNA' FontSize='14' FontWeight='Bold' Height='44' Padding='28,0' Background='#d4af37' Foreground='#000000' BorderThickness='0' Cursor='Hand'>
                        <Button.Resources>
                            <Style TargetType='Border'>
                                <Setter Property='CornerRadius' Value='6'/>
                            </Style>
                        </Button.Resources>
                    </Button>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>";

        window = (Window)XamlReader.Parse(xaml);
        txtSourcePath = (TextBox)window.FindName("txtSourcePath");
        txtDestPath = (TextBox)window.FindName("txtDestPath");
        txtServerIp = (TextBox)window.FindName("txtServerIp");
        lblServerStatus = (TextBlock)window.FindName("lblServerStatus");
        lblProfileBadge = (TextBlock)window.FindName("lblProfileBadge");
        btnResetProfile = (Button)window.FindName("btnResetProfile");
        btnCheckServer = (Button)window.FindName("btnCheckServer");
        lblVersionBadge = (TextBlock)window.FindName("lblVersionBadge");
        lblStatusText = (TextBlock)window.FindName("lblStatusText");
        progressBar = (ProgressBar)window.FindName("progressBar");
        btnMainAction = (Button)window.FindName("btnMainAction");
        btnBrowseSource = (Button)window.FindName("btnBrowseSource");
        btnBrowseDest = (Button)window.FindName("btnBrowseDest");
        btnReinstall = (Button)window.FindName("btnReinstall");
        chkEnableVoice = (CheckBox)window.FindName("chkEnableVoice");
        lblMicState = (TextBlock)window.FindName("lblMicState");
        lblVoiceStatus = (TextBlock)window.FindName("lblVoiceStatus");

        window.Closed += (s, e) => {
            if (voiceManager != null) {
                voiceManager.Stop();
                voiceManager = null;
            }
        };

        btnBrowseSource.Click += (s, e) => BrowseSourceDirectory();
        btnBrowseDest.Click += (s, e) => BrowseDestDirectory();
        btnMainAction.Click += (s, e) => HandleMainAction();
        btnReinstall.Click += (s, e) => StartInstallation();
        btnResetProfile.Click += (s, e) => ResetPlayerIdentity();
        btnCheckServer.Click += async (s, e) => await CheckForServerUpdatesAsync();
        txtSourcePath.TextChanged += (s, e) => ValidateSourceGame();
        txtServerIp.TextChanged += async (s, e) => {
            EnsurePlayerIdentity();
            await CheckForServerUpdatesAsync();
        };
    }

    private void DetectSourceGame() {
        string[] candidates = new string[] {
            @"C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition",
            @"D:\SteamLibrary\steamapps\common\Skyrim Special Edition",
            @"E:\SteamLibrary\steamapps\common\Skyrim Special Edition",
            @"F:\SteamLibrary\steamapps\common\Skyrim Special Edition",
            @"C:\Users\kerim\Games\Faalgrin\Modlist\Stock Game"
        };

        try {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 489830")) {
                if (key != null) {
                    object loc = key.GetValue("InstallLocation");
                    if (loc != null && Directory.Exists(loc.ToString())) {
                        txtSourcePath.Text = loc.ToString();
                        return;
                    }
                }
            }
        } catch { }

        foreach (string path in candidates) {
            if (File.Exists(Path.Combine(path, "SkyrimSE.exe"))) {
                txtSourcePath.Text = path;
                return;
            }
        }

        txtSourcePath.Text = "";
        lblVersionBadge.Text = "[!] SkyrimSE.exe bulunamadi. Lutfen 'Gozat...' ile oyun klasorunu secin.";
        lblVersionBadge.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
    }

    private void ValidateSourceGame() {
        string path = txtSourcePath.Text.Trim();
        string exe = Path.Combine(path, "SkyrimSE.exe");

        if (string.IsNullOrEmpty(path) || !File.Exists(exe)) {
            lblVersionBadge.Text = "[X] SkyrimSE.exe bu klasorde bulunamadi.";
            lblVersionBadge.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            isExactVersion = false;
            return;
        }

        try {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exe);
            detectedVersion = string.Format("{0}.{1}.{2}.{3}", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);

            if (detectedVersion == "1.6.1170.0") {
                lblVersionBadge.Text = "[✓] Uyumlu Surum Tespit Edildi: 1.6.1170.0 (Downgrade Gerekmiyor)";
                lblVersionBadge.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                isExactVersion = true;
            } else {
                lblVersionBadge.Text = string.Format("[!] Farkli Surum Tespit Edildi ({0}). Stock Game kurulurken otomatik 1.6.1170 surumune dusurulecektir.", detectedVersion);
                lblVersionBadge.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                isExactVersion = false;
            }
        } catch (Exception ex) {
            lblVersionBadge.Text = "[!] Surum okunamadi: " + ex.Message;
            lblVersionBadge.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
            isExactVersion = false;
        }
    }

    private void EnsurePlayerIdentity() {
        if (!window.Dispatcher.CheckAccess()) {
            window.Dispatcher.Invoke(new Action(EnsurePlayerIdentity));
            return;
        }
        string dest = txtDestPath.Text.Trim();
        string identityFile = Path.Combine(dest, "player-identity.json");
        string serverIp = txtServerIp.Text.Trim();
        bool isHost = serverIp == "127.0.0.1" || serverIp.Equals("localhost", StringComparison.OrdinalIgnoreCase);

        if (File.Exists(identityFile)) {
            try {
                string content = File.ReadAllText(identityFile);
                int id = ExtractJsonInt(content, "profileId");
                if (id > 0) {
                    if (isHost && id == 1) {
                        assignedProfileId = 1;
                    } else if (!isHost && id == 1) {
                        assignedProfileId = GenerateRandomProfileId();
                        SaveIdentity(identityFile, assignedProfileId, false);
                    } else {
                        assignedProfileId = id;
                    }
                    UpdateIdentityDisplay();
                    return;
                }
            } catch { }
        }

        if (isHost) {
            assignedProfileId = 1;
            SaveIdentity(identityFile, 1, true);
        } else {
            assignedProfileId = GenerateRandomProfileId();
            SaveIdentity(identityFile, assignedProfileId, false);
        }
        UpdateIdentityDisplay();
    }

    private int GenerateRandomProfileId() {
        return new Random().Next(10002, 99999);
    }

    private void SaveIdentity(string filePath, int profileId, bool isHost) {
        try {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string json = string.Format(
"{{\n  \"profileId\": {0},\n  \"isHost\": {1},\n  \"createdUtc\": \"{2}\"\n}}",
                profileId, isHost ? "true" : "false", DateTime.UtcNow.ToString("o")
            );
            File.WriteAllText(filePath, json, new UTF8Encoding(false));
        } catch { }
    }

    private void UpdateIdentityDisplay() {
        if (!window.Dispatcher.CheckAccess()) {
            window.Dispatcher.Invoke(new Action(UpdateIdentityDisplay));
            return;
        }
        string serverIp = txtServerIp.Text.Trim();
        bool isHost = serverIp == "127.0.0.1" || serverIp.Equals("localhost", StringComparison.OrdinalIgnoreCase);

        if (assignedProfileId == 1 && isHost) {
            lblProfileBadge.Text = "#1 (Sunucu Sahibi)";
            lblProfileBadge.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
        } else {
            lblProfileBadge.Text = string.Format("#{0} (Kalici)", assignedProfileId);
            lblProfileBadge.Foreground = new SolidColorBrush(Color.FromRgb(212, 175, 55));
        }
    }

    private void ResetPlayerIdentity() {
        var result = MessageBox.Show(
            "Mevcut oyuncu kimliginizi sifirlamak istiyor musunuz?\n\nBu islem yeni bir ID olusturur ve sunucuda sifirdan yeni bir karakter acmanizi saglar.",
            "Karakteri Sifirla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );
        if (result == MessageBoxResult.Yes) {
            assignedProfileId = GenerateRandomProfileId();
            string dest = txtDestPath.Text.Trim();
            SaveIdentity(Path.Combine(dest, "player-identity.json"), assignedProfileId, false);
            UpdateIdentityDisplay();
            ConfigureClientSettings(dest, txtServerIp.Text.Trim());
            MessageBox.Show(string.Format("Yeni oyuncu kimliginiz atandi: #{0}\nSunucuya baglandiginizda yeni karakter olusturacaksiniz.", assignedProfileId), "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CheckInstallStatus() {
        if (!window.Dispatcher.CheckAccess()) {
            window.Dispatcher.Invoke(new Action(CheckInstallStatus));
            return;
        }
        string dest = txtDestPath.Text.Trim();
        string loader = Path.Combine(dest, "skse64_loader.exe");
        string platform = Path.Combine(dest, @"Data\SKSE\Plugins\SkyrimPlatform.dll");
        string skyrimEsm = Path.Combine(dest, @"Data\Skyrim.esm");

        if (File.Exists(loader) && File.Exists(platform)) {
            Task.Run(async () => await CheckForServerUpdatesAsync());
        } else {
            bool hasPartial = File.Exists(skyrimEsm);
            btnMainAction.Content = hasPartial ? "KURULUMU TAMAMLA (Kaldigi Yerden)" : "KURULUMU YAP VE OYNA";
            btnMainAction.Background = new SolidColorBrush(Color.FromRgb(212, 175, 55));
            btnReinstall.Visibility = hasPartial ? Visibility.Visible : Visibility.Collapsed;
            lblStatusText.Text = hasPartial 
                ? "Daha once kopyalanan dosyalar tespit edildi. Eksik dosyalardan devam etmek icin butona basin."
                : "Kurulum icin 'Kurulumu Yap' butonuna basin.";
            lblServerStatus.Text = hasPartial ? "Eksik Dosyalar Var (Devam Edilebilir)" : "Kurulum bekleniyor...";
            lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(141, 147, 168));
            progressBar.Value = 0;
        }
    }

    private async Task CheckForServerUpdatesAsync() {
        string dest = "";
        string serverIp = "";
        window.Dispatcher.Invoke(new Action(() => {
            dest = txtDestPath.Text.Trim();
            serverIp = txtServerIp.Text.Trim();
        }));

        if (string.IsNullOrEmpty(serverIp)) return;

        string loader = Path.Combine(dest, "skse64_loader.exe");
        if (!File.Exists(loader)) {
            window.Dispatcher.Invoke(new Action(() => {
                lblServerStatus.Text = "Kurulum bekleniyor...";
                lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(141, 147, 168));
            }));
            return;
        }

        window.Dispatcher.Invoke(new Action(() => {
            lblServerStatus.Text = "Sunucu kontrol ediliyor...";
            lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(141, 147, 168));
        }));

        string url = string.Format("http://{0}:3000/modpack-version.json", serverIp);
        string githubVersionUrl = "https://github.com/fallenhak/skymptrtest/releases/latest/download/modpack-version.json";
        bool serverOnline = false;
        string json = null;

        try {
            using (WebClient wc = new WebClient()) {
                wc.Headers.Add("User-Agent", "SkyMPTR-Launcher");
                var task = wc.DownloadStringTaskAsync(new Uri(url));
                if (await Task.WhenAny(task, Task.Delay(3500)) == task) {
                    json = await task;
                    serverOnline = true;
                }
            }
        } catch {
            serverOnline = false;
        }

        if (serverOnline && !string.IsNullOrEmpty(json)) {
            serverModpackVersion = ExtractJsonInt(json, "version");
            serverModpackHash = ExtractJsonString(json, "hash");
            serverModpackSizeBytes = ExtractJsonLong(json, "sizeBytes");
        } else {
            // Sunucu kapali veya yanit vermediyse GitHub Releases uzerindeki en guncel mod surumune bak
            string ghJson = null;
            try {
                using (WebClient wcGh = new WebClient()) {
                    wcGh.Headers.Add("User-Agent", "SkyMPTR-Launcher");
                    var ghTask = wcGh.DownloadStringTaskAsync(new Uri(githubVersionUrl));
                    if (await Task.WhenAny(ghTask, Task.Delay(3500)) == ghTask) {
                        ghJson = await ghTask;
                    }
                }
            } catch {
                ghJson = null;
            }

            if (!string.IsNullOrEmpty(ghJson)) {
                serverModpackVersion = ExtractJsonInt(ghJson, "version");
                serverModpackHash = ExtractJsonString(ghJson, "hash");
                serverModpackSizeBytes = ExtractJsonLong(ghJson, "sizeBytes");
            }
        }

        string localFile = Path.Combine(dest, "installed-version.json");
        localModpackVersion = 0;
        localModpackHash = "";
        if (File.Exists(localFile)) {
            try {
                string localJson = File.ReadAllText(localFile);
                localModpackVersion = ExtractJsonInt(localJson, "version");
                localModpackHash = ExtractJsonString(localJson, "hash");
            } catch { }
        }

        window.Dispatcher.Invoke(new Action(() => {
            if (serverOnline) {
                lblServerStatus.Text = string.Format("Sunucu Aktif (Mod Paketi v{0})", serverModpackVersion);
                lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));

                if (serverModpackVersion > localModpackVersion || (!string.IsNullOrEmpty(serverModpackHash) && serverModpackHash != localModpackHash)) {
                    updateRequired = true;
                    btnMainAction.Content = string.Format("GUNCELLEMEYI INDIR (v{0})", serverModpackVersion);
                    btnMainAction.Background = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                    lblStatusText.Text = string.Format("[!] Sunucuda yeni mod paketi var (v{0}). Oyuna girmeden once guncelleyin!", serverModpackVersion);
                    lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                } else {
                    updateRequired = false;
                    btnMainAction.Content = "OYUNA BASLA";
                    btnMainAction.Background = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                    lblStatusText.Text = string.Format("[✓] Modlar sunucuyla esit ve guncel (v{0}).", localModpackVersion);
                    lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                    progressBar.Value = 100;
                }
            } else {
                // Sunucu cevrimdisi
                if (serverModpackVersion > localModpackVersion) {
                    updateRequired = true;
                    lblServerStatus.Text = string.Format("Sunucu Kapali | GitHub Mod Paketi (v{0})", serverModpackVersion);
                    lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(212, 175, 55));
                    btnMainAction.Content = string.Format("GUNCELLEMEYI INDIR (GitHub v{0})", serverModpackVersion);
                    btnMainAction.Background = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                    lblStatusText.Text = string.Format("[!] GitHub'da yeni mod paketi var (v{0}). Sunucu kapaliyken de guncelleyebilirsiniz.", serverModpackVersion);
                    lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
                } else {
                    lblServerStatus.Text = "Sunucu Cevrimdisi";
                    lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                    lblStatusText.Text = string.Format("[!] Sunucuya baglanilamadi ({0}:3000). Modlar yerel olarak hazir (v{1}).", serverIp, localModpackVersion);
                    lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));

                    updateRequired = false;
                    btnMainAction.Content = "OYUNA BASLA (Cevrimdisi)";
                    btnMainAction.Background = new SolidColorBrush(Color.FromRgb(70, 75, 95));
                }
            }
            btnReinstall.Visibility = Visibility.Visible;
        }));
    }

    private void BrowseSourceDirectory() {
        System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
        dlg.Description = "Orijinal Skyrim Special Edition klasorunu secin (SkyrimSE.exe'nin oldugu klasor):";
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
            txtSourcePath.Text = dlg.SelectedPath;
        }
    }

    private void BrowseDestDirectory() {
        System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
        dlg.Description = "SkyMP TR Stock Game kurulumunun yapilacagi klasoru secin:";
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
            txtDestPath.Text = dlg.SelectedPath;
            EnsurePlayerIdentity();
            CheckInstallStatus();
        }
    }

    private void HandleMainAction() {
        if (isInstalling) return;

        string dest = txtDestPath.Text.Trim();
        string loader = Path.Combine(dest, "skse64_loader.exe");

        if (!File.Exists(loader)) {
            StartInstallation();
        } else if (updateRequired) {
            StartModpackUpdate();
        } else {
            LaunchGame();
        }
    }

    private async void StartInstallation() {
        string source = txtSourcePath.Text.Trim();
        string dest = txtDestPath.Text.Trim();
        string serverIp = txtServerIp.Text.Trim();

        if (string.IsNullOrEmpty(source) || !File.Exists(Path.Combine(source, "SkyrimSE.exe"))) {
            MessageBox.Show("Lutfen gecerli bir Skyrim Special Edition kaynak klasoru secin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        isInstalling = true;
        btnMainAction.IsEnabled = false;
        btnBrowseSource.IsEnabled = false;
        btnBrowseDest.IsEnabled = false;
        btnReinstall.IsEnabled = false;
        btnResetProfile.IsEnabled = false;
        btnCheckServer.IsEnabled = false;

        EnsurePlayerIdentity();

        try {
            await Task.Run(() => PerformInstallation(source, dest, serverIp));
            window.Dispatcher.Invoke(new Action(() => {
                CheckInstallStatus();
                MessageBox.Show("SkyMP TR kurulumu basariyla tamamlandi!\n'OYUNA BASLA' butonuna basarak sunucuya baglanabilirsiniz.", "Kurulum Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        } catch (Exception ex) {
            window.Dispatcher.Invoke(new Action(() => {
                MessageBox.Show("Kurulum sirasinda bir hata olustu:\n" + ex.Message, "Kurulum Hatasi", MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatusText.Text = "Hata: " + ex.Message;
                lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }));
        } finally {
            window.Dispatcher.Invoke(new Action(() => {
                isInstalling = false;
                btnMainAction.IsEnabled = true;
                btnBrowseSource.IsEnabled = true;
                btnBrowseDest.IsEnabled = true;
                btnReinstall.IsEnabled = true;
                btnResetProfile.IsEnabled = true;
                btnCheckServer.IsEnabled = true;
            }));
        }
    }

    private void PerformInstallation(string source, string dest, string serverIp) {
        UpdateProgress(5, "Hedef dizin hazirlaniyor...");
        Directory.CreateDirectory(dest);
        string destData = Path.Combine(dest, "Data");
        Directory.CreateDirectory(destData);

        // 1. Temel oyun bilesenleri
        UpdateProgress(8, "Master dosyalar (ESM) kontrol ediliyor...");
        string sourceData = Path.Combine(source, "Data");
        string[] masters = new string[] { "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm" };
        for (int i = 0; i < masters.Length; i++) {
            string m = masters[i];
            string srcFile = Path.Combine(sourceData, m);
            string dstFile = Path.Combine(destData, m);
            if (File.Exists(srcFile)) {
                bool skipped = CopyFileIfDifferent(srcFile, dstFile);
                int pct = 8 + (int)((i + 1) * 12.0 / masters.Length);
                string status = skipped 
                    ? string.Format("ESM ({0}/{1}): {2} [Zaten mevcut, atlandi]", i + 1, masters.Length, m)
                    : string.Format("ESM kopyalaniyor ({0}/{1}): {2}...", i + 1, masters.Length, m);
                UpdateProgress(pct, status);
            }
        }

        UpdateProgress(20, "Grafik ve ses paketleri (BSA) listeleniyor...");
        string[] allBsa = Directory.GetFiles(sourceData, "*.bsa");
        List<string> bsaFiles = new List<string>();
        foreach (string bsa in allBsa) {
            string name = Path.GetFileName(bsa);
            if (name.StartsWith("Skyrim", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Dawnguard", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("HearthFires", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Dragonborn", StringComparison.OrdinalIgnoreCase)) {
                bsaFiles.Add(bsa);
            }
        }

        for (int i = 0; i < bsaFiles.Count; i++) {
            string bsa = bsaFiles[i];
            string name = Path.GetFileName(bsa);
            string dstBsa = Path.Combine(destData, name);
            FileInfo fi = new FileInfo(bsa);
            double mb = fi.Length / 1048576.0;
            int pct = 20 + (int)((i + 1) * 35.0 / (bsaFiles.Count > 0 ? bsaFiles.Count : 1));
            
            bool alreadyExists = File.Exists(dstBsa) && new FileInfo(dstBsa).Length == fi.Length && fi.Length > 0;
            if (alreadyExists) {
                UpdateProgress(pct, string.Format("BSA ({0}/{1}): {2} ({3:0} MB) [Zaten mevcut, atlandi]", i + 1, bsaFiles.Count, name, mb));
            } else {
                UpdateProgress(pct, string.Format("BSA kopyalaniyor ({0}/{1}): {2} ({3:0} MB)...", i + 1, bsaFiles.Count, name, mb));
                CopyFileIfDifferent(bsa, dstBsa);
            }
        }

        UpdateProgress(55, "Calistirici ve kutuphaneler kontrol ediliyor...");
        string[] binaries = new string[] { "SkyrimSELauncher.exe", "steam_api64.dll", "Skyrim_Default.ini" };
        foreach (string b in binaries) {
            string srcFile = Path.Combine(source, b);
            if (File.Exists(srcFile)) {
                CopyFileIfDifferent(srcFile, Path.Combine(dest, b));
            }
        }
        File.WriteAllText(Path.Combine(dest, "Skyrim.ccc"), "");

        // 2. Surum Denetimi ve Downgrade
        string exeSrc = Path.Combine(source, "SkyrimSE.exe");
        string exeDest = Path.Combine(dest, "SkyrimSE.exe");
        string binkSrc = Path.Combine(source, "bink2w64.dll");
        string binkDest = Path.Combine(dest, "bink2w64.dll");

        if (isExactVersion) {
            UpdateProgress(60, "1.6.1170 calistirici kontrol ediliyor...");
            CopyFileIfDifferent(exeSrc, exeDest);
            if (File.Exists(binkSrc)) CopyFileIfDifferent(binkSrc, binkDest);
        } else {
            UpdateProgress(60, "Oyun 1.6.1170 surumune downgrade ediliyor...");
            ExtractDowngradeBinaries(dest);
        }

        // 3. Modlar, SKSE ve SkyMP Client Kurulumu
        UpdateProgress(75, "SKSE, Engine Fixes, Display Tweaks ve SkyMP paketleri kontrol ediliyor...");
        ExtractCompanionData(dest);

        // 4. Ayarlarin Yapilandirilmasi
        UpdateProgress(90, "Sunucu ve ekran ayarlari yapilandiriliyor...");
        ConfigureClientSettings(dest, serverIp);
        ConfigureDisplayTweaks(dest);

        UpdateProgress(100, "Kurulum tamamlandi!");
    }

    private void EnsureCompanionDataDownloaded(string dest) {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string zipPath = Path.Combine(appDir, "SkyMPTR-Data.zip");
        if (File.Exists(zipPath)) return;

        string labDir = Path.Combine(appDir, @"..\..\.local\skymp-756fb86\skyrim-1.6.1170\lab-player-1\game");
        if (Directory.Exists(labDir)) return;

        UpdateProgress(60, "SkyMP TR veri paketi GitHub'dan indiriliyor...");
        string githubDataUrl = "https://github.com/fallenhak/skymptrtest/releases/latest/download/SkyMPTR-Data.zip";
        string tempZip = zipPath + ".tmp";
        if (File.Exists(tempZip)) File.Delete(tempZip);

        try {
            using (WebClient wc = new WebClient()) {
                wc.Headers.Add("User-Agent", "SkyMPTR-Launcher");
                ManualResetEvent done = new ManualResetEvent(false);
                Exception dlErr = null;
                wc.DownloadProgressChanged += (s, e) => {
                    double mbRec = e.BytesReceived / 1048576.0;
                    double mbTotal = e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive / 1048576.0 : 206.0;
                    int pct = 60 + (int)(e.ProgressPercentage * 0.14);
                    UpdateProgress(pct, string.Format("GitHub'dan paket indiriliyor: %{0} ({1:0.0} / {2:0.0} MB)", e.ProgressPercentage, mbRec, mbTotal));
                };
                wc.DownloadFileCompleted += (s, e) => {
                    if (e.Error != null) dlErr = e.Error;
                    done.Set();
                };
                wc.DownloadFileAsync(new Uri(githubDataUrl), tempZip);
                done.WaitOne();
                if (dlErr != null) throw dlErr;
            }
            if (File.Exists(tempZip)) {
                File.Move(tempZip, zipPath);
            }
        } catch (Exception ex) {
            if (File.Exists(tempZip)) File.Delete(tempZip);
            throw new Exception("GitHub'dan SkyMPTR-Data.zip indirilemedi:\n" + ex.Message);
        }
    }

    private void ExtractDowngradeBinaries(string dest) {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string bundledExe = Path.Combine(appDir, @"downgrade\SkyrimSE.exe");
        string bundledBink = Path.Combine(appDir, @"downgrade\bink2w64.dll");

        if (File.Exists(bundledExe)) {
            CopyFileIfDifferent(bundledExe, Path.Combine(dest, "SkyrimSE.exe"));
            if (File.Exists(bundledBink)) CopyFileIfDifferent(bundledBink, Path.Combine(dest, "bink2w64.dll"));
        } else {
            EnsureCompanionDataDownloaded(dest);
            string zipPath = Path.Combine(appDir, "SkyMPTR-Data.zip");
            if (File.Exists(zipPath)) {
                using (ZipArchive zip = ZipFile.OpenRead(zipPath)) {
                    foreach (ZipArchiveEntry entry in zip.Entries) {
                        if (entry.FullName.Equals("SkyrimSE.exe", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.Equals("bink2w64.dll", StringComparison.OrdinalIgnoreCase)) {
                            ExtractEntryIfDifferent(entry, Path.Combine(dest, entry.Name));
                        }
                    }
                }
            }
        }
    }

    private void ExtractCompanionData(string dest) {
        EnsureCompanionDataDownloaded(dest);
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string zipPath = Path.Combine(appDir, "SkyMPTR-Data.zip");

        if (File.Exists(zipPath)) {
            using (ZipArchive zip = ZipFile.OpenRead(zipPath)) {
                int total = zip.Entries.Count;
                int cur = 0;
                foreach (ZipArchiveEntry entry in zip.Entries) {
                    cur++;
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string destPath = Path.Combine(dest, entry.FullName);
                    ExtractEntryIfDifferent(entry, destPath);
                    if (cur % 25 == 0 || cur == total) {
                        int pct = 75 + (int)(cur * 15.0 / (total > 0 ? total : 1));
                        UpdateProgress(pct, string.Format("Mod dosyalari kontrol ediliyor ({0}/{1})...", cur, total));
                    }
                }
            }
        } else {
            string labDir = Path.Combine(appDir, @"..\..\.local\skymp-756fb86\skyrim-1.6.1170\lab-player-1\game");
            if (Directory.Exists(labDir)) {
                CopyDirectoryRecursive(labDir, dest);
            }
        }
    }

    private async void StartModpackUpdate() {
        if (isInstalling) return;
        isInstalling = true;

        btnMainAction.IsEnabled = false;
        btnBrowseSource.IsEnabled = false;
        btnBrowseDest.IsEnabled = false;
        btnReinstall.IsEnabled = false;
        btnResetProfile.IsEnabled = false;
        btnCheckServer.IsEnabled = false;

        string dest = txtDestPath.Text.Trim();
        string serverIp = txtServerIp.Text.Trim();
        string downloadUrl = string.Format("http://{0}:3000/modpack.zip", serverIp);
        string githubDownloadUrl = "https://github.com/fallenhak/skymptrtest/releases/latest/download/modpack.zip";
        string tempZip = Path.Combine(dest, "modpack_update.tmp.zip");

        try {
            UpdateProgress(0, "Guncelleme paketi indiriliyor...");
            if (File.Exists(tempZip)) File.Delete(tempZip);

            bool downloaded = false;
            // 1. Once yerel sunucuyu dene
            try {
                using (WebClient wc = new WebClient()) {
                    wc.Headers.Add("User-Agent", "SkyMPTR-Launcher");
                    wc.DownloadProgressChanged += (s, e) => {
                        double mbRec = e.BytesReceived / 1048576.0;
                        double mbTotal = e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive / 1048576.0 : (serverModpackSizeBytes / 1048576.0);
                        UpdateProgress(e.ProgressPercentage, string.Format("Sunucudan mod paketi indiriliyor: %{0} ({1:0.0} / {2:0.0} MB)", e.ProgressPercentage, mbRec, mbTotal));
                    };
                    var dlTask = wc.DownloadFileTaskAsync(new Uri(downloadUrl), tempZip);
                    if (await Task.WhenAny(dlTask, Task.Delay(4000)) == dlTask) {
                        await dlTask;
                        if (File.Exists(tempZip) && new FileInfo(tempZip).Length > 1000) {
                            downloaded = true;
                        }
                    }
                }
            } catch { }

            // 2. Sunucu yanit vermezse GitHub Releases'den son hizda indir
            if (!downloaded) {
                if (File.Exists(tempZip)) File.Delete(tempZip);
                using (WebClient wc = new WebClient()) {
                    wc.Headers.Add("User-Agent", "SkyMPTR-Launcher");
                    wc.DownloadProgressChanged += (s, e) => {
                        double mbRec = e.BytesReceived / 1048576.0;
                        double mbTotal = e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive / 1048576.0 : (serverModpackSizeBytes / 1048576.0);
                        UpdateProgress(e.ProgressPercentage, string.Format("GitHub'dan mod paketi indiriliyor: %{0} ({1:0.0} / {2:0.0} MB)", e.ProgressPercentage, mbRec, mbTotal));
                    };
                    await wc.DownloadFileTaskAsync(new Uri(githubDownloadUrl), tempZip);
                }
            }

            UpdateProgress(90, "Guncelleme dosyalari cikartiliyor...");
            await Task.Run(() => {
                using (ZipArchive zip = ZipFile.OpenRead(tempZip)) {
                    int total = zip.Entries.Count;
                    int cur = 0;
                    foreach (ZipArchiveEntry entry in zip.Entries) {
                        cur++;
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string destRoot = Path.GetFullPath(dest);
                        string destPath = Path.GetFullPath(Path.Combine(dest, entry.FullName));
                        if (!destPath.StartsWith(destRoot + Path.DirectorySeparatorChar) && !destPath.Equals(destRoot, StringComparison.OrdinalIgnoreCase)) {
                            continue; // Path traversal protection (R3)
                        }
                        string entryDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(entryDir) && !Directory.Exists(entryDir)) {
                            Directory.CreateDirectory(entryDir);
                        }
                        entry.ExtractToFile(destPath, true); // Overwrite on modpack update (R2)
                        if (cur % 20 == 0 || cur == total) {
                            int pct = 90 + (int)(cur * 9.0 / (total > 0 ? total : 1));
                            UpdateProgress(pct, string.Format("Guncelleme dosyalari kuruluyor ({0}/{1})...", cur, total));
                        }
                    }
                }
                if (File.Exists(tempZip)) File.Delete(tempZip);

                string vJson = string.Format(
"{{\n  \"version\": {0},\n  \"hash\": \"{1}\",\n  \"updatedAtUtc\": \"{2}\"\n}}",
                    serverModpackVersion, serverModpackHash, DateTime.UtcNow.ToString("o")
                );
                File.WriteAllText(Path.Combine(dest, "installed-version.json"), vJson, new UTF8Encoding(false));

                ConfigureClientSettings(dest, serverIp);
                ConfigureDisplayTweaks(dest);
            });

            window.Dispatcher.Invoke(new Action(() => {
                UpdateProgress(100, string.Format("Guncelleme tamamlandi! Mod paketi v{0} aktif.", serverModpackVersion));
                updateRequired = false;
                btnMainAction.Content = "OYUNA BASLA";
                btnMainAction.Background = new SolidColorBrush(Color.FromRgb(39, 174, 96));
                lblServerStatus.Text = string.Format("Sunucu Aktif (Mod Paketi v{0})", serverModpackVersion);
                lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                lblStatusText.Text = string.Format("[✓] Modlar sunucuyla esit ve guncel (v{0}).", serverModpackVersion);
                lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));

                MessageBox.Show("Mod paketi guncellemesi basariyla tamamlandi!\n'OYUNA BASLA' butonuna basarak sunucuya katilabilirsiniz.", "Guncelleme Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
            }));
        } catch (Exception ex) {
            window.Dispatcher.Invoke(new Action(() => {
                MessageBox.Show("Guncelleme indirilirken hata olustu:\n" + ex.Message, "Guncelleme Hatasi", MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatusText.Text = "Guncelleme hatasi: " + ex.Message;
                lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }));
        } finally {
            window.Dispatcher.Invoke(new Action(() => {
                isInstalling = false;
                btnMainAction.IsEnabled = true;
                btnBrowseSource.IsEnabled = true;
                btnBrowseDest.IsEnabled = true;
                btnReinstall.IsEnabled = true;
                btnResetProfile.IsEnabled = true;
                btnCheckServer.IsEnabled = true;
            }));
        }
    }

    private void ConfigureClientSettings(string dest, string ip) {
        string settingsFile = Path.Combine(dest, @"Data\Platform\Plugins\skymp5-client-settings.txt");
        try {
            string dir = Path.GetDirectoryName(settingsFile);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = string.Format(
"{{\n  \"gameData\": {{\n    \"profileId\": {0}\n  }},\n  \"master\": \"\",\n  \"server-ip\": \"{1}\",\n  \"server-port\": 7777,\n  \"server-http-url\": \"http://{1}:3000\",\n  \"server-info-ignore\": true,\n  \"server-master-key\": null,\n  \"ignoreLoadOrderMismatch\": false\n}}",
                assignedProfileId, ip);
            File.WriteAllText(settingsFile, json, new UTF8Encoding(false));
        } catch { }
    }

    private void ConfigureDisplayTweaks(string dest) {
        string iniFile = Path.Combine(dest, @"Data\SKSE\Plugins\SSEDisplayTweaks.ini");
        if (File.Exists(iniFile)) {
            string content = File.ReadAllText(iniFile);
            content = Regex.Replace(content, @"(?m)^#?\s*Fullscreen\s*=.*$", "Fullscreen=false");
            content = Regex.Replace(content, @"(?m)^#?\s*Borderless\s*=.*$", "Borderless=true");
            content = Regex.Replace(content, @"(?m)^#?\s*Resolution\s*=.*$", "Resolution=1280x720");
            File.WriteAllText(iniFile, content, new UTF8Encoding(false));
        }
    }

    private void LaunchGame() {
        string dest = txtDestPath.Text.Trim();
        string loader = Path.Combine(dest, "skse64_loader.exe");

        if (!File.Exists(loader)) {
            MessageBox.Show("skse64_loader.exe bulunamadi. Lutfen once kurulumu yapin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (updateRequired) {
            MessageBox.Show("Sunucuda yeni bir mod guncellemesi var! Sunucuya girebilmek icin once guncellemeyi indirmelisiniz.", "Guncelleme Zorunlu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        EnsurePlayerIdentity();
        ConfigureClientSettings(dest, txtServerIp.Text.Trim());

        lblStatusText.Text = "Oyun baslatiliyor...";
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = loader;
        psi.WorkingDirectory = dest;
        Process.Start(psi);

        // Sesli Sohbet Baslat
        if (chkEnableVoice != null && chkEnableVoice.IsChecked == true) {
            try {
                if (voiceManager != null) {
                    voiceManager.Stop();
                }
                string srvIp = txtServerIp.Text.Trim();
                if (srvIp == "127.0.0.1" || srvIp.Equals("localhost", StringComparison.OrdinalIgnoreCase)) {
                    srvIp = "127.0.0.1";
                }
                voiceManager = new VoiceManager(srvIp, 3001, assignedProfileId, dest);
                voiceManager.OnTalkStateChanged = (talking, mode) => {
                    window.Dispatcher.Invoke(new Action(() => {
                        string modeStr = mode == VoiceMode.Whisper ? "Fisilti" : (mode == VoiceMode.Shout ? "Bagirma" : "Normal");
                        if (talking) {
                            lblMicState.Text = string.Format("Konusuluyor... ({0})", modeStr);
                            lblMicState.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                        } else {
                            lblMicState.Text = string.Format("Hazir ({0} - F8)", modeStr);
                            lblMicState.Foreground = new SolidColorBrush(Color.FromRgb(141, 147, 168));
                        }
                    }));
                };
                voiceManager.OnVoiceModeChanged = (mode) => {
                    window.Dispatcher.Invoke(new Action(() => {
                        string modeStr = mode == VoiceMode.Whisper ? "Fisilti" : (mode == VoiceMode.Shout ? "Bagirma" : "Normal");
                        lblMicState.Text = string.Format("Hazir ({0} - F8)", modeStr);
                    }));
                };
                voiceManager.Start();
            } catch (Exception ex) {
                Console.WriteLine("VoiceManager start hatasi: " + ex.Message);
            }
        }

        lblStatusText.Text = "Skyrim SE calisiyor. Keyifli oyunlar! [V tusu ile konusabilirsiniz]";
    }

    private void UpdateProgress(int percent, string message) {
        window.Dispatcher.Invoke(new Action(() => {
            progressBar.Value = percent;
            lblStatusText.Text = message;
        }));
    }

    private static int ExtractJsonInt(string json, string key) {
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)");
        int val;
        if (m.Success && int.TryParse(m.Groups[1].Value, out val)) {
            return val;
        }
        return 0;
    }

    private static long ExtractJsonLong(string json, string key) {
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)");
        long val;
        if (m.Success && long.TryParse(m.Groups[1].Value, out val)) {
            return val;
        }
        return 0;
    }

    private static string ExtractJsonString(string json, string key) {
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
        if (m.Success) {
            return m.Groups[1].Value;
        }
        return "";
    }

    private static bool CopyFileIfDifferent(string src, string dest) {
        if (!File.Exists(src)) return false;
        if (File.Exists(dest)) {
            try {
                FileInfo sInfo = new FileInfo(src);
                FileInfo dInfo = new FileInfo(dest);
                if (dInfo.Length == sInfo.Length && dInfo.Length > 0) {
                    return true;
                }
            } catch { }
        }
        string dir = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }
        File.Copy(src, dest, true);
        return false;
    }

    private static bool ExtractEntryIfDifferent(ZipArchiveEntry entry, string destPath) {
        if (File.Exists(destPath)) {
            try {
                FileInfo fi = new FileInfo(destPath);
                if (fi.Length == entry.Length && fi.Length > 0) {
                    return true;
                }
            } catch { }
        }
        string dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }
        entry.ExtractToFile(destPath, true);
        return false;
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir) {
        Directory.CreateDirectory(targetDir);
        foreach (string file in Directory.GetFiles(sourceDir)) {
            string dest = Path.Combine(targetDir, Path.GetFileName(file));
            CopyFileIfDifferent(file, dest);
        }
        foreach (string dir in Directory.GetDirectories(sourceDir)) {
            string dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectoryRecursive(dir, dest);
        }
    }
}

public enum VoiceMode {
    Whisper = 0,
    Normal = 1,
    Shout = 2
}

public class VoiceManager {
    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEX {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEHDR {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    public delegate void WaveInCallback(IntPtr hwi, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);
    public delegate void WaveOutCallback(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("winmm.dll")]
    public static extern int waveInOpen(out IntPtr phwi, int uDeviceID, ref WAVEFORMATEX lpFormat, WaveInCallback dwCallback, IntPtr dwInstance, int fdwOpen);
    [DllImport("winmm.dll")]
    public static extern int waveInPrepareHeader(IntPtr hwi, IntPtr pwh, int cbwh);
    [DllImport("winmm.dll")]
    public static extern int waveInAddBuffer(IntPtr hwi, IntPtr pwh, int cbwh);
    [DllImport("winmm.dll")]
    public static extern int waveInStart(IntPtr hwi);
    [DllImport("winmm.dll")]
    public static extern int waveInStop(IntPtr hwi);
    [DllImport("winmm.dll")]
    public static extern int waveInClose(IntPtr hwi);

    [DllImport("winmm.dll")]
    public static extern int waveOutOpen(out IntPtr phwo, int uDeviceID, ref WAVEFORMATEX lpFormat, WaveOutCallback dwCallback, IntPtr dwInstance, int fdwOpen);
    [DllImport("winmm.dll")]
    public static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);
    [DllImport("winmm.dll")]
    public static extern int waveOutWrite(IntPtr hwo, IntPtr pwh, int cbwh);
    [DllImport("winmm.dll")]
    public static extern int waveOutClose(IntPtr hwo);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int VK_V = 0x56;
    private const int VK_F8 = 0x77;
    private const int CALLBACK_FUNCTION = 0x00030000;
    private const uint WIM_DATA = 0x3C0;
    private const uint WOM_DONE = 0x3BD;

    private string serverIp;
    private int serverPort;
    private int profileId;
    private string gameDirectory;
    private bool isRunning;
    private bool isTalking;
    private uint sequenceNumber;
    public VoiceMode CurrentMode = VoiceMode.Normal;

    private IntPtr hWaveIn = IntPtr.Zero;
    private IntPtr hWaveOut = IntPtr.Zero;
    private IntPtr hookId = IntPtr.Zero;
    private LowLevelKeyboardProc keyboardProc;
    private WaveInCallback waveInCb;
    private WaveOutCallback waveOutCb;
    private UdpClient udpClient;
    private Thread receiveThread;
    private Thread heartbeatThread;

    public Action<bool, VoiceMode> OnTalkStateChanged;
    public Action<VoiceMode> OnVoiceModeChanged;

    public VoiceManager(string serverIp, int serverPort, int profileId, string gameDirectory) {
        this.serverIp = serverIp;
        this.serverPort = serverPort;
        this.profileId = profileId;
        this.gameDirectory = gameDirectory;
        WriteVoiceState(false, (int)CurrentMode, 0.0f);
    }

    public void Start() {
        if (isRunning) return;
        isRunning = true;
        try {
            udpClient = new UdpClient();
            udpClient.Connect(serverIp, serverPort);

            InitAudioPlayback();
            InitAudioRecording();
            InitKeyboardHook();

            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            heartbeatThread = new Thread(HeartbeatLoop);
            heartbeatThread.IsBackground = true;
            heartbeatThread.Start();
        } catch (Exception ex) {
            Console.WriteLine("VoiceManager start error: " + ex.Message);
        }
    }

    public void Stop() {
        if (!isRunning) return;
        isRunning = false;
        try {
            WriteVoiceState(false, (int)CurrentMode, 0.0f);
            if (hookId != IntPtr.Zero) {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
            if (hWaveIn != IntPtr.Zero) {
                waveInStop(hWaveIn);
                waveInClose(hWaveIn);
                hWaveIn = IntPtr.Zero;
            }
            if (hWaveOut != IntPtr.Zero) {
                waveOutClose(hWaveOut);
                hWaveOut = IntPtr.Zero;
            }
            if (udpClient != null) {
                udpClient.Close();
                udpClient = null;
            }
        } catch { }
    }

    private void WriteVoiceState(bool talking, int mode, float level) {
        if (string.IsNullOrEmpty(gameDirectory)) return;
        try {
            string dir = Path.Combine(gameDirectory, @"Data\Platform");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "voice-state.json");
            string tmp = file + ".tmp";
            string json = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{{\"talking\":{0},\"mode\":{1},\"level\":{2:0.000},\"t\":{3}}}",
                talking ? "true" : "false",
                mode,
                level,
                Environment.TickCount
            );
            File.WriteAllText(tmp, json, Encoding.UTF8);
            if (File.Exists(file)) File.Delete(file);
            File.Move(tmp, file);
        } catch { }
    }

    private void InitKeyboardHook() {
        keyboardProc = HookCallback;
        IntPtr hMod = GetModuleHandle(null);
        hookId = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, hMod, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
        if (nCode >= 0) {
            int vkCode = Marshal.ReadInt32(lParam);
            int msg = wParam.ToInt32();
            if (vkCode == VK_V) {
                if (msg == 0x0100 || msg == 0x0104) { // WM_KEYDOWN
                    if (!isTalking) {
                        isTalking = true;
                        WriteVoiceState(true, (int)CurrentMode, 0.05f);
                        if (OnTalkStateChanged != null) OnTalkStateChanged(true, CurrentMode);
                    }
                } else if (msg == 0x0101 || msg == 0x0105) { // WM_KEYUP
                    if (isTalking) {
                        isTalking = false;
                        WriteVoiceState(false, (int)CurrentMode, 0.0f);
                        if (OnTalkStateChanged != null) OnTalkStateChanged(false, CurrentMode);
                    }
                }
            } else if (vkCode == VK_F8) {
                if (msg == 0x0100 || msg == 0x0104) { // WM_KEYDOWN
                    CurrentMode = (VoiceMode)(((int)CurrentMode + 1) % 3);
                    WriteVoiceState(isTalking, (int)CurrentMode, isTalking ? 0.05f : 0.0f);
                    if (OnVoiceModeChanged != null) OnVoiceModeChanged(CurrentMode);
                }
            }
        }
        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private void InitAudioRecording() {
        WAVEFORMATEX fmt = new WAVEFORMATEX();
        fmt.wFormatTag = 1; // PCM
        fmt.nChannels = 1;   // Mono
        fmt.nSamplesPerSec = 16000;
        fmt.wBitsPerSample = 16;
        fmt.nBlockAlign = (ushort)(fmt.nChannels * fmt.wBitsPerSample / 8);
        fmt.nAvgBytesPerSec = fmt.nSamplesPerSec * fmt.nBlockAlign;
        fmt.cbSize = 0;

        waveInCb = OnWaveIn;
        int res = waveInOpen(out hWaveIn, -1, ref fmt, waveInCb, IntPtr.Zero, CALLBACK_FUNCTION);
        if (res != 0 || hWaveIn == IntPtr.Zero) return;

        // Allocate 2 buffers of 2560 bytes (80 ms chunk)
        for (int i = 0; i < 2; i++) {
            AllocateAndAddInputBuffer(2560);
        }
        waveInStart(hWaveIn);
    }

    private void AllocateAndAddInputBuffer(int size) {
        IntPtr bufferPtr = Marshal.AllocHGlobal(size);
        WAVEHDR hdr = new WAVEHDR();
        hdr.lpData = bufferPtr;
        hdr.dwBufferLength = (uint)size;
        IntPtr hdrPtr = Marshal.AllocHGlobal(Marshal.SizeOf(hdr));
        Marshal.StructureToPtr(hdr, hdrPtr, false);
        waveInPrepareHeader(hWaveIn, hdrPtr, Marshal.SizeOf(hdr));
        waveInAddBuffer(hWaveIn, hdrPtr, Marshal.SizeOf(hdr));
    }

    private void OnWaveIn(IntPtr hwi, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2) {
        if (uMsg == WIM_DATA && isRunning) {
            IntPtr hdrPtr = dwParam1;
            WAVEHDR hdr = (WAVEHDR)Marshal.PtrToStructure(hdrPtr, typeof(WAVEHDR));
            if (hdr.dwBytesRecorded > 0 && isTalking && udpClient != null) {
                byte[] audio = new byte[hdr.dwBytesRecorded];
                Marshal.Copy(hdr.lpData, audio, 0, (int)hdr.dwBytesRecorded);

                // Calculate real RMS volume from 16-bit PCM audio samples
                int sampleCount = audio.Length / 2;
                double sumSquares = 0;
                for (int i = 0; i < sampleCount; i++) {
                    short s = BitConverter.ToInt16(audio, i * 2);
                    sumSquares += (double)s * s;
                }
                double rms = Math.Sqrt(sumSquares / (sampleCount > 0 ? sampleCount : 1));
                float level = (float)Math.Min(1.0, Math.Max(0.0, (rms - 150.0) / 4500.0));

                WriteVoiceState(true, (int)CurrentMode, level);

                // Send Packet: [0x02, (uint32)profileId, (uint32)seq, (byte)voiceMode, audio...]
                byte[] packet = new byte[10 + audio.Length];
                packet[0] = 0x02;
                Array.Copy(BitConverter.GetBytes((uint)profileId), 0, packet, 1, 4);
                Array.Copy(BitConverter.GetBytes(sequenceNumber++), 0, packet, 5, 4);
                packet[9] = (byte)CurrentMode;
                Array.Copy(audio, 0, packet, 10, audio.Length);

                try {
                    udpClient.Send(packet, packet.Length);
                } catch { }
            }
            if (hWaveIn != IntPtr.Zero && isRunning) {
                waveInAddBuffer(hWaveIn, hdrPtr, Marshal.SizeOf(typeof(WAVEHDR)));
            }
        }
    }

    private void InitAudioPlayback() {
        WAVEFORMATEX fmt = new WAVEFORMATEX();
        fmt.wFormatTag = 1; // PCM
        fmt.nChannels = 2;   // Stereo (for 3D spatial panning)
        fmt.nSamplesPerSec = 16000;
        fmt.wBitsPerSample = 16;
        fmt.nBlockAlign = (ushort)(fmt.nChannels * fmt.wBitsPerSample / 8);
        fmt.nAvgBytesPerSec = fmt.nSamplesPerSec * fmt.nBlockAlign;
        fmt.cbSize = 0;

        waveOutCb = OnWaveOut;
        waveOutOpen(out hWaveOut, -1, ref fmt, waveOutCb, IntPtr.Zero, CALLBACK_FUNCTION);
    }

    private void OnWaveOut(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2) {
        if (uMsg == WOM_DONE) {
            IntPtr hdrPtr = dwParam1;
            if (hdrPtr != IntPtr.Zero) {
                WAVEHDR hdr = (WAVEHDR)Marshal.PtrToStructure(hdrPtr, typeof(WAVEHDR));
                if (hdr.lpData != IntPtr.Zero) Marshal.FreeHGlobal(hdr.lpData);
                Marshal.FreeHGlobal(hdrPtr);
            }
        }
    }

    private void ReceiveLoop() {
        IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
        while (isRunning && udpClient != null) {
            try {
                byte[] packet = udpClient.Receive(ref remoteEp);
                if (packet == null || packet.Length < 17) continue;

                if (packet[0] == 0x03 && hWaveOut != IntPtr.Zero) { // Spatial Voice Packet
                    float dist = BitConverter.ToSingle(packet, 5);
                    float relX = BitConverter.ToSingle(packet, 9);
                    float relY = BitConverter.ToSingle(packet, 13);

                    if (dist > 2200.0f) continue;

                    float vol = 1.0f;
                    if (dist > 400.0f) vol = Math.Max(0.0f, (2200.0f - dist) / 1800.0f);

                    float pan = 0.0f;
                    if (Math.Abs(relX) > 0.1f || Math.Abs(relY) > 0.1f) {
                        pan = (float)Math.Sin(Math.Atan2(relX, relY));
                        if (pan < -0.85f) pan = -0.85f;
                        if (pan > 0.85f) pan = 0.85f;
                    }

                    float leftMult = vol * Math.Min(1.0f, 1.0f - pan);
                    float rightMult = vol * Math.Min(1.0f, 1.0f + pan);

                    int monoSamples = (packet.Length - 17) / 2;
                    int stereoBytesCount = monoSamples * 4;
                    byte[] stereoData = new byte[stereoBytesCount];

                    for (int i = 0; i < monoSamples; i++) {
                        short monoSample = BitConverter.ToInt16(packet, 17 + i * 2);
                        short leftSample = (short)(monoSample * leftMult);
                        short rightSample = (short)(monoSample * rightMult);

                        stereoData[i * 4] = (byte)(leftSample & 0xFF);
                        stereoData[i * 4 + 1] = (byte)((leftSample >> 8) & 0xFF);
                        stereoData[i * 4 + 2] = (byte)(rightSample & 0xFF);
                        stereoData[i * 4 + 3] = (byte)((rightSample >> 8) & 0xFF);
                    }

                    IntPtr pData = Marshal.AllocHGlobal(stereoBytesCount);
                    Marshal.Copy(stereoData, 0, pData, stereoBytesCount);

                    WAVEHDR hdr = new WAVEHDR();
                    hdr.lpData = pData;
                    hdr.dwBufferLength = (uint)stereoBytesCount;

                    IntPtr hdrPtr = Marshal.AllocHGlobal(Marshal.SizeOf(hdr));
                    Marshal.StructureToPtr(hdr, hdrPtr, false);

                    waveOutPrepareHeader(hWaveOut, hdrPtr, Marshal.SizeOf(hdr));
                    waveOutWrite(hWaveOut, hdrPtr, Marshal.SizeOf(hdr));
                }
            } catch { }
        }
    }

    private void HeartbeatLoop() {
        while (isRunning && udpClient != null) {
            try {
                byte[] hb = new byte[5];
                hb[0] = 0x01;
                Array.Copy(BitConverter.GetBytes((uint)profileId), 0, hb, 1, 4);
                udpClient.Send(hb, hb.Length);
            } catch { }
            Thread.Sleep(3000);
        }
    }
}
