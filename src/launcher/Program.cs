using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

    private async void CheckInstallStatus() {
        string dest = txtDestPath.Text.Trim();
        string loader = Path.Combine(dest, "skse64_loader.exe");
        string platform = Path.Combine(dest, @"Data\SKSE\Plugins\SkyrimPlatform.dll");

        if (File.Exists(loader) && File.Exists(platform)) {
            await CheckForServerUpdatesAsync();
        } else {
            btnMainAction.Content = "KURULUMU YAP VE OYNA";
            btnMainAction.Background = new SolidColorBrush(Color.FromRgb(212, 175, 55));
            btnReinstall.Visibility = Visibility.Collapsed;
            lblStatusText.Text = "Kurulum icin 'Kurulumu Yap' butonuna basin.";
            lblServerStatus.Text = "Kurulum bekleniyor...";
            lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(141, 147, 168));
            progressBar.Value = 0;
        }
    }

    private async Task CheckForServerUpdatesAsync() {
        string dest = txtDestPath.Text.Trim();
        string loader = Path.Combine(dest, "skse64_loader.exe");
        string serverIp = txtServerIp.Text.Trim();

        if (string.IsNullOrEmpty(serverIp)) return;

        if (!File.Exists(loader)) {
            lblServerStatus.Text = "Kurulum bekleniyor...";
            lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(141, 147, 168));
            return;
        }

        lblServerStatus.Text = "Sunucu kontrol ediliyor...";
        lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(141, 147, 168));

        string url = string.Format("http://{0}:3000/modpack-version.json", serverIp);

        try {
            string json = "";
            using (WebClient wc = new WebClient()) {
                wc.Headers.Add("User-Agent", "SkyMPTR-Launcher");
                var task = wc.DownloadStringTaskAsync(new Uri(url));
                if (await Task.WhenAny(task, Task.Delay(3500)) == task) {
                    json = await task;
                } else {
                    throw new Exception("Sunucu yanit vermedi (zaman asimi).");
                }
            }

            serverModpackVersion = ExtractJsonInt(json, "version");
            serverModpackHash = ExtractJsonString(json, "hash");
            serverModpackSizeBytes = ExtractJsonLong(json, "sizeBytes");

            lblServerStatus.Text = string.Format("Sunucu Aktif (Mod Paketi v{0})", serverModpackVersion);
            lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));

            // Read local installed version
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

            if (serverModpackVersion > localModpackVersion || (!string.IsNullOrEmpty(serverModpackHash) && serverModpackHash != localModpackHash)) {
                updateRequired = true;
                btnMainAction.Content = string.Format("GUNCELLEMEYI INDIR (v{0})", serverModpackVersion);
                btnMainAction.Background = new SolidColorBrush(Color.FromRgb(230, 126, 34)); // Turuncu
                lblStatusText.Text = string.Format("[!] Sunucuda yeni mod paketi var (v{0}). Oyuna girmeden once guncelleyin!", serverModpackVersion);
                lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
            } else {
                updateRequired = false;
                btnMainAction.Content = "OYUNA BASLA";
                btnMainAction.Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Yesil
                lblStatusText.Text = string.Format("[✓] Modlar sunucuyla esit ve guncel (v{0}).", localModpackVersion);
                lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                progressBar.Value = 100;
            }
            btnReinstall.Visibility = Visibility.Visible;
        } catch (Exception ex) {
            lblServerStatus.Text = "Sunucu Cevrimdisi";
            lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            lblStatusText.Text = string.Format("[!] Sunucuya baglanilamadi ({0}:3000): {1}", serverIp, ex.Message);
            lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));

            updateRequired = false;
            btnMainAction.Content = "OYUNA BASLA (Cevrimdisi)";
            btnMainAction.Background = new SolidColorBrush(Color.FromRgb(70, 75, 95));
            btnReinstall.Visibility = Visibility.Visible;
        }
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

        try {
            await Task.Run(() => PerformInstallation(source, dest, serverIp));
            CheckInstallStatus();
            MessageBox.Show("SkyMP TR kurulumu basariyla tamamlandi!\n'OYUNA BASLA' butonuna basarak sunucuya baglanabilirsiniz.", "Kurulum Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        } catch (Exception ex) {
            MessageBox.Show("Kurulum sirasinda bir hata olustu:\n" + ex.Message, "Kurulum Hatasi", MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatusText.Text = "Hata: " + ex.Message;
            lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
        } finally {
            isInstalling = false;
            btnMainAction.IsEnabled = true;
            btnBrowseSource.IsEnabled = true;
            btnBrowseDest.IsEnabled = true;
            btnReinstall.IsEnabled = true;
            btnResetProfile.IsEnabled = true;
            btnCheckServer.IsEnabled = true;
        }
    }

    private void PerformInstallation(string source, string dest, string serverIp) {
        UpdateProgress(5, "Hedef dizin hazirlaniyor...");
        Directory.CreateDirectory(dest);
        string destData = Path.Combine(dest, "Data");
        Directory.CreateDirectory(destData);

        // 1. Temel oyun bilesenleri
        UpdateProgress(10, "Temel oyun master dosyalari (ESM) kopyalaniyor...");
        string sourceData = Path.Combine(source, "Data");
        string[] masters = new string[] { "Skyrim.esm", "Update.esm", "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm" };
        foreach (string m in masters) {
            string srcFile = Path.Combine(sourceData, m);
            if (File.Exists(srcFile)) {
                File.Copy(srcFile, Path.Combine(destData, m), true);
            }
        }

        UpdateProgress(25, "Temel oyun grafik ve ses paketleri (BSA) kopyalaniyor...");
        string[] bsaFiles = Directory.GetFiles(sourceData, "*.bsa");
        int count = 0;
        foreach (string bsa in bsaFiles) {
            string name = Path.GetFileName(bsa);
            if (name.StartsWith("Skyrim", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Dawnguard", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("HearthFires", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Dragonborn", StringComparison.OrdinalIgnoreCase)) {
                File.Copy(bsa, Path.Combine(destData, name), true);
                count++;
            }
        }

        UpdateProgress(50, "Calistirici ve kutuphaneler hazirlaniyor...");
        string[] binaries = new string[] { "SkyrimSELauncher.exe", "steam_api64.dll", "Skyrim_Default.ini" };
        foreach (string b in binaries) {
            string srcFile = Path.Combine(source, b);
            if (File.Exists(srcFile)) {
                File.Copy(srcFile, Path.Combine(dest, b), true);
            }
        }
        File.WriteAllText(Path.Combine(dest, "Skyrim.ccc"), "");

        // 2. Surum Denetimi ve Downgrade
        string exeSrc = Path.Combine(source, "SkyrimSE.exe");
        string exeDest = Path.Combine(dest, "SkyrimSE.exe");
        string binkSrc = Path.Combine(source, "bink2w64.dll");
        string binkDest = Path.Combine(dest, "bink2w64.dll");

        if (isExactVersion) {
            UpdateProgress(60, "Uyumlu 1.6.1170 calistirici kopyalaniyor...");
            File.Copy(exeSrc, exeDest, true);
            if (File.Exists(binkSrc)) File.Copy(binkSrc, binkDest, true);
        } else {
            UpdateProgress(60, "Oyun 1.6.1170 surumune downgrade ediliyor...");
            ExtractDowngradeBinaries(dest);
        }

        // 3. Modlar, SKSE ve SkyMP Client Kurulumu
        UpdateProgress(75, "SKSE, Engine Fixes, Display Tweaks ve SkyMP paketleri aciliyor...");
        ExtractCompanionData(dest);

        // 4. Ayarlarin Yapilandirilmasi
        UpdateProgress(90, "Sunucu ve ekran ayarlari yapilandiriliyor...");
        EnsurePlayerIdentity();
        ConfigureClientSettings(dest, serverIp);
        ConfigureDisplayTweaks(dest);

        UpdateProgress(100, "Kurulum tamamlandi!");
    }

    private void ExtractDowngradeBinaries(string dest) {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string bundledExe = Path.Combine(appDir, @"downgrade\SkyrimSE.exe");
        string bundledBink = Path.Combine(appDir, @"downgrade\bink2w64.dll");

        if (File.Exists(bundledExe)) {
            File.Copy(bundledExe, Path.Combine(dest, "SkyrimSE.exe"), true);
            if (File.Exists(bundledBink)) File.Copy(bundledBink, Path.Combine(dest, "bink2w64.dll"), true);
        } else {
            string zipPath = Path.Combine(appDir, "SkyMPTR-Data.zip");
            if (File.Exists(zipPath)) {
                using (ZipArchive zip = ZipFile.OpenRead(zipPath)) {
                    foreach (ZipArchiveEntry entry in zip.Entries) {
                        if (entry.FullName.Equals("SkyrimSE.exe", StringComparison.OrdinalIgnoreCase) ||
                            entry.FullName.Equals("bink2w64.dll", StringComparison.OrdinalIgnoreCase)) {
                            entry.ExtractToFile(Path.Combine(dest, entry.Name), true);
                        }
                    }
                }
            }
        }
    }

    private void ExtractCompanionData(string dest) {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string zipPath = Path.Combine(appDir, "SkyMPTR-Data.zip");

        if (File.Exists(zipPath)) {
            using (ZipArchive zip = ZipFile.OpenRead(zipPath)) {
                foreach (ZipArchiveEntry entry in zip.Entries) {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    string destPath = Path.Combine(dest, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    entry.ExtractToFile(destPath, true);
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
        string tempZip = Path.Combine(dest, "modpack_update.tmp.zip");

        try {
            UpdateProgress(0, "Guncelleme paketi indiriliyor...");
            if (File.Exists(tempZip)) File.Delete(tempZip);

            using (WebClient wc = new WebClient()) {
                wc.Headers.Add("User-Agent", "SkyMPTR-Launcher");
                wc.DownloadProgressChanged += (s, e) => {
                    double mbRec = e.BytesReceived / 1048576.0;
                    double mbTotal = e.TotalBytesToReceive > 0 ? e.TotalBytesToReceive / 1048576.0 : (serverModpackSizeBytes / 1048576.0);
                    UpdateProgress(e.ProgressPercentage, string.Format("Mod paketi indiriliyor: %{0} ({1:0.0} / {2:0.0} MB)", e.ProgressPercentage, mbRec, mbTotal));
                };
                await wc.DownloadFileTaskAsync(new Uri(downloadUrl), tempZip);
            }

            UpdateProgress(90, "Guncelleme dosyalari cikartiliyor...");
            await Task.Run(() => {
                using (ZipArchive zip = ZipFile.OpenRead(tempZip)) {
                    foreach (ZipArchiveEntry entry in zip.Entries) {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string destPath = Path.Combine(dest, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                        entry.ExtractToFile(destPath, true);
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

            UpdateProgress(100, string.Format("Guncelleme tamamlandi! Mod paketi v{0} aktif.", serverModpackVersion));
            updateRequired = false;
            btnMainAction.Content = "OYUNA BASLA";
            btnMainAction.Background = new SolidColorBrush(Color.FromRgb(39, 174, 96));
            lblServerStatus.Text = string.Format("Sunucu Aktif (Mod Paketi v{0})", serverModpackVersion);
            lblServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            lblStatusText.Text = string.Format("[✓] Modlar sunucuyla esit ve guncel (v{0}).", serverModpackVersion);
            lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));

            MessageBox.Show("Mod paketi guncellemesi basariyla tamamlandi!\n'OYUNA BASLA' butonuna basarak sunucuya katilabilirsiniz.", "Guncelleme Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        } catch (Exception ex) {
            MessageBox.Show("Guncelleme indirilirken hata olustu:\n" + ex.Message, "Guncelleme Hatasi", MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatusText.Text = "Guncelleme hatasi: " + ex.Message;
            lblStatusText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
        } finally {
            isInstalling = false;
            btnMainAction.IsEnabled = true;
            btnBrowseSource.IsEnabled = true;
            btnBrowseDest.IsEnabled = true;
            btnReinstall.IsEnabled = true;
            btnResetProfile.IsEnabled = true;
            btnCheckServer.IsEnabled = true;
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

        lblStatusText.Text = "Skyrim SE calisiyor. Keyifli oyunlar!";
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

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir) {
        Directory.CreateDirectory(targetDir);
        foreach (string file in Directory.GetFiles(sourceDir)) {
            string dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }
        foreach (string dir in Directory.GetDirectories(sourceDir)) {
            string dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectoryRecursive(dir, dest);
        }
    }
}
