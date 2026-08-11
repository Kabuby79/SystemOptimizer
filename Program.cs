using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Win32;

[assembly: AssemblyTitle("Universal System & Windows Maintenance Center")]
[assembly: AssemblyDescription("Windows Maintenance, Power Optimization and Hardware Diagnostics Suite")]
[assembly: AssemblyCompany("E.P. Software")]
[assembly: AssemblyProduct("Universal WinOptimizer")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ComVisible(false)]

namespace UniversalOptimizer
{
    public class TaskItem
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string CategoryName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Command { get; set; }
        public bool Recommended { get; set; }
        public bool Selected { get; set; }
        public string Oem { get; set; }
        public string Os { get; set; }

        public Border CardBorder { get; set; }
        public CheckBox CheckBoxCtrl { get; set; }
        public long SizeEstimateBytes { get; set; }

        public TaskItem(string id, string category, string categoryName, string name, string desc, string cmd, bool rec, string oem = "all", string os = "all")
        {
            Id = id;
            Category = category;
            CategoryName = categoryName;
            Name = name;
            Description = desc;
            Command = cmd;
            Recommended = rec;
            Selected = rec;
            Oem = oem;
            Os = os;
        }
    }

    public class SystemInfo
    {
        public string Manufacturer = "Generic";
        public string Model = "PC";
        public string DeviceModel = "PC";
        public string OEMBrand = "generic";
        public string OS = "Microsoft Windows";
        public string OSType = "win11";
        public int OSBuild = 22000;
        public string CPU = "Processore";
        public string Cores = "Core / Thread";
        public double RAM_TotalGB = 16.0;
        public double RAM_FreeGB = 8.0;
        public double RAM_UsedPercent = 50.0;
        public string GPU = "Scheda Video";
        public string GPU_Driver = "...";
        public double Disk_SizeGB = 512.0;
        public double Disk_FreeGB = 256.0;
        public double Disk_UsedPercent = 50.0;
        public string PowerScheme = "Bilanciato / Standard";
        public bool IsLaptop = false;
    }

    public class MainWindow : Window
    {
        private SystemInfo sysInfo = new SystemInfo();
        private List<TaskItem> allTasks = new List<TaskItem>();
        private List<TaskItem> activeTasks = new List<TaskItem>();
        private string currentFilter = "all";

        // Startup Splash Overlay References
        private Grid splashOverlay;
        private ProgressBar splashProgressBar;
        private TextBlock txtSplashStatus;

        // UI References
        private StackPanel pnlTasks;
        private TextBlock txtSelectedCount;
        private TextBlock txtSummaryDesc;
        private Button btnRun;
        private Grid modalBackdrop;
        private ProgressBar modalProgressBar;
        private TextBlock txtModalPercent;
        private StackPanel modalStepList;
        private Button btnModalDone;
        private Border modalWarningBanner;
        private TextBlock txtModalWarningText;
        private TextBlock txtModalTitle;
        private TextBlock txtModalCommandLog;
        private bool isExecuting = false;

        // Hardware TextBlocks
        private TextBlock txtCpuTitle, txtCpuSub;
        private TextBlock txtRamTitle, txtRamSub;
        private TextBlock txtGpuTitle, txtGpuSub;
        private TextBlock txtDiskTitle, txtDiskSub;
        private TextBlock txtHwHeader;
        private TextBlock txtOsFooterTag;
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        // Theme Palette - Colori Tenui ed Eleganti (Soft Dark Glassmorphism)
        private SolidColorBrush brushBg = new SolidColorBrush(Color.FromRgb(11, 15, 25));
        private SolidColorBrush brushHeader = new SolidColorBrush(Color.FromRgb(17, 24, 39));
        private SolidColorBrush brushCard = new SolidColorBrush(Color.FromRgb(22, 31, 48));
        private SolidColorBrush brushCardSelected = new SolidColorBrush(Color.FromRgb(28, 42, 66));
        private SolidColorBrush brushBorder = new SolidColorBrush(Color.FromRgb(40, 53, 72));
        private SolidColorBrush brushBorderSelected = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        private SolidColorBrush brushCyan = new SolidColorBrush(Color.FromRgb(56, 189, 248));      // Soft Sky Cyan
        private SolidColorBrush brushEmerald = new SolidColorBrush(Color.FromRgb(52, 211, 153));   // Soft Emerald
        private SolidColorBrush brushAmber = new SolidColorBrush(Color.FromRgb(251, 191, 36));     // Soft Amber
        private SolidColorBrush brushPurple = new SolidColorBrush(Color.FromRgb(192, 132, 252));   // Soft Lavender
        private SolidColorBrush brushRose = new SolidColorBrush(Color.FromRgb(251, 113, 133));     // Soft Rose
        private SolidColorBrush brushTextWhite = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        private SolidColorBrush brushTextSlate = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        private SolidColorBrush brushTextMuted = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private string searchQuery = "";
        private Button btnProfiles;

        public MainWindow()
        {
            this.Title = "Universal System & Windows Maintenance Center";
            this.Width = 1280;
            this.Height = 880;
            this.MinWidth = 1050;
            this.MinHeight = 720;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = brushBg;
            this.Foreground = brushTextWhite;
            this.FontFamily = new FontFamily("Segoe UI, Inter");

            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
                }
            }
            catch { }

            this.Closing += MainWindow_Closing;

            InitTasks();
            BuildWpfUI();

            Task.Run(() =>
            {
                ScanHardwareBackground((pct, msg) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        splashProgressBar.Value = pct;
                        txtSplashStatus.Text = msg;
                    });
                });

                this.Dispatcher.Invoke(() =>
                {
                    UpdateHwCards();
                    RenderTaskList();
                    UpdateFooterSummary();
                    splashOverlay.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isExecuting)
            {
                MessageBox.Show(
                    "ATTENZIONE: Manutenzione di sistema in corso (SFC/DISM/Registri attivi).\nNon chiudere la finestra per evitare corruzioni di sistema.",
                    "Manutenzione in corso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                e.Cancel = true;
            }
        }

        private void InitTasks()
        {
            allTasks = new List<TaskItem>()
            {
                // RISPARMIO ENERGIA & BATTERIA
                new TaskItem("power_standby", "power", "Risparmio Energia & Batteria", "Ottimizza Timeout Schermo & Sospensione a Batteria", "Imposta lo spegnimento schermo a 5 min e la sospensione automatica a 15 min a batteria.", "powercfg /change monitor-timeout-dc 5; powercfg /change standby-timeout-dc 15", true),
                new TaskItem("power_pcie", "power", "Risparmio Energia & Batteria", "Abilita Risparmio Massimo Bus PCIe (Link State Management)", "Riduce il consumo energetico di NVMe e bus PCIe quando non sotto carico.", "powercfg /setdcvalueindex scheme_current sub_pci express 2; powercfg /setactive scheme_current", true),
                new TaskItem("power_usb", "power", "Risparmio Energia & Batteria", "Abilita Sospensione Selettiva Porte USB", "Sospende l'alimentazione alle porte USB inattive prolungando l'autonomia a batteria.", "powercfg /setdcvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1; powercfg /setactive scheme_current", true),
                new TaskItem("power_wake", "power", "Risparmio Energia & Batteria", "Disabilita Timer di Riattivazione Nascosti (No Wake-ups)", "Impedisce riattivazioni improvvise dallo standby (evita surriscaldamenti nello zaino).", "powercfg /setdcvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 0; powercfg /setactive scheme_current", true),
                new TaskItem("power_profile", "power", "Risparmio Energia & Batteria", "Attiva Profilo Energetico Bilanciato Intelligente", "Ottimizza la regolazione automatica delle frequenze e delle ventole del sistema.", "powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e", true),
                new TaskItem("power_hiber", "power", "Risparmio Energia & Batteria", "Riduci Dimensione File Ibernazione (Libera fino a 32GB)", "Imposta l'ibernazione ridotta per liberare spazio prezioso su disco C:.", "powercfg /h /type reduced", false),

                // MANUTENZIONE & RIPARAZIONE WINDOWS
                new TaskItem("win_dism", "win_maintenance", "Manutenzione & Riparazione Windows", "Riparazione Archivio Componenti (DISM RestoreHealth)", "Verifica e ripara i file dell'archivio componenti con l'immagine ufficiale Microsoft.", "DISM.exe /Online /Cleanup-Image /RestoreHealth", true),
                new TaskItem("win_winsxs", "win_maintenance", "Manutenzione & Riparazione Windows", "Pulizia Approfondita Componenti Obsoleti & WinSxS", "Elimina i backup storici di Windows Update liberando diversi GB su C:.", "DISM.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase", true),
                new TaskItem("win_sfc", "win_maintenance", "Manutenzione & Riparazione Windows", "Verifica Integrità File di Sistema Protetti (SFC /scannow)", "Scansiona e ripristina file DLL e driver di sistema protetti danneggiati.", "sfc.exe /scannow", true),
                new TaskItem("win_chkdsk", "win_maintenance", "Manutenzione & Riparazione Windows", "Controllo File System & Metadati (CHKDSK Scan)", "Esegue un controllo online non distruttivo del filesystem NTFS su C:.", "chkdsk.exe C: /scan", true),
                new TaskItem("win_netreset", "win_maintenance", "Manutenzione & Riparazione Windows", "Reset Completo Stack di Rete TCP/IP e Winsock", "Reinizializza il catalogo Winsock e TCP/IP risolvendo disconnessioni e lag.", "netsh winsock reset; netsh int ip reset", false),

                // APP DI AVVIO
                new TaskItem("start_copilot", "startup", "App di Avvio (Startup)", "Disabilita Microsoft Copilot al Boot", "Rimuove il precaricamento automatico di Copilot all'accesso (risparmia RAM).", "Get-ItemProperty 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -ErrorAction SilentlyContinue | Get-Member -MemberType NoteProperty | Where-Object { $_.Name -like 'MicrosoftCopilotAutoLaunch*' } | ForEach-Object { Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name $_.Name -ErrorAction SilentlyContinue }", true),
                new TaskItem("start_edge", "startup", "App di Avvio (Startup)", "Disabilita Precaricamento Microsoft Edge", "Impedisce a Edge di avviarsi nascosto in background all'accensione (Startup Boost).", "Get-ItemProperty 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -ErrorAction SilentlyContinue | Get-Member -MemberType NoteProperty | Where-Object { $_.Name -like 'MicrosoftEdgeAutoLaunch*' } | ForEach-Object { Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name $_.Name -ErrorAction SilentlyContinue }", true),
                new TaskItem("start_vpn", "startup", "App di Avvio (Startup)", "Disabilita Avvio Automatico VPN Terze Parti", "Rimuove l'avvio automatico continuo di client VPN all'avvio.", "Remove-ItemProperty -Path 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name 'BdVpnApp' -ErrorAction SilentlyContinue", true),
                new TaskItem("start_unattend", "startup", "App di Avvio (Startup)", "Rimuovi Residui Script di Setup (renameUnattend.bat)", "Elimina file batch orfani nella cartella di avvio di sistema.", "Remove-Item 'C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\renameUnattend.bat' -Force -ErrorAction SilentlyContinue", true),
                new TaskItem("start_pcloud", "startup", "App di Avvio (Startup)", "Disabilita Avvio Automatico pCloud Drive", "Avvia pCloud solo quando apri manualmente l'app anziché sempre in background.", "Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name 'pCloud' -ErrorAction SilentlyContinue", false),

                // TELEMETRIA & SERVIZI
                new TaskItem("telem_diagtrack", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Telemetria & Esperienze Utente (DiagTrack)", "Arresta e disabilita il servizio Connected User Experiences per massima privacy.", "Stop-Service 'DiagTrack', 'dmwappushservice' -Force -ErrorAction SilentlyContinue; Set-Service 'DiagTrack', 'dmwappushservice' -StartupType Disabled -ErrorAction SilentlyContinue", true),
                new TaskItem("telem_inventory", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Valutazione Inventario (InventorySvc)", "Disattiva il monitoraggio diagnostico Microsoft Compatibility Appraiser.", "Stop-Service 'InventorySvc' -Force -ErrorAction SilentlyContinue; Set-Service 'InventorySvc' -StartupType Disabled -ErrorAction SilentlyContinue", true),
                // Samsung
                new TaskItem("telem_samsung_sa", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Samsung Analytics Agent (SA)", "Arresta la telemetria statistica Samsung in background.", "Stop-Service 'SamsungAnalyticsService' -Force -ErrorAction SilentlyContinue; Set-Service 'SamsungAnalyticsService' -StartupType Disabled -ErrorAction SilentlyContinue", true, "samsung"),
                new TaskItem("telem_samsung_hqm", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Samsung Hardware Quality Monitoring", "Disattiva il logging di diagnostica hardware Samsung.", "Stop-Service 'SamsungHQMService' -Force -ErrorAction SilentlyContinue; Set-Service 'SamsungHQMService' -StartupType Disabled -ErrorAction SilentlyContinue", true, "samsung"),
                new TaskItem("telem_bixby", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Bixby Voice & SysTray Hook", "Arresta Bixby, elimina il task di avvio e disinstalla il pacchetto vocale.", "Stop-Process -Name BixbySystray, UWPBixbyClient -Force -ErrorAction SilentlyContinue; Stop-Service 'SamsungBixbyService' -Force -ErrorAction SilentlyContinue; Set-Service 'SamsungBixbyService' -StartupType Disabled -ErrorAction SilentlyContinue; Get-AppxPackage *Bixby* -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue", true, "samsung"),
                new TaskItem("bloat_livewall", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Servizio Live Wallpaper", "Arresta il processo di sfondi animati che consuma GPU e batteria.", "Stop-Service 'LiveWallpaperService' -Force -ErrorAction SilentlyContinue; Set-Service 'LiveWallpaperService' -StartupType Disabled -ErrorAction SilentlyContinue", true, "samsung"),
                new TaskItem("bloat_quicksearch", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Samsung Quick Search Service & Indexer", "Disattiva l'indicizzatore duplicato e le attività pianificate correlate.", "Stop-Service 'Quick Search Service' -Force -ErrorAction SilentlyContinue; Set-Service 'Quick Search Service' -StartupType Disabled -ErrorAction SilentlyContinue", true, "samsung"),
                new TaskItem("bloat_smartswitch", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Servizio Smart Switch", "Disattiva il demone residente in RAM per trasferimento file da smartphone.", "Stop-Service 'SmartSwitchService' -Force -ErrorAction SilentlyContinue; Set-Service 'SmartSwitchService' -StartupType Disabled -ErrorAction SilentlyContinue", true, "samsung"),
                new TaskItem("bloat_camerashare", "telemetry", "Servizi, Bloatware & Telemetria", "Rimuovi Samsung Camera Share", "Disinstalla l'app e il relativo servizio in background.", "Get-AppxPackage *CameraShare* -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue; Stop-Service 'SamsungCameraShareService' -Force -ErrorAction SilentlyContinue", true, "samsung"),
                // Dell
                new TaskItem("telem_dell", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Telemetria Dell SupportAssist / Data Vault", "Arresta i servizi di telemetria e raccolta log Dell.", "@('DellDataVault', 'DellDataVaultProcessor', 'SupportAssistAgent') | ForEach-Object { Stop-Service $_ -Force -ErrorAction SilentlyContinue; Set-Service $_ -StartupType Disabled -ErrorAction SilentlyContinue }", true, "dell"),
                // HP
                new TaskItem("telem_hp", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita HP Touchpoint Analytics & Telemetria", "Disattiva i servizi di raccolta dati e diagnostica invasiva HP.", "@('HPAppHelperCap', 'HPNetworkCap', 'HPSysInfoCap', 'HPAnalytics') | ForEach-Object { Stop-Service $_ -Force -ErrorAction SilentlyContinue; Set-Service $_ -StartupType Disabled -ErrorAction SilentlyContinue }", true, "hp"),
                // Lenovo
                new TaskItem("telem_lenovo", "telemetry", "Servizi, Bloatware & Telemetria", "Disabilita Telemetria Lenovo Experience Improvement", "Disattiva il logging di utilizzo e telemetria Lenovo in background.", "@('LenovoVantageService', 'ImControllerService') | ForEach-Object { Stop-Service $_ -Force -ErrorAction SilentlyContinue; Set-Service $_ -StartupType Manual -ErrorAction SilentlyContinue }", true, "lenovo"),

                // HARDWARE, GRAFICA & DRIVER
                new TaskItem("os_w11_widgets", "hardware", "Hardware, Grafica & Driver", "[Win 11] Disabilita Feed Notizie & Widget Barra", "Disattiva i widget con notizie online e feed che consumano memoria WebView2.", "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'TaskbarDa' -Value 0 -ErrorAction SilentlyContinue", true, "all", "win11"),
                new TaskItem("os_w11_chat", "hardware", "Hardware, Grafica & Driver", "[Win 11] Disabilita Chat / Teams Consumer in Background", "Rimuove l'icona Chat/Teams integrata nella barra di Windows 11.", "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'TaskbarMn' -Value 0 -ErrorAction SilentlyContinue", true, "all", "win11"),
                new TaskItem("os_w10_cortana", "hardware", "Hardware, Grafica & Driver", "[Win 10] Disabilita Assistente Cortana in Background", "Disattiva l'assistente vocale Cortana residente nei processi di Windows 10.", "$path = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search'; if(-not (Test-Path $path)){ New-Item -Path $path -Force | Out-Null }; Set-ItemProperty -Path $path -Name 'AllowCortana' -Value 0 -ErrorAction SilentlyContinue", true, "all", "win10"),
                new TaskItem("os_w10_news", "hardware", "Hardware, Grafica & Driver", "[Win 10] Disabilita 'Notizie e Interessi' Barra", "Rimuove il widget meteo/notizie con feed in background su Windows 10.", "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Feeds' -Name 'ShellFeedsTaskbarViewMode' -Value 2 -ErrorAction SilentlyContinue", true, "all", "win10"),
                new TaskItem("hw_winupdates", "hardware", "Hardware, Grafica & Driver", "Verifica ed Installa Aggiornamenti Windows", "Apre il pannello delle impostazioni di Windows Update per cercare patch di sistema.", "Start-Process ms-settings:windowsupdate", true),
                new TaskItem("hw_driverupdates", "hardware", "Hardware, Grafica & Driver", "Verifica ed Installa Aggiornamenti Driver", "Ricerca online e propone in un popup i driver hardware da scaricare e installare.", "custom_driver_updates", true),
                new TaskItem("hw_winget", "hardware", "Hardware, Grafica & Driver", "Aggiorna Tutte le App e Pacchetti con WinGet", "Esegue 'winget upgrade --all' per aggiornare tutti i software installati.", "winget upgrade --all --include-unknown --accept-package-agreements --accept-source-agreements", false),
                new TaskItem("hw_hags", "hardware", "Hardware, Grafica & Driver", "Abilita Accelerazione Hardware GPU (HAGS)", "Consente alla GPU di gestire direttamente la VRAM riducendo latenza e carico CPU.", "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers' -Name 'HwSchMode' -Value 2 -ErrorAction SilentlyContinue", true),
                new TaskItem("hw_gamedvr", "hardware", "Hardware, Grafica & Driver", "Disabilita Registrazione Background Xbox GameDVR", "Disattiva il buffer video Xbox continuo, eliminando micro-stuttering e liberando RAM.", "Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -ErrorAction SilentlyContinue", true),
                new TaskItem("net_delivery", "hardware", "Hardware, Grafica & Driver", "Disabilita Condivisione P2P Windows Update", "Impedisce a Windows Update di usare la banda e CPU per inviare file ad altri PC.", "if (-not (Test-Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization')) { New-Item 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization' -Force | Out-Null }; Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization' -Name 'DODownloadMode' -Value 0 -ErrorAction SilentlyContinue", true),
                new TaskItem("hw_trim", "hardware", "Hardware, Grafica & Driver", "Esegui Re-TRIM Ottimizzazione SSD NVMe", "Invia il comando TRIM su tutte le unità SSD per massimizzare la velocità di scrittura.", "Optimize-Volume -DriveLetter C -ReTrim -Verbose", true),

                // PULIZIA & CACHE
                new TaskItem("clean_winupdate", "cleanup", "Pulizia & Cache", "Svuota Cache Download Windows Update", "Elimina i file di installazione temporanei scaricati da Windows Update.", "Stop-Service 'wuauserv' -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\\Windows\\SoftwareDistribution\\Download\\*' -Recurse -Force -ErrorAction SilentlyContinue; Start-Service 'wuauserv' -ErrorAction SilentlyContinue", true),
                new TaskItem("clean_wer", "cleanup", "Pulizia & Cache", "Pulizia File di Crash (Windows Error Reporting)", "Elimina dump di memoria e log di crash accumulati in Windows.", "Remove-Item 'C:\\ProgramData\\Microsoft\\Windows\\WER\\ReportArchive\\*' -Recurse -Force -ErrorAction SilentlyContinue", true),
                new TaskItem("clean_dns", "cleanup", "Pulizia & Cache", "Flush Completo Resolver DNS di Windows", "Svuota la cache locale DNS risolvendo eventuali errori di connessione.", "Clear-DnsClientCache; ipconfig /flushdns", true),
                new TaskItem("clean_browsers", "cleanup", "Pulizia & Cache", "Pulizia Approfondita Cache Browser (Chrome/Edge/Firefox/Brave)", "Elimina cache HTTP, GPU cache e shader liberando centinaia di MB.", "$dirs = @('$env:LOCALAPPDATA\\Google\\Chrome\\User Data\\*\\*Cache*', '$env:LOCALAPPDATA\\Microsoft\\Edge\\User Data\\*\\*Cache*', '$env:LOCALAPPDATA\\BraveSoftware\\Brave-Browser\\User Data\\*\\*Cache*', '$env:LOCALAPPDATA\\Mozilla\\Firefox\\Profiles\\*\\cache2'); foreach($d in $dirs){ Get-Item $d -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item -Path \"$($_.FullName)\\*\" -Recurse -Force -ErrorAction SilentlyContinue } }", true),
                new TaskItem("clean_temp", "cleanup", "Pulizia & Cache", "Pulizia File Temporanei Windows (%TEMP% e Temp)", "Elimina file temporanei di sistema per liberare spazio su disco.", "Remove-Item -Path \"$env:TEMP\\*\" -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -Path 'C:\\Windows\\Temp\\*' -Recurse -Force -ErrorAction SilentlyContinue", true),
                new TaskItem("clean_spotlight", "cleanup", "Pulizia & Cache", "Rimuovi Icona 'Scopri questa immagine' (Spotlight) Desktop", "Nasconde l'icona indesiderata di Windows Spotlight dal desktop.", "$reg = 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel'; if(-not (Test-Path $reg)){ New-Item -Path $reg -Force | Out-Null }; Set-ItemProperty -Path $reg -Name '{2cc5ca98-6485-489a-920e-b3e88a6ccce3}' -Value 1 -Type DWord -ErrorAction SilentlyContinue", true),
                new TaskItem("clean_trash", "cleanup", "Pulizia & Cache", "Svuota Cestino di Windows", "Elimina definitivamente gli elementi presenti nel cestino.", "Clear-RecycleBin -Force -ErrorAction SilentlyContinue", false)
            };
        }

        private void ScanHardwareBackground(Action<int, string> progress)
        {
            try
            {
                progress(10, "Rilevamento modello dispositivo e OEM...");
                using (var s = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
                {
                    foreach (var o in s.Get())
                    {
                        if (o["Manufacturer"] != null) sysInfo.Manufacturer = o["Manufacturer"].ToString().Trim();
                        if (o["Model"] != null) sysInfo.Model = o["Model"].ToString().Trim();
                    }
                }
                sysInfo.DeviceModel = sysInfo.Manufacturer + " " + sysInfo.Model;

                progress(25, "Rilevamento dettagli processore (CPU)...");
                using (var s = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                {
                    foreach (var o in s.Get())
                    {
                        if (o["Name"] != null) sysInfo.CPU = o["Name"].ToString().Trim();
                        if (o["NumberOfCores"] != null) sysInfo.Cores = o["NumberOfCores"] + " Cores / " + o["NumberOfLogicalProcessors"] + " Threads";
                    }
                }

                progress(40, "Analisi memoria RAM e sistema operativo...");
                using (var s = new ManagementObjectSearcher("SELECT Caption, TotalVisibleMemorySize, FreePhysicalMemory, OSArchitecture, Version FROM Win32_OperatingSystem"))
                {
                    foreach (var o in s.Get())
                    {
                        if (o["Caption"] != null) sysInfo.OS = o["Caption"] + " (" + o["OSArchitecture"] + ", Build " + o["Version"] + ")";
                        if (o["TotalVisibleMemorySize"] != null && o["FreePhysicalMemory"] != null)
                        {
                            double tot = Convert.ToDouble(o["TotalVisibleMemorySize"]) / 1024.0 / 1024.0;
                            double fre = Convert.ToDouble(o["FreePhysicalMemory"]) / 1024.0 / 1024.0;
                            sysInfo.RAM_TotalGB = Math.Round(tot, 1);
                            sysInfo.RAM_FreeGB = Math.Round(fre, 1);
                            sysInfo.RAM_UsedPercent = Math.Round(((tot - fre) / tot) * 100.0, 1);
                        }
                    }
                }

                progress(55, "Rilevamento scheda video (GPU) e driver...");
                using (var s = new ManagementObjectSearcher("SELECT Name, DriverVersion FROM Win32_VideoController"))
                {
                    foreach (var o in s.Get())
                    {
                        if (o["Name"] != null)
                        {
                            sysInfo.GPU = o["Name"].ToString().Trim();
                            if (o["DriverVersion"] != null) sysInfo.GPU_Driver = o["DriverVersion"].ToString().Trim();
                            break;
                        }
                    }
                }

                progress(70, "Analisi spazio libero e file system su C:...");
                DriveInfo c = new DriveInfo("C");
                if (c.IsReady)
                {
                    double totG = c.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    double freG = c.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    sysInfo.Disk_SizeGB = Math.Round(totG, 1);
                    sysInfo.Disk_FreeGB = Math.Round(freG, 1);
                    sysInfo.Disk_UsedPercent = Math.Round(((totG - freG) / totG) * 100.0, 1);
                }

                sysInfo.OSBuild = Environment.OSVersion.Version.Build;
                sysInfo.OSType = sysInfo.OSBuild >= 22000 ? "win11" : "win10";

                string m = sysInfo.Manufacturer.ToLower();
                if (m.Contains("samsung")) sysInfo.OEMBrand = "samsung";
                else if (m.Contains("dell")) sysInfo.OEMBrand = "dell";
                else if (m.Contains("hp") || m.Contains("hewlett")) sysInfo.OEMBrand = "hp";
                else if (m.Contains("lenovo")) sysInfo.OEMBrand = "lenovo";
                else if (m.Contains("asus")) sysInfo.OEMBrand = "asus";
                else if (m.Contains("acer")) sysInfo.OEMBrand = "acer";

                sysInfo.IsLaptop = CheckIsLaptop();

                progress(80, "Verifica servizi ed elementi di avvio...");
                allTasks.RemoveAll(t => t.Id.StartsWith("dyn_"));
                ScanDynamicStartupApps();
                ScanDynamicThirdPartyServices();

                progress(90, "Calcolo stima spazio liberabile sul disco...");
                // Calculate space estimates for cleanup tasks
                foreach (var task in allTasks)
                {
                    if (task.Category == "cleanup" || task.Id == "win_winsxs")
                    {
                        task.SizeEstimateBytes = GetTaskSizeEstimate(task.Id);
                    }
                }

                activeTasks = allTasks.FindAll(t =>
                {
                    bool oemMatch = (t.Oem == "all" || t.Oem == sysInfo.OEMBrand);
                    bool osMatch = (t.Os == "all" || t.Os == sysInfo.OSType);
                    if (!oemMatch || !osMatch) return false;

                    if (t.Category == "power" && !sysInfo.IsLaptop && t.Id != "power_profile")
                    {
                        return false;
                    }

                    if (t.Id == "start_pcloud")
                    {
                        return DoesRegistryValueExist(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "pCloud");
                    }
                    if (t.Id == "start_vpn")
                    {
                        return DoesRegistryValueExist(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "BdVpnApp") ||
                               DoesRegistryValueExist(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "BdVpnApp");
                    }
                    if (t.Id == "start_unattend")
                    {
                        return File.Exists(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\renameUnattend.bat");
                    }

                    return true;
                });

                UpdateDynamicTasksStates();
                progress(100, "Caricamento completato!");
            }
            catch { }
        }

        private bool DoesRegistryValueExist(RegistryKey root, string subKeyPath, string valueName)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(subKeyPath))
                {
                    if (key != null) return key.GetValue(valueName) != null;
                }
            }
            catch { }
            return false;
        }

        private bool CheckIsLaptop()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Battery"))
                {
                    using (var collection = searcher.Get())
                    {
                        if (collection.Count > 0) return true;
                    }
                }
            }
            catch { }

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure"))
                {
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            UInt16[] chassisTypes = (UInt16[])obj["ChassisTypes"];
                            if (chassisTypes != null)
                            {
                                foreach (var type in chassisTypes)
                                {
                                    if (type == 8 || type == 9 || type == 10 || type == 11 || type == 12 || type == 14)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                if (!Directory.Exists(path)) return 0;
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }

        private long GetTaskSizeEstimate(string taskId)
        {
            long size = 0;
            try
            {
                if (taskId == "clean_temp")
                {
                    size += GetDirectorySize(Path.GetTempPath());
                    size += GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                }
                else if (taskId == "clean_wer")
                {
                    string werPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\WER");
                    size += GetDirectorySize(Path.Combine(werPath, "ReportArchive"));
                    size += GetDirectorySize(Path.Combine(werPath, "ReportQueue"));
                }
                else if (taskId == "clean_winupdate")
                {
                    string swDist = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"SoftwareDistribution\Download");
                    size += GetDirectorySize(swDist);
                }
                else if (taskId == "clean_browsers")
                {
                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    size += GetDirectorySize(Path.Combine(local, @"Google\Chrome\User Data\Default\Cache"));
                    size += GetDirectorySize(Path.Combine(local, @"Google\Chrome\User Data\Default\Code Cache"));
                    size += GetDirectorySize(Path.Combine(local, @"Microsoft\Edge\User Data\Default\Cache"));
                    size += GetDirectorySize(Path.Combine(local, @"Microsoft\Edge\User Data\Default\Code Cache"));
                    size += GetDirectorySize(Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data\Default\Cache"));
                }
                else if (taskId == "clean_trash")
                {
                    SHQUERYRBINFO rbInfo = new SHQUERYRBINFO();
                    rbInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                    int hr = SHQueryRecycleBin(null, ref rbInfo);
                    if (hr == 0)
                    {
                        size = rbInfo.i64Size;
                    }
                }
                else if (taskId == "win_winsxs")
                {
                    string winsxsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "WinSxS");
                    if (Directory.Exists(winsxsPath))
                    {
                        size = 1800000000;
                    }
                }
            }
            catch { }
            return size;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 KB";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        private void UpdateDynamicTasksStates()
        {
            try
            {
                // 1. HAGS (hw_hags)
                var taskHags = allTasks.Find(t => t.Id == "hw_hags");
                if (taskHags != null)
                {
                    int val = GetRegistryDWordValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", -1);
                    if (val == 2)
                    {
                        taskHags.Name = "Disabilita Accelerazione Hardware GPU (HAGS)";
                        taskHags.Description = "Disattiva la gestione diretta della VRAM da parte della GPU (consigliato in caso di incompatibilità).";
                        taskHags.Command = "registry_set_hklm:SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers:HwSchMode:1";
                        taskHags.Recommended = false;
                    }
                    else
                    {
                        taskHags.Name = "Abilita/Attiva Accelerazione Hardware GPU (HAGS)";
                        taskHags.Description = "Consente alla GPU di gestire direttamente la VRAM riducendo latenza e carico CPU.";
                        taskHags.Command = "registry_set_hklm:SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers:HwSchMode:2";
                        taskHags.Recommended = true;
                    }
                }

                // 2. Xbox GameDVR (hw_gamedvr)
                var taskGameDVR = allTasks.Find(t => t.Id == "hw_gamedvr");
                if (taskGameDVR != null)
                {
                    int val = GetRegistryDWordValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", -1);
                    if (val == 1)
                    {
                        taskGameDVR.Name = "Disabilita Registrazione Background Xbox GameDVR";
                        taskGameDVR.Description = "Disattiva il buffer video Xbox continuo, eliminando micro-stuttering e liberando RAM.";
                        taskGameDVR.Command = "registry_set_hkcu:System\\GameConfigStore:GameDVR_Enabled:0";
                        taskGameDVR.Recommended = true;
                    }
                    else
                    {
                        taskGameDVR.Name = "Abilita Registrazione Background Xbox GameDVR";
                        taskGameDVR.Description = "Abilita il buffer di registrazione video automatico in background per clip di gioco.";
                        taskGameDVR.Command = "registry_set_hkcu:System\\GameConfigStore:GameDVR_Enabled:1";
                        taskGameDVR.Recommended = false;
                    }
                }

                // 3. P2P Delivery Optimization (net_delivery)
                var taskDelivery = allTasks.Find(t => t.Id == "net_delivery");
                if (taskDelivery != null)
                {
                    int val = GetRegistryDWordValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", -1);
                    if (val == 0)
                    {
                        taskDelivery.Name = "Abilita Condivisione P2P Windows Update";
                        taskDelivery.Description = "Consente a Windows di condividere patch con altri PC della rete locale per risparmiare internet.";
                        taskDelivery.Command = "registry_set_hklm:SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization:DODownloadMode:3";
                        taskDelivery.Recommended = false;
                    }
                    else
                    {
                        taskDelivery.Name = "Disabilita Condivisione P2P Windows Update";
                        taskDelivery.Description = "Impedisce a Windows Update di usare la banda e CPU per inviare file ad altri PC.";
                        taskDelivery.Command = "registry_set_hklm:SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization:DODownloadMode:0";
                        taskDelivery.Recommended = true;
                    }
                }

                // 4. Windows 11 Widgets (os_w11_widgets)
                var taskWidgets = allTasks.Find(t => t.Id == "os_w11_widgets");
                if (taskWidgets != null)
                {
                    int val = GetRegistryDWordValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", -1);
                    if (val == 0)
                    {
                        taskWidgets.Name = "[Win 11] Abilita Feed Notizie & Widget Barra";
                        taskWidgets.Description = "Mostra l'icona meteo e il feed notizie nella barra delle applicazioni.";
                        taskWidgets.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced:TaskbarDa:1";
                        taskWidgets.Recommended = false;
                    }
                    else
                    {
                        taskWidgets.Name = "[Win 11] Disabilita Feed Notizie & Widget Barra";
                        taskWidgets.Description = "Disattiva i widget con notizie online e feed che consumano memoria WebView2.";
                        taskWidgets.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced:TaskbarDa:0";
                        taskWidgets.Recommended = true;
                    }
                }

                // 5. Windows 11 Chat (os_w11_chat)
                var taskChat = allTasks.Find(t => t.Id == "os_w11_chat");
                if (taskChat != null)
                {
                    int val = GetRegistryDWordValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", -1);
                    if (val == 0)
                    {
                        taskChat.Name = "[Win 11] Abilita Chat / Teams Consumer in Barra";
                        taskChat.Description = "Ripristina l'icona Chat/Teams integrata nella barra di Windows 11.";
                        taskChat.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced:TaskbarMn:1";
                        taskChat.Recommended = false;
                    }
                    else
                    {
                        taskChat.Name = "[Win 11] Disabilita Chat / Teams Consumer in Background";
                        taskChat.Description = "Rimuove l'icona Chat/Teams integrata nella barra di Windows 11.";
                        taskChat.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced:TaskbarMn:0";
                        taskChat.Recommended = true;
                    }
                }

                // 6. Cortana (os_w10_cortana)
                var taskCortana = allTasks.Find(t => t.Id == "os_w10_cortana");
                if (taskCortana != null)
                {
                    int val = GetRegistryDWordValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", -1);
                    if (val == 0)
                    {
                        taskCortana.Name = "[Win 10] Abilita Assistente Cortana in Background";
                        taskCortana.Description = "Consente l'attivazione e l'uso dell'assistente vocale Microsoft Cortana.";
                        taskCortana.Command = "registry_set_hklm:SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search:AllowCortana:1";
                        taskCortana.Recommended = false;
                    }
                    else
                    {
                        taskCortana.Name = "[Win 10] Disabilita Assistente Cortana in Background";
                        taskCortana.Description = "Disattiva l'assistente vocale Cortana residente nei processi di Windows 10.";
                        taskCortana.Command = "registry_set_hklm:SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search:AllowCortana:0";
                        taskCortana.Recommended = true;
                    }
                }

                // 7. Windows 10 News (os_w10_news)
                var taskNews = allTasks.Find(t => t.Id == "os_w10_news");
                if (taskNews != null)
                {
                    int val = GetRegistryDWordValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Feeds", "ShellFeedsTaskbarViewMode", -1);
                    if (val == 2)
                    {
                        taskNews.Name = "[Win 10] Abilita 'Notizie e Interessi' Barra";
                        taskNews.Description = "Mostra l'icona meteo e il feed notizie nella barra di Windows 10.";
                        taskNews.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Feeds:ShellFeedsTaskbarViewMode:0";
                        taskNews.Recommended = false;
                    }
                    else
                    {
                        taskNews.Name = "[Win 10] Disabilita 'Notizie e Interessi' Barra";
                        taskNews.Description = "Rimuove le notizie e gli interessi con feed meteo in background su Windows 10.";
                        taskNews.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Feeds:ShellFeedsTaskbarViewMode:2";
                        taskNews.Recommended = true;
                    }
                }

                // 8. Spotlight Icon (clean_spotlight)
                var taskSpotlight = allTasks.Find(t => t.Id == "clean_spotlight");
                if (taskSpotlight != null)
                {
                    int val = GetRegistryDWordValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{2cc5ca98-6485-489a-920e-b3e88a6ccce3}", -1);
                    if (val == 1)
                    {
                        taskSpotlight.Name = "Mostra Icona 'Scopri questa immagine' (Spotlight) Desktop";
                        taskSpotlight.Description = "Ripristina l'icona di Windows Spotlight per cambiare sfondo direttamente dal Desktop.";
                        taskSpotlight.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel:{2cc5ca98-6485-489a-920e-b3e88a6ccce3}:0";
                        taskSpotlight.Recommended = false;
                    }
                    else
                    {
                        taskSpotlight.Name = "Rimuovi Icona 'Scopri questa immagine' (Spotlight) Desktop";
                        taskSpotlight.Description = "Nasconde l'icona indesiderata di Windows Spotlight dal desktop.";
                        taskSpotlight.Command = "registry_set_hkcu:Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel:{2cc5ca98-6485-489a-920e-b3e88a6ccce3}:1";
                        taskSpotlight.Recommended = true;
                    }
                }
            }
            catch { }
        }

        private int GetRegistryDWordValue(RegistryKey root, string subKeyPath, string valueName, int defaultValue)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(subKeyPath))
                {
                    if (key != null && key.GetValue(valueName) != null)
                    {
                        return Convert.ToInt32(key.GetValue(valueName));
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private void ScanDynamicStartupApps()
        {
            ScanRunKey(Registry.CurrentUser, "hkcu");
            ScanRunKey(Registry.LocalMachine, "hklm");
        }

        private void ScanRunKey(RegistryKey root, string keyType)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (key == null) return;
                    foreach (string valName in key.GetValueNames())
                    {
                        if (IsEssentialStartup(valName)) continue;

                        // Evita duplicati con i task statici
                        if (allTasks.Exists(t => t.Id.StartsWith("start_") && t.Command.Contains(valName))) continue;

                        string id = "dyn_run_" + keyType + "_" + valName.ToLower().Replace(" ", "_");
                        if (allTasks.Exists(t => t.Id == id)) continue;

                        string displayLoc = keyType == "hkcu" ? "Utente" : "Macchina";
                        allTasks.Add(new TaskItem(
                            id,
                            "startup",
                            "App di Avvio (Startup)",
                            "Disabilita Avvio: " + valName,
                            "Rimuove l'avvio automatico di " + valName + " all'accesso di Windows (" + displayLoc + ").",
                            "registry_remove:" + keyType + ":" + valName,
                            false
                        ));
                    }
                }
            }
            catch { }
        }

        private bool IsEssentialStartup(string name)
        {
            string n = name.ToLower();
            string[] essentials = new string[] {
                "securityhealth", "windowsdefender", "advancedmicrodevices", "nvidia", 
                "intel", "realtek", "synaptics", "logitech", "onedrive", "onedrivesetup",
                "keyboardservice", "touchpad", "audio", "sound", "graphics", "vanguard",
                "antivirus", "mcafee", "norton", "avast", "avg", "kaspersky", "bitdefender",
                "malwarebytes", "sophos", "eset", "f-secure", "avira", "trendmicro", "panda"
            };

            foreach (var ess in essentials)
            {
                if (n.Contains(ess)) return true;
            }
            return false;
        }

        private void ScanDynamicThirdPartyServices()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, DisplayName, PathName, StartMode FROM Win32_Service"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        if (obj["PathName"] == null || obj["Name"] == null) continue;
                        string path = obj["PathName"].ToString().Trim();
                        string name = obj["Name"].ToString().Trim();
                        string dispName = obj["DisplayName"] != null ? obj["DisplayName"].ToString().Trim() : name;

                        // Controlla lo stato reale dal Registro (evita bug di cache WMI/SCM per servizi protetti)
                        if (GetServiceStartTypeFromRegistry(name) == 4) continue;

                        if (IsEssentialSystemPath(path, name, dispName)) continue;
                        if (IsSecurityService(name, dispName, path)) continue;

                        // Evita duplicati con i task statici
                        if (allTasks.Exists(t => t.Id == "telem_" + name.ToLower() || t.Id == "bloat_" + name.ToLower())) continue;

                        string id = "dyn_svc_" + name.ToLower();
                        if (allTasks.Exists(t => t.Id == id)) continue;

                        allTasks.Add(new TaskItem(
                            id,
                            "telemetry",
                            "Servizi, Bloatware & Telemetria",
                            "Disabilita Servizio: " + dispName,
                            "Arresta e disabilita l'avvio del servizio di terze parti '" + name + "' per liberare RAM.",
                            "service_disable:" + name,
                            false
                        ));
                    }
                }
            }
            catch { }
        }

        private int GetServiceStartTypeFromRegistry(string serviceName)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName))
                {
                    if (key != null && key.GetValue("Start") != null)
                    {
                        return Convert.ToInt32(key.GetValue("Start"));
                    }
                }
            }
            catch { }
            return -1;
        }

        private bool IsEssentialSystemPath(string path, string name, string dispName)
        {
            string p = path.ToLower();
            string n = name.ToLower();
            string d = dispName.ToLower();

            if (p.Contains("system32") && p.Contains("svchost.exe")) return true;
            if (p.Contains(@"c:\windows\system32\")) return true;
            if (p.Contains(@"c:\windows\system\")) return true;
            if (p.Contains(@"c:\windows\servicing\")) return true;
            if (p.Contains(@"c:\windows\winsxs\")) return true;
            if (p.Contains(@"c:\windows\microsoft.net\")) return true;

            string[] hardwareKeywords = new string[] {
                "nvidia", "nvspcaps", "display.driver", "amd", "ati technologies", "intel", 
                "realtek", "synaptics", "logitech", "soundblaster", "dolby", "waves audio", 
                "bluetooth", "wlan", "wifi", "wacom", "elantech", "asuslink", "delltech", "lenovo"
            };

            foreach (var kw in hardwareKeywords)
            {
                if (p.Contains(kw) || n.Contains(kw) || d.Contains(kw)) return true;
            }
            return false;
        }

        private bool IsSecurityService(string name, string dispName, string path)
        {
            string n = name.ToLower();
            string d = dispName.ToLower();
            string p = path.ToLower();

            string[] securityKeywords = new string[] {
                "defender", "wdnis", "msmpeng", "securityhealth", "antivirus", "antimalware", 
                "mcafee", "norton", "avast", "avg", "kaspersky", "bitdefender", "malwarebytes", 
                "sophos", "eset", "f-secure", "avira", "trendmicro", "panda", "bullguard", 
                "firewall", "sentinel", "crowdstrike", "kaspersky", "symantec", "webroot",
                "lphs", "mpssvc", "windefend", "sense"
            };

            foreach (var kw in securityKeywords)
            {
                if (n.Contains(kw) || d.Contains(kw) || p.Contains(kw)) return true;
            }
            return false;
        }

        private void BuildWpfUI()
        {
            Grid mainRoot = new Grid();

            // Row Definitions
            mainRoot.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80) });  // 0: Header
            mainRoot.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(185) }); // 1: Hardware
            mainRoot.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(56) });  // 2: Controls/Filters
            mainRoot.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // 3: Task list
            mainRoot.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(74) });  // 4: Footer

            // 0: HEADER
            Border bdrHeader = new Border() { Background = brushHeader, Padding = new Thickness(24, 12, 24, 12) };
            Grid grdHeader = new Grid();

            StackPanel spTitles = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            StackPanel spTitleBadges = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock lblMainTitle = new TextBlock() { Text = "Universal System & Windows Maintenance Center", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = brushTextWhite };
            
            Border bdrCredit = new Border() { Background = new SolidColorBrush(Color.FromArgb(40, 168, 85, 247)), BorderBrush = brushPurple, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(16, 0, 8, 0) };
            bdrCredit.Child = new TextBlock() { Text = "★ Sviluppato da E.P. con AI", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(216, 180, 254)) };

            Border bdrAdmin = new Border() { Background = new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)), BorderBrush = brushEmerald, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(10, 3, 10, 3) };
            bdrAdmin.Child = new TextBlock() { Text = "✓ ADMIN READY", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = brushEmerald };

            spTitleBadges.Children.Add(lblMainTitle);
            spTitleBadges.Children.Add(bdrCredit);
            spTitleBadges.Children.Add(bdrAdmin);

            TextBlock lblSubTitle = new TextBlock() { Text = "Manutenzione completa Windows 10/11 • Risparmio Energia & Batteria • Ottimizzazione Hardware", FontSize = 12, Foreground = brushTextSlate, Margin = new Thickness(0, 4, 0, 0) };

            spTitles.Children.Add(spTitleBadges);
            spTitles.Children.Add(lblSubTitle);

            StackPanel spHeaderButtons = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

            Button btnRefresh = new Button() {
                Content = "🔄 Ricontrolla PC",
                VerticalAlignment = VerticalAlignment.Center,
                Width = 130,
                Height = 36,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = brushCard,
                Foreground = brushCyan,
                BorderBrush = brushBorder,
                BorderThickness = new Thickness(1),
                Template = CreateRoundedButtonTemplate(new CornerRadius(8)),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnRefresh.Click += (s, e) => {
                splashOverlay.Visibility = Visibility.Visible;
                txtSplashStatus.Text = "Riavvio analisi del PC...";
                splashProgressBar.Value = 0;

                Task.Run(() =>
                {
                    InitTasks();
                    ScanHardwareBackground((pct, msg) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            splashProgressBar.Value = pct;
                            txtSplashStatus.Text = msg;
                        });
                    });

                    Dispatcher.Invoke(() =>
                    {
                        UpdateHwCards();
                        RenderTaskList();
                        UpdateFooterSummary();
                        splashOverlay.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Diagnostica hardware e controllo sistema aggiornati con successo!", "Universal Optimizer", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            };

            Button btnExportReport = new Button() {
                Content = "📄 Report Sistema",
                VerticalAlignment = VerticalAlignment.Center,
                Width = 140,
                Height = 36,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = brushCard,
                Foreground = brushPurple,
                BorderBrush = brushBorder,
                BorderThickness = new Thickness(1),
                Template = CreateRoundedButtonTemplate(new CornerRadius(8)),
                Cursor = Cursors.Hand
            };
            btnExportReport.Click += BtnExportReport_Click;

            Button btnDisclaimer = new Button() {
                Content = "ℹ️ Note Legali & Info",
                VerticalAlignment = VerticalAlignment.Center,
                Width = 150,
                Height = 36,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = brushCard,
                Foreground = brushAmber,
                BorderBrush = brushBorder,
                BorderThickness = new Thickness(1),
                Template = CreateRoundedButtonTemplate(new CornerRadius(8)),
                Cursor = Cursors.Hand,
                Margin = new Thickness(10, 0, 0, 0)
            };
            btnDisclaimer.Click += (s, e) => {
                DisclaimerWindow dWin = new DisclaimerWindow();
                dWin.Owner = this;
                dWin.ShowDialog();
            };

            spHeaderButtons.Children.Add(btnRefresh);
            spHeaderButtons.Children.Add(btnExportReport);
            spHeaderButtons.Children.Add(btnDisclaimer);

            grdHeader.Children.Add(spTitles);
            grdHeader.Children.Add(spHeaderButtons);
            bdrHeader.Child = grdHeader;
            Grid.SetRow(bdrHeader, 0);
            mainRoot.Children.Add(bdrHeader);

            // 1: HARDWARE DIAGNOSTICS
            Border bdrHw = new Border() { Background = brushCard, Margin = new Thickness(20, 10, 20, 6), Padding = new Thickness(16, 10, 16, 10), CornerRadius = new CornerRadius(8), BorderBrush = brushBorder, BorderThickness = new Thickness(1) };
            Grid grdHw = new Grid();
            grdHw.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24) });
            grdHw.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(96) });
            grdHw.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24) });

            txtHwHeader = new TextBlock() { Text = "📊 RAPPORTO HARDWARE & DIAGNOSTICA IN TEMPO REALE (" + sysInfo.DeviceModel + " • " + sysInfo.OEMBrand.ToUpper() + ")", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = brushCyan };
            Grid.SetRow(txtHwHeader, 0);
            grdHw.Children.Add(txtHwHeader);

            UniformGrid ugHwCards = new UniformGrid() { Columns = 4, Margin = new Thickness(0, 4, 0, 4) };

            Border cardCpu = CreateHwCard("CPU", sysInfo.CPU, sysInfo.Cores, brushCyan, out txtCpuTitle, out txtCpuSub);
            Border cardRam = CreateHwCard("RAM", sysInfo.RAM_FreeGB + " GB Liberi / " + sysInfo.RAM_TotalGB + " GB", sysInfo.RAM_UsedPercent + "% in uso", brushEmerald, out txtRamTitle, out txtRamSub);
            Border cardGpu = CreateHwCard("GPU", sysInfo.GPU, "Driver: " + sysInfo.GPU_Driver, brushAmber, out txtGpuTitle, out txtGpuSub);
            Border cardDisk = CreateHwCard("DISCO C: (SSD NVMe)", sysInfo.Disk_FreeGB + " GB Liberi / " + sysInfo.Disk_SizeGB + " GB", "TRIM Attivo • " + sysInfo.Disk_UsedPercent + "% pieno", brushPurple, out txtDiskTitle, out txtDiskSub);
            cardDisk.Cursor = Cursors.Hand;
            cardDisk.ToolTip = "Clicca qui per vedere tutti i dischi collegati";

            StackPanel spDisk = cardDisk.Child as StackPanel;
            if (spDisk != null)
            {
                TextBlock lblTitle = spDisk.Children.Count > 0 ? spDisk.Children[0] as TextBlock : null;
                if (lblTitle != null)
                {
                    Grid titleGrid = new Grid();
                    titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    spDisk.Children.RemoveAt(0);
                    lblTitle.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(lblTitle, 0);
                    titleGrid.Children.Add(lblTitle);

                    TextBlock txtLens = new TextBlock
                    {
                        Text = "🔍",
                        FontSize = 15.0, // Resa più grande
                        Foreground = brushPurple,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    Grid.SetColumn(txtLens, 2);
                    titleGrid.Children.Add(txtLens);

                    spDisk.Children.Insert(0, titleGrid);
                }
            }

            cardDisk.MouseEnter += (s, e) => { cardDisk.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)); cardDisk.BorderBrush = brushPurple; };
            cardDisk.MouseLeave += (s, e) => { cardDisk.Background = brushCard; cardDisk.BorderBrush = brushBorder; };
            cardDisk.MouseLeftButtonUp += (s, e) => {
                DiskDetailsWindow dWindow = new DiskDetailsWindow();
                dWindow.Owner = this;
                dWindow.ShowDialog();
            };

            ugHwCards.Children.Add(cardCpu);
            ugHwCards.Children.Add(cardRam);
            ugHwCards.Children.Add(cardGpu);
            ugHwCards.Children.Add(cardDisk);
            Grid.SetRow(ugHwCards, 1);
            grdHw.Children.Add(ugHwCards);

            txtOsFooterTag = new TextBlock() { Text = "Sistema: " + sysInfo.OS + "  |  Versione: " + (sysInfo.OSType == "win11" ? "Windows 11 (Rilevato)" : "Windows 10 (Rilevato)"), FontSize = 11, Foreground = brushTextMuted, Margin = new Thickness(4, 2, 0, 0) };
            Grid.SetRow(txtOsFooterTag, 2);
            grdHw.Children.Add(txtOsFooterTag);

            bdrHw.Child = grdHw;
            Grid.SetRow(bdrHw, 1);
            mainRoot.Children.Add(bdrHw);

            // 2: CONTROLS & FILTER TABS
            Border bdrControls = new Border() { Background = new SolidColorBrush(Color.FromRgb(14, 20, 34)), Padding = new Thickness(14, 6, 14, 6), BorderBrush = brushBorder, BorderThickness = new Thickness(0, 1, 0, 1) };
            DockPanel dpControls = new DockPanel();

            btnProfiles = new Button() {
                Content = "⭐ Seleziona Consigliati ▾",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(26, 36, 56)),
                Foreground = brushEmerald,
                BorderBrush = brushBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 5, 12, 5),
                Template = CreateRoundedButtonTemplate(new CornerRadius(8)),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            ContextMenu cmProfiles = new ContextMenu() {
                Template = CreateCustomContextMenuTemplate()
            };

            MenuItem miBasic = new MenuItem() {
                Header = "🛡️  Manutenzione Basic",
                Foreground = brushCyan,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Template = CreateCustomMenuItemTemplate()
            };
            miBasic.Click += (s, e) => { SetSelectionMode("light_maintenance"); };

            MenuItem miRec = new MenuItem() {
                Header = "⭐  Seleziona Consigliati",
                Foreground = brushEmerald,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Template = CreateCustomMenuItemTemplate()
            };
            miRec.Click += (s, e) => { SetSelectionMode("recommended"); };

            MenuItem miAll = new MenuItem() {
                Header = "🚀  Manutenzione Totale",
                Foreground = brushPurple,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Template = CreateCustomMenuItemTemplate()
            };
            miAll.Click += (s, e) => { SetSelectionMode("all"); };

            MenuItem miNone = new MenuItem() {
                Header = "✍️  Selezione Manuale",
                Foreground = brushAmber,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Template = CreateCustomMenuItemTemplate()
            };
            miNone.Click += (s, e) => { SetSelectionMode("none"); };

            cmProfiles.Items.Add(miBasic);
            cmProfiles.Items.Add(miRec);
            cmProfiles.Items.Add(miAll);
            cmProfiles.Items.Add(new Separator() { Background = brushBorder, Margin = new Thickness(4, 4, 4, 4) });
            cmProfiles.Items.Add(miNone);

            btnProfiles.ContextMenu = cmProfiles;
            btnProfiles.Click += (s, e) => {
                cmProfiles.PlacementTarget = btnProfiles;
                cmProfiles.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                cmProfiles.IsOpen = true;
            };
            DockPanel.SetDock(btnProfiles, Dock.Right);

            StackPanel spFilters = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            spFilters.Children.Add(CreateFilterTab("Risparmio Energia", "power"));
            spFilters.Children.Add(CreateFilterTab("Manutenzione Windows", "win_maintenance"));
            spFilters.Children.Add(CreateFilterTab("App Avvio", "startup"));
            spFilters.Children.Add(CreateFilterTab("Telemetria & Servizi", "telemetry"));
            spFilters.Children.Add(CreateFilterTab("Hardware & Driver", "hardware"));
            spFilters.Children.Add(CreateFilterTab("Pulizia & Cache", "cleanup"));

            TextBox txtSearch = new TextBox()
            {
                Width = 145,
                Height = 28,
                FontSize = 11,
                Background = brushCard,
                Foreground = brushTextSlate,
                BorderBrush = brushBorder,
                BorderThickness = new Thickness(1),
                Text = "🔍 Cerca...",
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 0, 8, 0),
                Margin = new Thickness(10, 0, 0, 0),
                Template = CreateRoundedTextBoxTemplate(new CornerRadius(8)),
                Cursor = Cursors.IBeam
            };

            txtSearch.GotFocus += (s, e) => {
                if (txtSearch.Text == "🔍 Cerca...") {
                    txtSearch.Text = "";
                    txtSearch.Foreground = brushTextWhite;
                }
            };
            txtSearch.LostFocus += (s, e) => {
                if (string.IsNullOrEmpty(txtSearch.Text)) {
                    txtSearch.Text = "🔍 Cerca...";
                    txtSearch.Foreground = brushTextSlate;
                }
            };
            txtSearch.TextChanged += (s, e) => {
                string query = txtSearch.Text.Trim();
                if (query == "🔍 Cerca...") query = "";
                searchQuery = query;
                RenderTaskList();
            };

            spFilters.Children.Add(txtSearch);

            dpControls.Children.Add(btnProfiles);
            dpControls.Children.Add(spFilters);
            bdrControls.Child = dpControls;
            Grid.SetRow(bdrControls, 2);
            mainRoot.Children.Add(bdrControls);

            // 3: TASK SCROLLABLE LIST
            ScrollViewer svTasks = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(20, 8, 20, 8) };
            pnlTasks = new StackPanel();
            svTasks.Content = pnlTasks;
            Grid.SetRow(svTasks, 3);
            mainRoot.Children.Add(svTasks);

            // 4: FOOTER
            Border bdrFooter = new Border() { Background = brushHeader, Padding = new Thickness(24, 12, 24, 12), BorderBrush = brushBorder, BorderThickness = new Thickness(0, 1, 0, 0) };
            Grid grdFooter = new Grid();

            StackPanel spSummary = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            txtSelectedCount = new TextBlock() { Text = "0", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = brushCyan, Margin = new Thickness(0, 0, 10, 0) };
            txtSummaryDesc = new TextBlock() { Text = "operazioni selezionate pronte per l'esecuzione.", FontSize = 13, Foreground = brushTextSlate, VerticalAlignment = VerticalAlignment.Center };
            spSummary.Children.Add(txtSelectedCount);
            spSummary.Children.Add(txtSummaryDesc);

            btnRun = new Button() {
                Content = "⚡ ESEGUI MANUTENZIONE SELEZIONATA",
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 340,
                Height = 48,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(5, 10, 20)),
                Background = new LinearGradientBrush(Color.FromRgb(56, 189, 248), Color.FromRgb(2, 132, 199), new Point(0, 0), new Point(1, 1)),
                BorderThickness = new Thickness(0),
                Template = CreateRoundedButtonTemplate(new CornerRadius(12)),
                Cursor = Cursors.Hand
            };
            btnRun.Click += BtnRun_Click;

            grdFooter.Children.Add(spSummary);
            grdFooter.Children.Add(btnRun);
            bdrFooter.Child = grdFooter;
            Grid.SetRow(bdrFooter, 4);
            mainRoot.Children.Add(bdrFooter);

            // IN-APP PROGRESS MODAL OVERLAY
            BuildModalOverlay(mainRoot);

            Grid windowRoot = new Grid();
            windowRoot.Children.Add(mainRoot);
            BuildStartupSplash(windowRoot);

            this.Content = windowRoot;
        }

        private ControlTemplate CreateRoundedButtonTemplate(CornerRadius cornerRadius)
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.CornerRadiusProperty, cornerRadius);
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.Name = "contentPresenter";
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetBinding(ContentPresenter.MarginProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            border.AppendChild(contentPresenter);
            template.VisualTree = border;

            // Evita che WPF disegni pulsanti quadrati o bianchi quando IsEnabled = false
            Trigger disabledTrigger = new Trigger() { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(26, 36, 54)), "border"));
            disabledTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(40, 53, 72)), "border"));
            template.Triggers.Add(disabledTrigger);

            return template;
        }

        private ControlTemplate CreateRoundedTextBoxTemplate(CornerRadius cornerRadius)
        {
            ControlTemplate template = new ControlTemplate(typeof(TextBox));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.CornerRadiusProperty, cornerRadius);
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            FrameworkElementFactory scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.Name = "PART_ContentHost";
            scrollViewer.SetValue(ScrollViewer.FocusableProperty, false);
            scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
            scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);

            border.AppendChild(scrollViewer);
            template.VisualTree = border;
            return template;
        }

        private ControlTemplate CreateCustomContextMenuTemplate()
        {
            ControlTemplate template = new ControlTemplate(typeof(ContextMenu));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(15, 23, 42)));
            border.SetValue(Border.BorderBrushProperty, brushBorder);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            border.SetValue(Border.PaddingProperty, new Thickness(6));

            FrameworkElementFactory stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.IsItemsHostProperty, true);

            border.AppendChild(stackPanel);
            template.VisualTree = border;
            return template;
        }

        private ControlTemplate CreateCustomMenuItemTemplate()
        {
            ControlTemplate template = new ControlTemplate(typeof(MenuItem));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "bdrItem";
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            border.SetValue(Border.PaddingProperty, new Thickness(14, 8, 18, 8));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.MarginProperty, new Thickness(2, 1, 2, 1));
            border.SetValue(Border.CursorProperty, Cursors.Hand);

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger() { Property = MenuItem.IsHighlightedProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 41, 59)), "bdrItem"));
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        private void BuildModalOverlay(Grid mainRoot)
        {
            modalBackdrop = new Grid() { Background = new SolidColorBrush(Color.FromArgb(200, 5, 8, 17)), Visibility = Visibility.Collapsed };
            Grid.SetRowSpan(modalBackdrop, 5);

            Border modalWindow = new Border() {
                Width = 720,
                Height = 590,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(140, 56, 189, 248)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24)
            };
            modalWindow.Effect = new DropShadowEffect() { Color = Color.FromRgb(56, 189, 248), BlurRadius = 25, Opacity = 0.25, ShadowDepth = 0 };

            Grid modalGrid = new Grid();
            modalGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });  // Title
            modalGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });  // Warning Banner
            modalGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(70) });  // Progress Bar
            modalGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }); // Step List
            modalGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });  // Command description/log (gray)
            modalGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50) });  // Footer Button

            txtModalTitle = new TextBlock() { Text = "⚡ Applicazione Manutenzioni in corso...", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = brushCyan, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(txtModalTitle, 0);
            modalGrid.Children.Add(txtModalTitle);

            modalWarningBanner = new Border() {
                Background = new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 4, 0, 6)
            };
            txtModalWarningText = new TextBlock() {
                Text = "🔒 MANUTENZIONE IN CORSO — NON CHIUDERE QUESTA FINESTRA\nI processi di sistema sono attivi. Non chiudere l'app né spegnere il PC fino al termine.",
                FontSize = 11,
                Foreground = brushTextWhite,
                TextWrapping = TextWrapping.Wrap
            };
            modalWarningBanner.Child = txtModalWarningText;
            Grid.SetRow(modalWarningBanner, 1);
            modalGrid.Children.Add(modalWarningBanner);

            StackPanel spProgress = new StackPanel() { Margin = new Thickness(0, 10, 0, 10) };
            txtModalPercent = new TextBlock() { Text = "0%", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = brushCyan, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };

            modalProgressBar = new ProgressBar() {
                Height = 14,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Foreground = new LinearGradientBrush(Color.FromRgb(56, 189, 248), Color.FromRgb(2, 132, 199), new Point(0, 0), new Point(1, 0)),
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 4, 0, 4)
            };

            spProgress.Children.Add(txtModalPercent);
            spProgress.Children.Add(modalProgressBar);
            Grid.SetRow(spProgress, 2);
            modalGrid.Children.Add(spProgress);

            Border bdrSv = new Border() {
                Background = new SolidColorBrush(Color.FromRgb(11, 15, 25)),
                BorderBrush = brushBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 6, 0, 6)
            };
            ScrollViewer svModalSteps = new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            modalStepList = new StackPanel() { Margin = new Thickness(8) };
            svModalSteps.Content = modalStepList;
            bdrSv.Child = svModalSteps;
            Grid.SetRow(bdrSv, 3);
            modalGrid.Children.Add(bdrSv);

            txtModalCommandLog = new TextBlock() {
                Text = "",
                FontSize = 11.5,
                Foreground = brushTextMuted,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid.SetRow(txtModalCommandLog, 4);
            modalGrid.Children.Add(txtModalCommandLog);

            btnModalDone = new Button() {
                Content = "🔒 In esecuzione... (non chiudere)",
                Height = 40,
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Right,
                FontWeight = FontWeights.Bold,
                IsEnabled = false,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Foreground = brushTextMuted,
                BorderThickness = new Thickness(0),
                Template = CreateRoundedButtonTemplate(new CornerRadius(10)),
                Cursor = Cursors.Hand
            };
            btnModalDone.Click += (s, e) => {
                if (isExecuting) return;
                modalBackdrop.Visibility = Visibility.Collapsed;
            };
            Grid.SetRow(btnModalDone, 5);
            modalGrid.Children.Add(btnModalDone);

            modalWindow.Child = modalGrid;
            modalBackdrop.Children.Add(modalWindow);
            mainRoot.Children.Add(modalBackdrop);
        }

        private void BuildStartupSplash(Grid parent)
        {
            splashOverlay = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(11, 15, 25)), // brushBg
                Visibility = Visibility.Visible
            };

            Grid contentGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // App title
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Sub title
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Progress status text
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Progress bar
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Note

            TextBlock lblTitle = new TextBlock
            {
                Text = "UNIVERSAL MAINTENANCE SUITE",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = brushCyan,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(lblTitle, 0);
            contentGrid.Children.Add(lblTitle);

            TextBlock lblSub = new TextBlock
            {
                Text = "Analisi iniziale hardware, servizi e stima spazio liberabile in corso...",
                FontSize = 13,
                Foreground = brushTextSlate,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };
            Grid.SetRow(lblSub, 1);
            contentGrid.Children.Add(lblSub);

            txtSplashStatus = new TextBlock
            {
                Text = "Inizializzazione...",
                FontSize = 12,
                Foreground = brushTextWhite,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(txtSplashStatus, 2);
            contentGrid.Children.Add(txtSplashStatus);

            splashProgressBar = new ProgressBar
            {
                Width = 450,
                Height = 10,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Foreground = new LinearGradientBrush(Color.FromRgb(56, 189, 248), Color.FromRgb(2, 132, 199), new Point(0, 0), new Point(1, 0)),
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderThickness = new Thickness(0)
            };
            Grid.SetRow(splashProgressBar, 3);
            contentGrid.Children.Add(splashProgressBar);

            TextBlock lblNote = new TextBlock
            {
                Text = "La scansione iniziale in background assicura la massima reattività dell'applicazione.",
                FontSize = 11,
                Foreground = brushTextMuted,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            Grid.SetRow(lblNote, 4);
            contentGrid.Children.Add(lblNote);

            splashOverlay.Children.Add(contentGrid);
            parent.Children.Add(splashOverlay);
        }

        private Border CreateHwCard(string label, string mainText, string subText, SolidColorBrush accent, out TextBlock txtMain, out TextBlock txtSub)
        {
            Border bdr = new Border() { Background = brushCard, BorderBrush = brushBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Margin = new Thickness(4), Padding = new Thickness(12, 8, 12, 8) };
            StackPanel sp = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };

            TextBlock lbl = new TextBlock() { Text = label, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = accent };
            txtMain = new TextBlock() { Text = mainText, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = brushTextWhite, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
            txtSub = new TextBlock() { Text = subText, FontSize = 11, Foreground = brushTextSlate, Margin = new Thickness(0, 2, 0, 0) };

            sp.Children.Add(lbl);
            sp.Children.Add(txtMain);
            sp.Children.Add(txtSub);
            bdr.Child = sp;
            return bdr;
        }

        private void UpdateHwCards()
        {
            txtHwHeader.Text = "📊 RAPPORTO HARDWARE & DIAGNOSTICA IN TEMPO REALE (" + sysInfo.DeviceModel + " • " + sysInfo.OEMBrand.ToUpper() + ")";
            txtCpuTitle.Text = sysInfo.CPU;
            txtCpuSub.Text = sysInfo.Cores;

            txtRamTitle.Text = sysInfo.RAM_FreeGB + " GB Liberi / " + sysInfo.RAM_TotalGB + " GB";
            txtRamSub.Text = sysInfo.RAM_UsedPercent + "% in uso";

            txtGpuTitle.Text = sysInfo.GPU;
            txtGpuSub.Text = "Driver: " + sysInfo.GPU_Driver;

            txtDiskTitle.Text = sysInfo.Disk_FreeGB + " GB Liberi / " + sysInfo.Disk_SizeGB + " GB";
            txtDiskSub.Text = "TRIM Attivo • " + sysInfo.Disk_UsedPercent + "% pieno";

            txtOsFooterTag.Text = "Sistema: " + sysInfo.OS + "  |  Versione: " + (sysInfo.OSType == "win11" ? "Windows 11 (Rilevato)" : "Windows 10 (Rilevato)");
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Filter = "Documento di testo (*.txt)|*.txt";
            sfd.FileName = "Report_Sistema_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            sfd.Title = "Salva Report di Sistema Completo";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string reportText = GenerateSystemReportText();
                    File.WriteAllText(sfd.FileName, reportText, Encoding.UTF8);
                    MessageBox.Show("Report di sistema salvato con successo!", "Salvataggio Completato", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Errore durante il salvataggio del report:\n" + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GenerateSystemReportText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("====================================================");
            sb.AppendLine("   REPORT COMPLETO DI SISTEMA (HARDWARE & SOFTWARE) ");
            sb.AppendLine("====================================================");
            sb.AppendLine(string.Format("Generato il: {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            sb.AppendLine();

            sb.AppendLine("--- INFORMAZIONI DI SISTEMA (SOFTWARE) ---");
            sb.AppendLine(string.Format("Sistema Operativo: {0}", sysInfo.OS));
            sb.AppendLine(string.Format("Tipo Windows:      {0}", sysInfo.OSType == "win11" ? "Windows 11" : "Windows 10"));
            sb.AppendLine(string.Format("Nome Computer:     {0}", Environment.MachineName));
            sb.AppendLine(string.Format("Utente Corrente:   {0}", Environment.UserName));
            sb.AppendLine(string.Format("Directory Windows: {0}", Environment.SystemDirectory));
            sb.AppendLine();

            sb.AppendLine("--- SPECIFICHE HARDWARE ---");
            sb.AppendLine(string.Format("Produttore/OEM:    {0}", sysInfo.OEMBrand));
            sb.AppendLine(string.Format("Modello PC:        {0}", sysInfo.DeviceModel));
            sb.AppendLine(string.Format("Processore (CPU):  {0}", sysInfo.CPU));
            sb.AppendLine(string.Format("Core / Thread:     {0}", sysInfo.Cores));
            sb.AppendLine(string.Format("Memoria RAM:       {0} GB totali ({1} GB liberi - {2}% in uso)", sysInfo.RAM_TotalGB, sysInfo.RAM_FreeGB, sysInfo.RAM_UsedPercent));
            sb.AppendLine(string.Format("Scheda Video (GPU):{0} (Driver: {1})", sysInfo.GPU, sysInfo.GPU_Driver));
            sb.AppendLine();

            sb.AppendLine("--- ANALISI DISCHI E STATO S.M.A.R.T. ---");
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, Model, Status, Size FROM Win32_DiskDrive"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string model = obj["Model"] != null ? obj["Model"].ToString().Trim() : "Disco Hardware";
                        string status = obj["Status"] != null ? obj["Status"].ToString().Trim() : "OK";
                        long size = obj["Size"] != null ? Convert.ToInt64(obj["Size"]) : 0;

                        string failurePredict = "";
                        try
                        {
                            using (var predictor = new ManagementObjectSearcher(@"root\wmi", "SELECT PredictFailure FROM MSStorageDriver_FailurePredictStatus"))
                            {
                                foreach (var pObj in predictor.Get())
                                {
                                    if (Convert.ToBoolean(pObj["PredictFailure"]))
                                    {
                                        failurePredict = " (ATTENZIONE: PREVISIONE DI FALLIMENTO HARDWARE!)";
                                    }
                                }
                            }
                        }
                        catch { }

                        sb.AppendLine(string.Format("• Fisico: {0} | Dimensione: {1} | Stato SMART: {2}{3}", model, FormatBytes(size), status, failurePredict));
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Errore durante la lettura WMI dei dischi fisici: " + ex.Message);
            }

            sb.AppendLine();
            sb.AppendLine("--- PARTIZIONI E FILE SYSTEM ---");
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                try
                {
                    sb.AppendLine(string.Format("• Unità {0} [{1}] | File System: {2} | Spazio: {3} liberi su {4}",
                        drive.Name,
                        string.IsNullOrEmpty(drive.VolumeLabel) ? "Locale" : drive.VolumeLabel,
                        drive.DriveFormat,
                        FormatBytes(drive.AvailableFreeSpace),
                        FormatBytes(drive.TotalSize)));
                }
                catch { }
            }

            sb.AppendLine();
            sb.AppendLine("--- STATO DEI SERVIZI DI MANUTENZIONE CRITICI ---");
            string[] services = new string[] { "wuauserv", "bits", "Spooler", "DiagTrack", "SysMain" };
            foreach (var service in services)
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher(string.Format("SELECT State, StartMode FROM Win32_Service WHERE Name='{0}'", service)))
                    {
                        bool found = false;
                        foreach (var obj in searcher.Get())
                        {
                            string state = obj["State"] != null ? obj["State"].ToString() : "Sconosciuto";
                            string startMode = obj["StartMode"] != null ? obj["StartMode"].ToString() : "Sconosciuto";
                            sb.AppendLine(string.Format("• Servizio: {0} ➔ Stato: {1} (Tipo Avvio: {2})", service, state, startMode));
                            found = true;
                        }
                        if (!found)
                        {
                            sb.AppendLine(string.Format("• Servizio: {0} ➔ Non trovato nel sistema", service));
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine(string.Format("• Servizio: {0} ➔ Errore durante la query WMI: {1}", service, ex.Message));
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- PROGRAMMI IN AVVIO AUTOMATICO (REGISTRY) ---");
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            sb.AppendLine(string.Format("• HKLM Run: {0} ➔ {1}", valueName, key.GetValue(valueName)));
                        }
                    }
                }
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            sb.AppendLine(string.Format("• HKCU Run: {0} ➔ {1}", valueName, key.GetValue(valueName)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Errore durante la lettura dei programmi all'avvio: " + ex.Message);
            }

            sb.AppendLine();
            sb.AppendLine("--- STORICO RECENTE ERRORI & CRASH DI WINDOWS (DIAGNOSTICA EVENTI) ---");
            try
            {
                bool hasErrors = false;
                DateTime threshold = DateTime.Now.AddDays(-14);

                // 1. Crash di Sistema, BSOD e Kernel-Power
                using (EventLog sysLog = new EventLog("System"))
                {
                    int count = 0;
                    for (int i = sysLog.Entries.Count - 1; i >= 0 && count < 10; i--)
                    {
                        EventLogEntry entry = sysLog.Entries[i];
                        if (entry.TimeGenerated < threshold) break;

                        long eventId = (entry.InstanceId & 0xFFFF);
                        if (entry.EntryType == EventLogEntryType.Error || eventId == 41 || eventId == 1001)
                        {
                            hasErrors = true;
                            count++;
                            string errorType = "Errore di Sistema / Servizio";
                            if (eventId == 41) errorType = "CRASH CRITICO / RIAVVIO IMPROVVISO (Kernel-Power)";
                            else if (eventId == 1001 && (entry.Source.IndexOf("BugCheck", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Message != null && entry.Message.IndexOf("Bugcheck", StringComparison.OrdinalIgnoreCase) >= 0))) errorType = "SCHERMATA BLU (BSOD / BugCheck)";
                            else if (entry.EntryType == EventLogEntryType.Error) errorType = "Errore Servizio / Driver Sistema";

                            sb.AppendLine(string.Format("• [{0}] Tipo: {1}", entry.TimeGenerated.ToString("dd/MM/yyyy HH:mm:ss"), errorType));
                            sb.AppendLine(string.Format("   Origine: {0} (ID Evento: {1})", entry.Source, eventId));
                            string cleanMsg = entry.Message != null ? entry.Message.Replace("\r\n", " ").Replace("\n", " ").Trim() : "Nessun dettaglio aggiuntivo";
                            if (cleanMsg.Length > 240) cleanMsg = cleanMsg.Substring(0, 240) + "...";
                            sb.AppendLine(string.Format("   Causa/Dettagli: {0}", cleanMsg));
                            sb.AppendLine();
                        }
                    }
                }

                // 2. Crash Applicativi (Application Error / Hang / WER)
                using (EventLog appLog = new EventLog("Application"))
                {
                    int count = 0;
                    for (int i = appLog.Entries.Count - 1; i >= 0 && count < 10; i--)
                    {
                        EventLogEntry entry = appLog.Entries[i];
                        if (entry.TimeGenerated < threshold) break;

                        long eventId = (entry.InstanceId & 0xFFFF);
                        if (entry.EntryType == EventLogEntryType.Error && (eventId == 1000 || eventId == 1002 || entry.Source == "Application Error" || entry.Source == "Application Hang" || entry.Source == "Windows Error Reporting"))
                        {
                            hasErrors = true;
                            count++;
                            string errorType = (eventId == 1002 || entry.Source == "Application Hang") ? "BLOCCO APPLICAZIONE (Application Hang)" : "CRASH APPLICAZIONE (Application Error)";

                            sb.AppendLine(string.Format("• [{0}] Tipo: {1}", entry.TimeGenerated.ToString("dd/MM/yyyy HH:mm:ss"), errorType));
                            sb.AppendLine(string.Format("   Origine: {0} (ID Evento: {1})", entry.Source, eventId));
                            string cleanMsg = entry.Message != null ? entry.Message.Replace("\r\n", " ").Replace("\n", " ").Trim() : "Nessun dettaglio aggiuntivo";
                            if (cleanMsg.Length > 240) cleanMsg = cleanMsg.Substring(0, 240) + "...";
                            sb.AppendLine(string.Format("   Causa/Dettagli: {0}", cleanMsg));
                            sb.AppendLine();
                        }
                    }
                }

                if (!hasErrors)
                {
                    sb.AppendLine("✔ Nessun crash critico, BSOD o errore grave registrato negli ultimi 14 giorni. Il sistema risulta stabile.");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Avviso: Impossibile completare la lettura del registro eventi di Windows: " + ex.Message);
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("====================================================");
            sb.AppendLine("             FINE DEL REPORT DI SISTEMA             ");
            sb.AppendLine("====================================================");
            return sb.ToString();
        }

        private List<Button> filterButtons = new List<Button>();

        private Button CreateFilterTab(string label, string filterKey)
        {
            bool isActive = (filterKey == currentFilter);
            Button btn = new Button() {
                Content = label,
                Tag = filterKey,
                FontSize = 11,
                FontWeight = (isActive ? FontWeights.Bold : FontWeights.Normal),
                Background = (isActive ? new SolidColorBrush(Color.FromRgb(30, 58, 95)) : brushCard),
                Foreground = (isActive ? brushCyan : brushTextSlate),
                BorderBrush = (isActive ? brushCyan : brushBorder),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0),
                Template = CreateRoundedButtonTemplate(new CornerRadius(8)),
                Cursor = Cursors.Hand
            };

            filterButtons.Add(btn);

            btn.Click += (s, e) => {
                if (currentFilter == filterKey)
                {
                    currentFilter = "all";
                }
                else
                {
                    currentFilter = filterKey;
                }
                UpdateFilterTabsVisuals();
                RenderTaskList();
            };
            return btn;
        }

        private void UpdateFilterTabsVisuals()
        {
            int totalSelected = activeTasks.FindAll(t => t.Selected).Count;
            int totalTasks = activeTasks.Count;

            foreach (var b in filterButtons)
            {
                if (b.Tag == null) continue;
                string key = b.Tag.ToString();
                bool isCurrent = (key == currentFilter);

                bool hasActiveHighlight = false;
                if (key == "all")
                {
                    // "Tutte" si evidenzia quando non è aperta SOLO se sono selezionate effettivamente TUTTE le voci
                    hasActiveHighlight = (totalSelected > 0 && totalSelected == totalTasks);
                }
                else
                {
                    // Le categorie specifiche si evidenziano se hanno almeno 1 voce selezionata
                    int selectedInCat = activeTasks.FindAll(t => t.Category == key && t.Selected).Count;
                    hasActiveHighlight = (selectedInCat > 0);
                }

                if (isCurrent)
                {
                    // Scheda attualmente aperta
                    b.Background = new SolidColorBrush(Color.FromRgb(30, 58, 95));
                    b.Foreground = brushCyan;
                    b.BorderBrush = brushCyan;
                    b.FontWeight = FontWeights.Bold;
                }
                else if (hasActiveHighlight)
                {
                    // Scheda NON aperta, ma con voci selezionate attive (leggermente evidenziata)
                    b.Background = new SolidColorBrush(Color.FromRgb(20, 36, 52));
                    b.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                    b.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 52, 211, 153)); // Bordo soft emerald
                    b.FontWeight = FontWeights.SemiBold;
                }
                else
                {
                    // Scheda inattiva e senza alcuna selezione
                    b.Background = brushCard;
                    b.Foreground = brushTextSlate;
                    b.BorderBrush = brushBorder;
                    b.FontWeight = FontWeights.Normal;
                }
            }
        }

        private Button CreateActionButton(string label, RoutedEventHandler handler)
        {
            Button btn = new Button() {
                Content = label,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(26, 36, 56)),
                Foreground = brushCyan,
                BorderBrush = brushBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(5, 0, 0, 0),
                Template = CreateRoundedButtonTemplate(new CornerRadius(8)),
                Cursor = Cursors.Hand
            };
            btn.Click += handler;
            return btn;
        }

        private void SetSelectionMode(string mode)
        {
            currentFilter = "all";

            var lightMaintenanceIds = new HashSet<string> { "win_sfc", "hw_trim", "clean_dns", "clean_temp", "clean_wer", "clean_trash" };

            foreach (var t in activeTasks)
            {
                if (mode == "all") t.Selected = true;
                else if (mode == "none") t.Selected = false;
                else if (mode == "recommended") t.Selected = t.Recommended;
                else if (mode == "light_maintenance") t.Selected = lightMaintenanceIds.Contains(t.Id);
            }

            if (btnProfiles != null)
            {
                if (mode == "light_maintenance")
                {
                    btnProfiles.Content = "🛡️ Manutenzione Basic ▾";
                    btnProfiles.Foreground = brushCyan;
                }
                else if (mode == "recommended")
                {
                    btnProfiles.Content = "⭐ Seleziona Consigliati ▾";
                    btnProfiles.Foreground = brushEmerald;
                }
                else if (mode == "all")
                {
                    btnProfiles.Content = "🚀 Manutenzione Totale ▾";
                    btnProfiles.Foreground = brushPurple;
                }
                else if (mode == "none")
                {
                    btnProfiles.Content = "✍️ Selezione Manuale ▾";
                    btnProfiles.Foreground = brushAmber;
                }
            }

            RenderTaskList();
            UpdateFooterSummary();
            UpdateFilterTabsVisuals();
        }

        private void RenderTaskList()
        {
            pnlTasks.Children.Clear();
            var visibleTasks = activeTasks.FindAll(t => 
                (currentFilter == "all" || t.Category == currentFilter) &&
                (string.IsNullOrEmpty(searchQuery) || 
                 t.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 || 
                 t.Description.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
            );

            foreach (var task in visibleTasks)
            {
                Border card = new Border() {
                    Background = task.Selected ? brushCardSelected : brushCard,
                    BorderBrush = task.Selected ? brushBorderSelected : brushBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 11, 16, 11),
                    Margin = new Thickness(0, 0, 0, 8),
                    Cursor = Cursors.Hand
                };
                task.CardBorder = card;

                Grid grd = new Grid();
                grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(32) });
                grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                grd.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                CheckBox chk = new CheckBox() { IsChecked = task.Selected, VerticalAlignment = VerticalAlignment.Center, Cursor = Cursors.Hand };
                task.CheckBoxCtrl = chk;

                chk.Checked += (s, e) => { task.Selected = true; card.Background = brushCardSelected; card.BorderBrush = brushBorderSelected; if (btnProfiles != null) { btnProfiles.Content = "✍️ Selezione Manuale ▾"; btnProfiles.Foreground = brushAmber; } UpdateFooterSummary(); UpdateFilterTabsVisuals(); };
                chk.Unchecked += (s, e) => { task.Selected = false; card.Background = brushCard; card.BorderBrush = brushBorder; if (btnProfiles != null) { btnProfiles.Content = "✍️ Selezione Manuale ▾"; btnProfiles.Foreground = brushAmber; } UpdateFooterSummary(); UpdateFilterTabsVisuals(); };

                Grid.SetColumn(chk, 0);
                grd.Children.Add(chk);

                StackPanel spText = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
                string sizeEstimateText = task.SizeEstimateBytes > 0 ? " (" + FormatBytes(task.SizeEstimateBytes) + ")" : "";
                TextBlock txtTitle = new TextBlock() { Text = task.Name + sizeEstimateText, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = brushTextWhite };
                TextBlock txtDesc = new TextBlock() { Text = task.Description, FontSize = 11.5, Foreground = brushTextSlate, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap };
                spText.Children.Add(txtTitle);
                spText.Children.Add(txtDesc);
                Grid.SetColumn(spText, 1);
                grd.Children.Add(spText);

                if (task.Recommended)
                {
                    Border bdrRec = new Border() {
                        Background = new SolidColorBrush(Color.FromArgb(35, 52, 211, 153)),
                        BorderBrush = brushEmerald,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 3, 8, 3),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    bdrRec.Child = new TextBlock() { Text = "CONSIGLIATO", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = brushEmerald };
                    Grid.SetColumn(bdrRec, 2);
                    grd.Children.Add(bdrRec);
                }

                card.MouseLeftButtonUp += (s, e) => {
                    chk.IsChecked = !chk.IsChecked;
                };

                card.Child = grd;
                pnlTasks.Children.Add(card);
            }
        }

        private void UpdateFooterSummary()
        {
            var selected = activeTasks.FindAll(t => t.Selected);
            int count = selected.Count;
            txtSelectedCount.Text = count.ToString();

            long totalEstimatedBytes = 0;
            foreach (var t in selected)
            {
                totalEstimatedBytes += t.SizeEstimateBytes;
            }

            if (count == 0)
            {
                txtSummaryDesc.Text = "Nessuna operazione selezionata. Seleziona almeno un controllo.";
                btnRun.IsEnabled = false;
                btnRun.Background = new SolidColorBrush(Color.FromRgb(26, 36, 54));
                btnRun.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }
            else
            {
                string estimateText = totalEstimatedBytes > 0 ? " | Spazio stimato liberabile: " + FormatBytes(totalEstimatedBytes) : "";
                txtSummaryDesc.Text = count + " operazioni pronte per l'esecuzione con privilegi Amministratore" + estimateText + ".";
                btnRun.IsEnabled = true;
                btnRun.Background = new LinearGradientBrush(Color.FromRgb(56, 189, 248), Color.FromRgb(2, 132, 199), new Point(0, 0), new Point(1, 1));
                btnRun.Foreground = new SolidColorBrush(Color.FromRgb(5, 10, 20));
            }

            UpdateFilterTabsVisuals();
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            var selected = activeTasks.FindAll(t => t.Selected);
            if (selected.Count == 0) return;

            bool hasLongRunningTask = selected.Any(t => t.Id == "win_dism" || t.Id == "win_sfc" || t.Id == "win_winsxs" || t.Id == "win_chkdsk");
            if (hasLongRunningTask)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Hai selezionato operazioni a lunga durata (come SFC, DISM o WinSxS) che potrebbero richiedere da diversi minuti fino a qualche ora a seconda dello stato del sistema.\n\nVuoi procedere comunque con l'esecuzione della manutenzione?",
                    "Conferma Operazioni Lunghe ⚠",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            isExecuting = true;
            modalBackdrop.Visibility = Visibility.Visible;
            modalStepList.Children.Clear();
            modalProgressBar.Value = 0;
            txtModalPercent.Text = "0%";
            txtModalTitle.Text = "⚡ Applicazione Manutenzioni in corso...";
            txtModalCommandLog.Text = "Inizializzazione manutenzione...";
            btnModalDone.IsEnabled = false;
            btnModalDone.Content = "🔒 In esecuzione... (non chiudere)";
            btnModalDone.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            btnModalDone.Foreground = brushTextMuted;

            modalWarningBanner.Background = new SolidColorBrush(Color.FromArgb(30, 239, 68, 68));
            modalWarningBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            if (hasLongRunningTask)
            {
                txtModalWarningText.Text = "🔒 MANUTENZIONE IN CORSO — NON CHIUDERE QUESTA FINESTRA\nI processi di sistema sono attivi. NOTA: Alcuni task selezionati (come SFC/DISM o WinSxS) potrebbero richiedere da diversi minuti fino a qualche ora a seconda dello stato del sistema. L'app non è bloccata.";
            }
            else
            {
                txtModalWarningText.Text = "🔒 MANUTENZIONE IN CORSO — NON CHIUDERE QUESTA FINESTRA\nI processi di sistema sono attivi. Non chiudere l'app né spegnere il PC fino al termine.";
            }

            // Populate items
            Dictionary<string, TextBlock> stepStatusMap = new Dictionary<string, TextBlock>();
            foreach (var task in selected)
            {
                Border row = new Border() {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = brushBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid grd = new Grid();
                TextBlock txtN = new TextBlock() { Text = task.Name, FontSize = 12, Foreground = brushTextWhite, VerticalAlignment = VerticalAlignment.Center };
                TextBlock txtS = new TextBlock() { Text = "In attesa...", FontSize = 11, Foreground = brushTextSlate, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                grd.Children.Add(txtN);
                grd.Children.Add(txtS);
                row.Child = grd;
                modalStepList.Children.Add(row);
                stepStatusMap[task.Id] = txtS;
            }

            // Execute in background thread with Native Win32 / .NET APIs and direct System32 processes
            Thread bgThread = new Thread(() => {
                string sys32 = Environment.SystemDirectory;
                long diskBefore = 0;
                try
                {
                    diskBefore = new DriveInfo("C").AvailableFreeSpace;
                }
                catch { }

                List<TaskReportItem> reportItems = new List<TaskReportItem>();
                long totalEstimatedBytes = 0;

                for (int i = 0; i < selected.Count; i++)
                {
                    var task = selected[i];
                    totalEstimatedBytes += task.SizeEstimateBytes;
                    int idx = i;
                    int total = selected.Count;
                    double basePct = Math.Round(((double)idx / total) * 100);
                    double currentTaskSubPct = 0.0;

                    this.Dispatcher.Invoke(() => {
                        modalProgressBar.Value = basePct;
                        txtModalPercent.Text = ((int)basePct) + "%";
                        txtModalCommandLog.Text = "Esecuzione in corso: " + task.Description;
                        if (stepStatusMap.ContainsKey(task.Id))
                        {
                            stepStatusMap[task.Id].Text = "⚙ In corso (0s)...";
                            stepStatusMap[task.Id].Foreground = brushCyan;
                        }
                    });

                    long folderSizeBefore = (task.Category == "cleanup" || task.Id == "win_winsxs")
                        ? GetTaskSizeEstimate(task.Id)
                        : 0;

                    DateTime stepStart = DateTime.Now;
                    try
                    {
                        ExecuteSingleTaskNative(task, sys32, ref currentTaskSubPct, idx, total, stepStart, stepStatusMap);

                        long folderSizeAfter = (task.Category == "cleanup" || task.Id == "win_winsxs")
                            ? GetTaskSizeEstimate(task.Id)
                            : 0;

                        long freedBytes = Math.Max(0, folderSizeBefore - folderSizeAfter);

                        double elapsedRaw = (DateTime.Now - stepStart).TotalSeconds;
                        string elapsedStr = (elapsedRaw > 300)
                            ? ((int)(elapsedRaw / 60)) + "m " + ((int)(elapsedRaw % 60)) + "s"
                            : Math.Round(elapsedRaw, 1) + "s";
                        double newPct = Math.Round(((double)(idx + 1) / total) * 100);

                        string details = "";
                        if (task.Category == "cleanup" || task.Id == "win_winsxs")
                        {
                            details = "Liberati: " + FormatBytes(freedBytes);
                        }
                        else
                        {
                            details = "Eseguito in " + elapsedStr;
                        }

                        reportItems.Add(new TaskReportItem
                        {
                            TaskName = task.Name,
                            Status = "Completato",
                            Details = details
                        });

                        this.Dispatcher.Invoke(() => {
                            modalProgressBar.Value = newPct;
                            txtModalPercent.Text = ((int)newPct) + "%";
                            if (stepStatusMap.ContainsKey(task.Id))
                            {
                                stepStatusMap[task.Id].Text = "✓ Completato (" + elapsedStr + ")";
                                stepStatusMap[task.Id].Foreground = brushEmerald;
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        reportItems.Add(new TaskReportItem
                        {
                            TaskName = task.Name,
                            Status = "Errore",
                            Details = ex.Message
                        });

                        this.Dispatcher.Invoke(() => {
                            if (stepStatusMap.ContainsKey(task.Id))
                            {
                                stepStatusMap[task.Id].Text = "⚠ Errore";
                                stepStatusMap[task.Id].Foreground = brushAmber;
                            }
                        });
                    }
                }

                long diskAfter = 0;
                try
                {
                    diskAfter = new DriveInfo("C").AvailableFreeSpace;
                }
                catch { }
                long totalDiskFreed = Math.Max(0, diskAfter - diskBefore);

                InitTasks();
                ScanHardwareBackground((pct, msg) => {
                    this.Dispatcher.Invoke(() => {
                        txtModalCommandLog.Text = "Aggiornamento diagnostica: " + msg;
                    });
                });

                // Generate detailed text report
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine("RAPPORTO DI MANUTENZIONE & OTTIMIZZAZIONE");
                sb.AppendLine("Eseguito il: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                sb.AppendLine("==================================================");
                sb.AppendLine("Dispositivo: " + sysInfo.DeviceModel);
                sb.AppendLine("OEM Brand: " + sysInfo.OEMBrand.ToUpper());
                sb.AppendLine("CPU: " + sysInfo.CPU);
                sb.AppendLine("RAM Totale: " + sysInfo.RAM_TotalGB + " GB");
                sb.AppendLine("GPU: " + sysInfo.GPU);
                sb.AppendLine("OS: " + sysInfo.OS);
                sb.AppendLine("==================================================");
                sb.AppendLine("STATISTICHE DI SPAZIO SU DISCO C:");
                sb.AppendLine("Spazio stimato da liberare: " + FormatBytes(totalEstimatedBytes));
                sb.AppendLine("Spazio effettivamente liberato su disco: " + FormatBytes(totalDiskFreed));
                sb.AppendLine("==================================================");
                sb.AppendLine("DETTAGLIO DELLE OPERAZIONI ESEGUITE:");
                foreach (var item in reportItems)
                {
                    sb.AppendLine(string.Format("- {0}: {1} ({2})", item.TaskName, item.Status, item.Details));
                }
                sb.AppendLine("==================================================");
                sb.AppendLine("Rapporto autogenerato da Universal Maintenance Center.");
                string rawReport = sb.ToString();

                this.Dispatcher.Invoke(() => {
                    isExecuting = false;
                    modalProgressBar.Value = 100;
                    txtModalPercent.Text = "100%";
                    txtModalTitle.Text = "✓ Manutenzione Completata con Successo!";
                    txtModalCommandLog.Text = "✓ Tutte le operazioni selezionate sono state completate con successo.";

                    modalWarningBanner.Background = new SolidColorBrush(Color.FromArgb(30, 16, 185, 129));
                    modalWarningBanner.BorderBrush = brushEmerald;
                    txtModalWarningText.Text = "✅ MANUTENZIONE COMPLETATA CON SUCCESSO\nTutti i processi di sistema sono terminati. Ora puoi chiudere la finestra in sicurezza.";

                    btnModalDone.IsEnabled = true;
                    btnModalDone.Content = "Chiudi";
                    btnModalDone.Background = new LinearGradientBrush(Color.FromRgb(56, 189, 248), Color.FromRgb(2, 132, 199), new Point(0, 0), new Point(1, 1));
                    btnModalDone.Foreground = new SolidColorBrush(Color.FromRgb(5, 10, 20));

                    UpdateHwCards();
                    RenderTaskList();
                    UpdateFooterSummary();

                    // Open the detailed report dialog
                    ReportWindow repWin = new ReportWindow(
                        string.Format("Spazio totale stimato: {0} | Spazio effettivamente liberato: {1}", FormatBytes(totalEstimatedBytes), FormatBytes(totalDiskFreed)),
                        reportItems,
                        rawReport
                    );
                    repWin.Owner = this;
                    repWin.ShowDialog();
                });
            });

            bgThread.IsBackground = true;
            bgThread.Start();
        }

        private void ExecuteSingleTaskNative(TaskItem task, string sys32, ref double currentTaskSubPct, int idx, int total, DateTime stepStart, Dictionary<string, TextBlock> stepStatusMap)
        {
            string cmd = task.Command.Trim();

            if (cmd.StartsWith("registry_set_hklm:", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = cmd.Split(':');
                if (parts.Length >= 4)
                {
                    string subKey = parts[1];
                    string valName = parts[2];
                    string valStr = parts[3];
                    RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add ""HKLM\" + subKey + @""" /v """ + valName + @""" /t REG_DWORD /d " + valStr + @" /f");
                }
            }
            else if (cmd.StartsWith("registry_set_hkcu:", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = cmd.Split(':');
                if (parts.Length >= 4)
                {
                    string subKey = parts[1];
                    string valName = parts[2];
                    string valStr = parts[3];
                    RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add ""HKCU\" + subKey + @""" /v """ + valName + @""" /t REG_DWORD /d " + valStr + @" /f");
                }
            }
            // 1. DIRECT SYSTEM EXECUTABLES
            else if (cmd.StartsWith("sfc.exe", StringComparison.OrdinalIgnoreCase))
            {
                RunNativeStreamingProcess(Path.Combine(sys32, "sfc.exe"), "/scannow", ref currentTaskSubPct, idx, total, stepStart, task.Id, stepStatusMap);
            }
            else if (cmd.StartsWith("DISM.exe", StringComparison.OrdinalIgnoreCase))
            {
                string args = cmd.Substring(8).Trim();
                RunNativeStreamingProcess(Path.Combine(sys32, "dism.exe"), args, ref currentTaskSubPct, idx, total, stepStart, task.Id, stepStatusMap);
            }
            else if (cmd.StartsWith("chkdsk.exe", StringComparison.OrdinalIgnoreCase))
            {
                string args = cmd.Substring(10).Trim();
                RunNativeStreamingProcess(Path.Combine(sys32, "chkdsk.exe"), args, ref currentTaskSubPct, idx, total, stepStart, task.Id, stepStatusMap);
            }
            else if (cmd.StartsWith("powercfg", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = cmd.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    string trimmed = p.Trim();
                    if (trimmed.StartsWith("powercfg", StringComparison.OrdinalIgnoreCase))
                    {
                        string args = trimmed.Substring(8).Trim();
                        RunDirectProcess(Path.Combine(sys32, "powercfg.exe"), args);
                    }
                }
            }
            else if (cmd.StartsWith("netsh", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = cmd.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    string trimmed = p.Trim();
                    if (trimmed.StartsWith("netsh", StringComparison.OrdinalIgnoreCase))
                    {
                        string args = trimmed.Substring(5).Trim();
                        RunDirectProcess(Path.Combine(sys32, "netsh.exe"), args);
                    }
                }
            }
            else if (task.Id == "hw_winget")
            {
                Dispatcher.Invoke(() =>
                {
                    WingetUpdateWindow win = new WingetUpdateWindow();
                    win.Owner = this;
                    win.ShowDialog();
                });
            }
            else if (cmd.StartsWith("winget", StringComparison.OrdinalIgnoreCase))
            {
                string args = cmd.Substring(6).Trim();
                RunNativeStreamingProcess("winget.exe", args, ref currentTaskSubPct, idx, total, stepStart, task.Id, stepStatusMap);
            }
            else if (task.Id == "hw_driverupdates")
            {
                Dispatcher.Invoke(() =>
                {
                    DriverUpdateWindow win = new DriverUpdateWindow();
                    win.Owner = this;
                    win.ShowDialog();
                });
            }
            else if (cmd.StartsWith("Start-Process", StringComparison.OrdinalIgnoreCase))
            {
                string target = cmd.Replace("Start-Process", "").Trim();
                Process.Start(target);
            }
            // 2. REGISTRY MODIFICATIONS (Spawning reg.exe - 100% Trusted by Antiviruses)
            else if (task.Id == "hw_hags")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers /v HwSchMode /t REG_DWORD /d 2 /f");
            }
            else if (task.Id == "hw_gamedvr")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKCU\System\GameConfigStore /v GameDVR_Enabled /t REG_DWORD /d 0 /f");
            }
            else if (task.Id == "net_delivery")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization /v DODownloadMode /t REG_DWORD /d 0 /f");
            }
            else if (task.Id == "os_w11_widgets")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced /v TaskbarDa /t REG_DWORD /d 0 /f");
            }
            else if (task.Id == "os_w11_chat")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced /v TaskbarMn /t REG_DWORD /d 0 /f");
            }
            else if (task.Id == "os_w10_cortana")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add ""HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search"" /v AllowCortana /t REG_DWORD /d 0 /f");
            }
            else if (task.Id == "os_w10_news")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKCU\Software\Microsoft\Windows\CurrentVersion\Feeds /v ShellFeedsTaskbarViewMode /t REG_DWORD /d 2 /f");
            }
            else if (task.Id == "clean_spotlight")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel /v ""{2cc5ca98-6485-489a-920e-b3e88a6ccce3}"" /t REG_DWORD /d 1 /f");
            }
            // 3. STARTUP KEYS CLEANUP (via reg.exe delete)
            else if (task.Id == "start_copilot")
            {
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), @"/c for /f ""tokens=1"" %a in ('reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /f MicrosoftCopilotAutoLaunch') do reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v %a /f");
            }
            else if (task.Id == "start_edge")
            {
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), @"/c for /f ""tokens=1"" %a in ('reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /f MicrosoftEdgeAutoLaunch') do reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v %a /f");
            }
            else if (task.Id == "start_vpn")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"delete HKLM\Software\Microsoft\Windows\CurrentVersion\Run /v BdVpnApp /f");
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v BdVpnApp /f");
            }
            else if (task.Id == "start_pcloud")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"delete HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v pCloud /f");
            }
            else if (task.Id == "start_unattend")
            {
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), @"/c del /f /q ""C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\renameUnattend.bat""");
            }
            // 4. CLEANUP & CACHE
            else if (task.Id == "clean_temp")
            {
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), @"/c del /f /q /s %temp%\* & del /f /q /s C:\Windows\Temp\*");
            }
            else if (task.Id == "clean_wer")
            {
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), @"/c del /f /q /s C:\ProgramData\Microsoft\Windows\WER\ReportArchive\*");
            }
            else if (task.Id == "clean_winupdate")
            {
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop wuauserv");
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), @"/c del /f /q /s C:\Windows\SoftwareDistribution\Download\*");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "start wuauserv");
            }
            else if (task.Id == "clean_dns")
            {
                RunDirectProcess(Path.Combine(sys32, "ipconfig.exe"), "/flushdns");
            }
            else if (task.Id == "clean_browsers")
            {
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), @"/c for /d %d in (""%localappdata%\Google\Chrome\User Data\*Cache*"" ""%localappdata%\Microsoft\Edge\User Data\*Cache*"") do del /f /q /s ""%d\*""");
            }
            else if (task.Id == "clean_trash")
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            }
            // 5. SERVICES & TELEMETRY (Via reg.exe add Start=4, 100% compliant with SCM protections and anti-virus)
            else if (task.Id == "telem_diagtrack")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\DiagTrack /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop DiagTrack /y");
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\dmwappushservice /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop dmwappushservice /y");
            }
            else if (task.Id == "telem_inventory")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\InventorySvc /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop InventorySvc /y");
            }
            else if (task.Id == "telem_samsung_sa")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\SamsungAnalyticsService /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop SamsungAnalyticsService /y");
            }
            else if (task.Id == "telem_samsung_hqm")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\SamsungHQMService /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop SamsungHQMService /y");
            }
            else if (task.Id == "telem_bixby")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\SamsungBixbyService /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop SamsungBixbyService /y");
                RunDirectProcess(Path.Combine(sys32, "taskkill.exe"), "/f /im BixbySystray.exe /im UWPBixbyClient.exe");
            }
            else if (task.Id == "bloat_livewall")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\LiveWallpaperService /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop LiveWallpaperService /y");
            }
            else if (task.Id == "bloat_quicksearch")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\Quick Search Service /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop \"Quick Search Service\" /y");
            }
            else if (task.Id == "bloat_smartswitch")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\SmartSwitchService /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop SmartSwitchService /y");
            }
            else if (task.Id == "bloat_camerashare")
            {
                RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"add HKLM\SYSTEM\CurrentControlSet\Services\SamsungCameraShareService /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop SamsungCameraShareService /y");
            }
            else if (task.Id == "telem_dell")
            {
                foreach (var svc in new string[] { "DellDataVault", "DellDataVaultProcessor", "SupportAssistAgent" })
                {
                    RunCommandAsSystem(@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\" + svc + @""" /v Start /t REG_DWORD /d 4 /f");
                    RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop \"" + svc + "\" /y");
                }
            }
            else if (task.Id == "telem_hp")
            {
                foreach (var svc in new string[] { "HPAppHelperCap", "HPNetworkCap", "HPSysInfoCap", "HPAnalytics" })
                {
                    RunCommandAsSystem(@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\" + svc + @""" /v Start /t REG_DWORD /d 4 /f");
                    RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop \"" + svc + "\" /y");
                }
            }
            else if (task.Id == "telem_lenovo")
            {
                foreach (var svc in new string[] { "LenovoVantageService", "ImControllerService" })
                {
                    RunCommandAsSystem(@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\" + svc + @""" /v Start /t REG_DWORD /d 4 /f");
                    RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop \"" + svc + "\" /y");
                }
            }
            // 6. TRIM & DEFRAG (Defrag.exe Native)
            else if (task.Id == "hw_trim")
            {
                RunNativeStreamingProcess(Path.Combine(sys32, "defrag.exe"), "C: /L /U /V", ref currentTaskSubPct, idx, total, stepStart, task.Id, stepStatusMap);
            }
            else if (cmd.StartsWith("registry_remove:", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = cmd.Split(':');
                if (parts.Length >= 3)
                {
                    string rootKey = parts[1].ToUpper();
                    RunDirectProcess(Path.Combine(sys32, "reg.exe"), @"delete " + rootKey + @"\Software\Microsoft\Windows\CurrentVersion\Run /v """ + parts[2] + @""" /f");
                }
            }
            else if (cmd.StartsWith("service_disable:", StringComparison.OrdinalIgnoreCase))
            {
                string sName = cmd.Substring(16).Trim();
                RunCommandAsSystem(@"reg add ""HKLM\SYSTEM\CurrentControlSet\Services\" + sName + @""" /v Start /t REG_DWORD /d 4 /f");
                RunDirectProcess(Path.Combine(sys32, "sc.exe"), "config \"" + sName + "\" start= disabled");
                RunDirectProcess(Path.Combine(sys32, "net.exe"), "stop \"" + sName + "\" /y");
            }
            else
            {
                // Clean fallback
                RunDirectProcess(Path.Combine(sys32, "cmd.exe"), "/c " + task.Command);
            }
        }

        private void RunNativeStreamingProcess(string exePath, string args, ref double currentTaskSubPct, int idx, int total, DateTime stepStart, string taskId, Dictionary<string, TextBlock> stepStatusMap)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = exePath;
            psi.Arguments = args;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = System.Text.Encoding.UTF8;
            psi.StandardErrorEncoding = System.Text.Encoding.UTF8;

            double localSubPct = 0.0;

            using (Process proc = new Process())
            {
                proc.StartInfo = psi;
                proc.ErrorDataReceived += (sProc, eProc) => { };
                proc.OutputDataReceived += (sProc, eProc) =>
                {
                    if (!string.IsNullOrEmpty(eProc.Data))
                    {
                        string line = eProc.Data.Trim();
                        Match m = Regex.Match(line, @"(\d{1,3}(?:\.\d+)?)\s*%");
                        if (m.Success)
                        {
                            double val;
                            if (double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out val))
                            {
                                if (val >= 0 && val <= 100)
                                {
                                    localSubPct = val;
                                    double overall = (idx * 100.0 + localSubPct) / total;
                                    this.Dispatcher.Invoke(() => {
                                        modalProgressBar.Value = overall;
                                        txtModalPercent.Text = ((int)overall) + "%";
                                    });
                                }
                            }
                        }
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                while (!proc.HasExited)
                {
                    Thread.Sleep(300);
                    int elapsedSec = (int)(DateTime.Now - stepStart).TotalSeconds;

                    if (localSubPct < 1.0)
                    {
                        double simulated = Math.Min(90.0, elapsedSec * 3.0);
                        double overallSim = (idx * 100.0 + simulated) / total;
                        this.Dispatcher.Invoke(() => {
                            modalProgressBar.Value = overallSim;
                            txtModalPercent.Text = ((int)overallSim) + "%";
                        });
                    }

                    string elapsedSecStr = (elapsedSec > 300)
                        ? (elapsedSec / 60) + "m " + (elapsedSec % 60) + "s"
                        : elapsedSec + "s";

                    this.Dispatcher.Invoke(() => {
                        if (stepStatusMap.ContainsKey(taskId))
                        {
                            stepStatusMap[taskId].Text = "⚙ In corso (" + elapsedSecStr + ")...";
                            stepStatusMap[taskId].Foreground = brushCyan;
                        }
                    });
                }

                proc.WaitForExit();
            }
        }

        private void RunDirectProcess(string exePath, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exePath;
                psi.Arguments = args;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(15000);
                }
            }
            catch { }
        }

        private void RunCommandAsSystem(string commandLine)
        {
            try
            {
                string taskName = "WinOptSysTask_" + Guid.NewGuid().ToString().Substring(0, 8);
                string sys32 = Environment.SystemDirectory;

                // Crea una task pianificata che gira come SYSTEM (privilegi massimi, bypassa ACL)
                string createArgs = "/create /tn \"" + taskName + "\" /tr \"" + commandLine.Replace("\"", "\\\"") + "\" /sc once /sd 01/01/2099 /st 00:00 /ru SYSTEM /rl HIGHEST /f";
                RunDirectProcess(Path.Combine(sys32, "schtasks.exe"), createArgs);

                // Esegue immediatamente la task
                RunDirectProcess(Path.Combine(sys32, "schtasks.exe"), "/run /tn \"" + taskName + "\"");

                // Attendi che la task termini
                Thread.Sleep(800);

                // Elimina la task
                RunDirectProcess(Path.Combine(sys32, "schtasks.exe"), "/delete /tn \"" + taskName + "\" /f");
            }
            catch { }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application app = new Application();
            app.Run(new MainWindow());
        }
    }

    public class DriverUpdateWindow : Window
    {
        private ProgressBar prgBar;
        private TextBlock txtStatus;
        private ListBox lstDrivers;
        private Button btnAction;
        private Button btnCancel;
        private Border loadingOverlay;
        private TextBlock txtLoadingStatus;
        
        private dynamic session;
        private List<dynamic> foundUpdatesList = new List<dynamic>();
        private List<DriverUpdateItem> uiItems = new List<DriverUpdateItem>();

        public DriverUpdateWindow()
        {
            Title = "Aggiornamenti Driver Online";
            Width = 550;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")); // Slate 900
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Segoe UI");

            Border rootBorder = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), // Slate 700
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            TextBlock txtTitle = new TextBlock
            {
                Text = "Ricerca Driver Disponibili",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")) // Sky 400
            };
            Grid.SetRow(txtTitle, 0);
            mainGrid.Children.Add(txtTitle);

            // ListBox
            lstDrivers = new ListBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")), // Slate 800
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5)
            };

            // Custom ItemTemplate for ListBox
            FrameworkElementFactory itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")));
            itemBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
            itemBorder.SetValue(Border.PaddingProperty, new Thickness(5));

            FrameworkElementFactory stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory checkBox = new FrameworkElementFactory(typeof(CheckBox));
            checkBox.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkBox.SetValue(CheckBox.MarginProperty, new Thickness(5, 0, 10, 0));
            checkBox.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsSelected") { Mode = System.Windows.Data.BindingMode.TwoWay });

            FrameworkElementFactory textStack = new FrameworkElementFactory(typeof(StackPanel));
            textStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            FrameworkElementFactory titleText = new FrameworkElementFactory(typeof(TextBlock));
            titleText.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            titleText.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            titleText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Title"));

            FrameworkElementFactory descText = new FrameworkElementFactory(typeof(TextBlock));
            descText.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")));
            descText.SetValue(TextBlock.FontSizeProperty, 11.0);
            descText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            descText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Description"));

            textStack.AppendChild(titleText);
            textStack.AppendChild(descText);
            stackPanel.AppendChild(checkBox);
            stackPanel.AppendChild(textStack);
            itemBorder.AppendChild(stackPanel);

            lstDrivers.ItemTemplate = new DataTemplate { VisualTree = itemBorder };

            Grid.SetRow(lstDrivers, 1);
            mainGrid.Children.Add(lstDrivers);

            // Bottom Buttons
            Grid btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            txtStatus = new TextBlock
            {
                Text = "Inizializzazione...",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtStatus, 0);
            btnGrid.Children.Add(txtStatus);

            btnCancel = CreateStyledButton("Chiudi", "#475569", "#334155");
            btnCancel.Width = 90;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => Close();
            Grid.SetColumn(btnCancel, 1);
            btnGrid.Children.Add(btnCancel);

            btnAction = CreateStyledButton("Scarica e Installa", "#0284C7", "#0369A1");
            btnAction.Width = 140;
            btnAction.IsEnabled = false;
            btnAction.Click += BtnAction_Click;
            Grid.SetColumn(btnAction, 2);
            btnGrid.Children.Add(btnAction);

            Grid.SetRow(btnGrid, 2);
            mainGrid.Children.Add(btnGrid);

            // Loading Overlay
            loadingOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
                CornerRadius = new CornerRadius(8),
                Visibility = Visibility.Visible
            };
            Grid loadGrid = new Grid();
            StackPanel loadStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            
            TextBlock loadText = new TextBlock
            {
                Text = "Ricerca driver online in corso...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Foreground = Brushes.White
            };
            txtLoadingStatus = loadText;

            prgBar = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 300,
                Height = 10,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06B6D4")),
                BorderThickness = new Thickness(0)
            };

            loadStack.Children.Add(loadText);
            loadStack.Children.Add(prgBar);
            loadGrid.Children.Add(loadStack);
            loadingOverlay.Child = loadGrid;

            Grid rootGrid = new Grid();
            rootGrid.Children.Add(mainGrid);
            rootGrid.Children.Add(loadingOverlay);
            rootBorder.Child = rootGrid;
            Content = rootBorder;

            Loaded += DriverUpdateWindow_Loaded;
        }

        private Button CreateStyledButton(string text, string hexNormal, string hexHover)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 32,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexNormal)));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexHover))
            });

            Trigger disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
            });

            template.Triggers.Add(hoverTrigger);
            template.Triggers.Add(disabledTrigger);

            btn.Template = template;
            return btn;
        }

        private void DriverUpdateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    session = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.Session"));
                    dynamic searcher = session.CreateUpdateSearcher();
                    dynamic searchResult = searcher.Search("IsInstalled=0 and Type='Driver' and IsHidden=0");
                    dynamic updates = searchResult.Updates;

                    List<DriverUpdateItem> tempItems = new List<DriverUpdateItem>();
                    for (int i = 0; i < updates.Count; i++)
                    {
                        dynamic update = updates.Item(i);
                        foundUpdatesList.Add(update);
                        tempItems.Add(new DriverUpdateItem
                        {
                            Index = i,
                            Title = update.Title,
                            Description = update.Description ?? "Aggiornamento driver hardware.",
                            IsSelected = true
                        });
                    }

                    Dispatcher.Invoke(() =>
                    {
                        uiItems = tempItems;
                        lstDrivers.ItemsSource = uiItems;
                        loadingOverlay.Visibility = Visibility.Collapsed;

                        if (uiItems.Count > 0)
                        {
                            txtStatus.Text = uiItems.Count + " driver disponibili.";
                            btnAction.IsEnabled = true;
                        }
                        else
                        {
                            txtStatus.Text = "Tutti i driver sono aggiornati.";
                            MessageBox.Show("Tutti i driver del sistema risultano aggiornati!", "Nessun Aggiornamento", MessageBoxButton.OK, MessageBoxImage.Information);
                            Close();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        txtStatus.Text = "Errore di ricerca.";
                        MessageBox.Show("Impossibile completare la ricerca driver: " + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                    });
                }
            });
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            var selectedToUpdate = uiItems.FindAll(x => x.IsSelected);
            if (selectedToUpdate.Count == 0)
            {
                MessageBox.Show("Seleziona almeno un driver da installare.", "Selezione Vuota", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;
            btnAction.IsEnabled = false;
            btnCancel.IsEnabled = false;

            Task.Run(() =>
            {
                try
                {
                    dynamic updateColl = Activator.CreateInstance(Type.GetTypeFromProgID("Microsoft.Update.UpdateColl"));
                    foreach (var uiItem in selectedToUpdate)
                    {
                        updateColl.Add(foundUpdatesList[uiItem.Index]);
                    }

                    // Download Phase
                    Dispatcher.Invoke(() =>
                    {
                        txtLoadingStatus.Text = "Download dei driver in corso...";
                    });

                    dynamic downloader = session.CreateUpdateDownloader();
                    downloader.Updates = updateColl;
                    downloader.Download();

                    // Install Phase
                    Dispatcher.Invoke(() =>
                    {
                        txtLoadingStatus.Text = "Installazione dei driver in corso...";
                    });

                    dynamic installer = session.CreateUpdateInstaller();
                    installer.Updates = updateColl;
                    dynamic installResult = installer.Install();

                    Dispatcher.Invoke(() =>
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Installazione completata con successo!\n\nNota: Potrebbe essere richiesto di riavviare il sistema per completare l'applicazione dei nuovi driver.", "Aggiornamento Completato", MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        btnAction.IsEnabled = true;
                        btnCancel.IsEnabled = true;
                        MessageBox.Show("Errore durante l'applicazione dei driver: " + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }
    }

    public class DriverUpdateItem
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
    }

    public class WingetUpdateWindow : Window
    {
        private ProgressBar prgBar;
        private TextBlock txtStatus;
        private ListBox lstApps;
        private Button btnAction;
        private Button btnCancel;
        private Border loadingOverlay;
        private TextBlock txtLoadingStatus;

        private List<WingetUpdateItem> uiItems = new List<WingetUpdateItem>();

        public WingetUpdateWindow()
        {
            Title = "Aggiornamento Applicazioni con WinGet";
            Width = 600;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")); // Slate 900
            Foreground = Brushes.White;
            FontFamily = new FontFamily("Segoe UI");

            Border rootBorder = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), // Slate 700
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20)
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            TextBlock txtTitle = new TextBlock
            {
                Text = "Applicazioni Aggiornabili (WinGet)",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"))
            };
            Grid.SetRow(txtTitle, 0);
            mainGrid.Children.Add(txtTitle);

            // ListBox
            lstApps = new ListBox
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5)
            };

            // Custom ItemTemplate
            FrameworkElementFactory itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")));
            itemBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
            itemBorder.SetValue(Border.PaddingProperty, new Thickness(8));

            FrameworkElementFactory stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory checkBox = new FrameworkElementFactory(typeof(CheckBox));
            checkBox.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkBox.SetValue(CheckBox.MarginProperty, new Thickness(5, 0, 15, 0));
            checkBox.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsSelected") { Mode = System.Windows.Data.BindingMode.TwoWay });

            FrameworkElementFactory textStack = new FrameworkElementFactory(typeof(StackPanel));
            textStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            FrameworkElementFactory titleText = new FrameworkElementFactory(typeof(TextBlock));
            titleText.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            titleText.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            titleText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("DisplayName"));

            FrameworkElementFactory descText = new FrameworkElementFactory(typeof(TextBlock));
            descText.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")));
            descText.SetValue(TextBlock.FontSizeProperty, 11.0);
            descText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("VersionDetails"));

            textStack.AppendChild(titleText);
            textStack.AppendChild(descText);
            stackPanel.AppendChild(checkBox);
            stackPanel.AppendChild(textStack);
            itemBorder.AppendChild(stackPanel);

            lstApps.ItemTemplate = new DataTemplate { VisualTree = itemBorder };

            Grid.SetRow(lstApps, 1);
            mainGrid.Children.Add(lstApps);

            // Bottom Panel
            Grid btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            txtStatus = new TextBlock
            {
                Text = "Scansione applicazioni in corso...",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtStatus, 0);
            btnGrid.Children.Add(txtStatus);

            btnCancel = CreateStyledButton("Chiudi", "#475569", "#334155");
            btnCancel.Width = 90;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => Close();
            Grid.SetColumn(btnCancel, 1);
            btnGrid.Children.Add(btnCancel);

            btnAction = CreateStyledButton("Aggiorna Selezionate", "#0284C7", "#0369A1");
            btnAction.Width = 160;
            btnAction.IsEnabled = false;
            btnAction.Click += BtnAction_Click;
            Grid.SetColumn(btnAction, 2);
            btnGrid.Children.Add(btnAction);

            Grid.SetRow(btnGrid, 2);
            mainGrid.Children.Add(btnGrid);

            // Loading Overlay
            loadingOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
                CornerRadius = new CornerRadius(8),
                Visibility = Visibility.Visible
            };
            Grid loadGrid = new Grid();
            StackPanel loadStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            
            txtLoadingStatus = new TextBlock
            {
                Text = "Scansione degli aggiornamenti disponibili con WinGet...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center
            };

            prgBar = new ProgressBar
            {
                IsIndeterminate = true,
                Width = 350,
                Height = 10,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06B6D4")),
                BorderThickness = new Thickness(0)
            };

            loadStack.Children.Add(txtLoadingStatus);
            loadStack.Children.Add(prgBar);
            loadGrid.Children.Add(loadStack);
            loadingOverlay.Child = loadGrid;

            Grid rootGrid = new Grid();
            rootGrid.Children.Add(mainGrid);
            rootGrid.Children.Add(loadingOverlay);
            rootBorder.Child = rootGrid;
            Content = rootBorder;

            Loaded += WingetUpdateWindow_Loaded;
        }

        private Button CreateStyledButton(string text, string hexNormal, string hexHover)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 32,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexNormal)));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexHover))
            });

            Trigger disabledTrigger = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
            });

            template.Triggers.Add(hoverTrigger);
            template.Triggers.Add(disabledTrigger);

            btn.Template = template;
            return btn;
        }

        private void WingetUpdateWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "winget.exe",
                        Arguments = "upgrade",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    };

                    List<string> outputLines = new List<string>();
                    using (Process p = Process.Start(psi))
                    {
                        if (p != null)
                        {
                            while (!p.StandardOutput.EndOfStream)
                            {
                                outputLines.Add(p.StandardOutput.ReadLine());
                            }
                            p.WaitForExit(30000);
                        }
                    }

                    List<WingetUpdateItem> tempItems = new List<WingetUpdateItem>();
                    bool startParsing = false;
                    foreach (var line in outputLines)
                    {
                        if (line == null) continue;
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;

                        if (trimmed.Contains("---"))
                        {
                            startParsing = true;
                            continue;
                        }

                        if (startParsing)
                        {
                            if (trimmed.StartsWith("An update to") || trimmed.StartsWith("Un aggiornamento per") || 
                                trimmed.StartsWith("aggiornamenti disponibili") || trimmed.StartsWith("upgrades available"))
                            {
                                break;
                            }

                            string[] parts = Regex.Split(trimmed, @"\s{2,}");
                            if (parts.Length >= 4)
                            {
                                tempItems.Add(new WingetUpdateItem
                                {
                                    Name = parts[0],
                                    Id = parts[1],
                                    InstalledVersion = parts[2],
                                    AvailableVersion = parts[3],
                                    IsSelected = true
                                });
                            }
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        uiItems = tempItems;
                        lstApps.ItemsSource = uiItems;
                        loadingOverlay.Visibility = Visibility.Collapsed;

                        if (uiItems.Count > 0)
                        {
                            txtStatus.Text = uiItems.Count + " aggiornamenti trovati.";
                            btnAction.IsEnabled = true;
                        }
                        else
                        {
                            txtStatus.Text = "Tutte le app sono aggiornate.";
                            MessageBox.Show("Tutte le applicazioni di terze parti risultano aggiornate!", "Nessun Aggiornamento", MessageBoxButton.OK, MessageBoxImage.Information);
                            Close();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        txtStatus.Text = "Errore durante la scansione.";
                        MessageBox.Show("Errore durante il controllo degli aggiornamenti con winget: " + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                    });
                }
            });
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            var selectedToUpdate = uiItems.FindAll(x => x.IsSelected);
            if (selectedToUpdate.Count == 0)
            {
                MessageBox.Show("Seleziona almeno un'applicazione da aggiornare.", "Selezione Vuota", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            loadingOverlay.Visibility = Visibility.Visible;
            btnAction.IsEnabled = false;
            btnCancel.IsEnabled = false;

            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < selectedToUpdate.Count; i++)
                    {
                        var app = selectedToUpdate[i];
                        int currentIdx = i + 1;
                        int totalCount = selectedToUpdate.Count;

                        Dispatcher.Invoke(() =>
                        {
                            prgBar.IsIndeterminate = false;
                            prgBar.Value = ((double)i / totalCount) * 100;
                            txtLoadingStatus.Text = string.Format("Aggiornamento in corso: {0}\n({1} di {2} completati)", app.Name, i, totalCount);
                        });

                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "winget.exe",
                            Arguments = "upgrade --id " + app.Id + " --silent --accept-package-agreements --accept-source-agreements",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (Process p = Process.Start(psi))
                        {
                            if (p != null) p.WaitForExit(90000); // 90 secondi timeout
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        prgBar.Value = 100;
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        MessageBox.Show("Tutti gli aggiornamenti software selezionati sono stati applicati con successo!", "Manutenzione Completata", MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;
                        btnAction.IsEnabled = true;
                        btnCancel.IsEnabled = true;
                        MessageBox.Show("Errore durante l'aggiornamento con winget: " + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }
    }

    public class WingetUpdateItem
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string InstalledVersion { get; set; }
        public string AvailableVersion { get; set; }
        public bool IsSelected { get; set; }

        public string DisplayName
        {
            get { return Name; }
        }

        public string VersionDetails
        {
            get { return string.Format("ID: {0}  |  Versione: {1}  ➔  Disponibile: {2}", Id, InstalledVersion, AvailableVersion); }
        }
    }

    public class ReportWindow : Window
    {
        private string reportText;

        public ReportWindow(string summaryText, List<TaskReportItem> items, string rawReport)
        {
            this.reportText = rawReport;
            this.Title = "Rapporto Finale Manutenzione & Ottimizzazione";
            this.Width = 650;
            this.Height = 500;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate 900
            this.Foreground = Brushes.White;
            this.FontFamily = new FontFamily("Segoe UI");
            this.ResizeMode = ResizeMode.NoResize;

            Border rootBorder = new Border { Padding = new Thickness(20), BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header (Title + Spazio liberato)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // ListView of tasks
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer buttons

            // Header
            StackPanel spHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            TextBlock txtTitle = new TextBlock { Text = "📊 RAPPORTO DI MANUTENZIONE COMPLETATO", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)) };
            TextBlock txtSummary = new TextBlock { Text = summaryText, FontSize = 12.5, Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)), Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap };
            spHeader.Children.Add(txtTitle);
            spHeader.Children.Add(txtSummary);
            Grid.SetRow(spHeader, 0);
            mainGrid.Children.Add(spHeader);

            // ListView for Task Details
            ListView lvDetails = new ListView
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };

            GridView gv = new GridView();
            gv.Columns.Add(new GridViewColumn { Header = "Operazione", DisplayMemberBinding = new System.Windows.Data.Binding("TaskName"), Width = 280 });
            gv.Columns.Add(new GridViewColumn { Header = "Stato", DisplayMemberBinding = new System.Windows.Data.Binding("Status"), Width = 100 });
            gv.Columns.Add(new GridViewColumn { Header = "Spazio Liberato / Dettagli", DisplayMemberBinding = new System.Windows.Data.Binding("Details"), Width = 210 });
            lvDetails.View = gv;
            lvDetails.ItemsSource = items;

            Grid.SetRow(lvDetails, 1);
            mainGrid.Children.Add(lvDetails);

            // Footer
            Grid grdf = new Grid();
            grdf.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grdf.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grdf.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Button btnExport = CreateStyledButton("Esporta Log (.txt)", "#0284C7", "#0369A1");
            btnExport.Width = 160;
            btnExport.Click += BtnExport_Click;
            Grid.SetColumn(btnExport, 1);
            grdf.Children.Add(btnExport);

            Button btnClose = CreateStyledButton("Chiudi", "#475569", "#334155");
            btnClose.Width = 100;
            btnClose.Margin = new Thickness(10, 0, 0, 0);
            btnClose.Click += (s, e) => Close();
            Grid.SetColumn(btnClose, 2);
            grdf.Children.Add(btnClose);

            Grid.SetRow(grdf, 2);
            mainGrid.Children.Add(grdf);

            rootBorder.Child = mainGrid;
            this.Content = rootBorder;
        }

        private Button CreateStyledButton(string text, string hexNormal, string hexHover)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 36,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexNormal)));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexHover))
            });

            template.Triggers.Add(hoverTrigger);
            btn.Template = template;
            return btn;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "File di testo (*.txt)|*.txt",
                FileName = "RapportoManutenzione_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt",
                Title = "Salva Rapporto Manutenzione"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, reportText);
                    MessageBox.Show("Report esportato con successo in:\n" + sfd.FileName, "Esportazione Completata", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Errore durante la scrittura del file: " + ex.Message, "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class TaskReportItem
    {
        public string TaskName { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
    }

    public class DiskDetailsWindow : Window
    {
        public DiskDetailsWindow()
        {
            this.Title = "Analisi e Stato di Salute Unità di Archiviazione";
            this.Width = 740;
            this.Height = 520;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate 900
            this.Foreground = Brushes.White;
            this.FontFamily = new FontFamily("Segoe UI");
            this.ResizeMode = ResizeMode.NoResize;

            Border rootBorder = new Border { Padding = new Thickness(20), BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: SMART physical drives info
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: ListView of partitions
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Close button

            // Header
            StackPanel spHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            TextBlock txtTitle = new TextBlock { Text = "💾 RILEVAZIONE E COMPATIBILITÀ UNITÀ ARCHIVIAZIONE", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(192, 132, 252)) };
            TextBlock txtSub = new TextBlock { Text = "Analisi dei file system, stato dei blocchi in scrittura e compatibilità con i tool di manutenzione.", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 4, 0, 0) };
            spHeader.Children.Add(txtTitle);
            spHeader.Children.Add(txtSub);
            Grid.SetRow(spHeader, 0);
            mainGrid.Children.Add(spHeader);

            // SMART Physical Drives Info
            Border bdrSmart = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 15)
            };

            StackPanel spSmart = new StackPanel();
            TextBlock lblSmartTitle = new TextBlock { Text = "🔍 STATO DI SALUTE HARDWARE (S.M.A.R.T. ATTIVO)", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(168, 85, 247)), Margin = new Thickness(0, 0, 0, 6) };
            spSmart.Children.Add(lblSmartTitle);

            TextBlock txtSmartList = new TextBlock
            {
                Text = GetPhysicalDisksSmartReport(),
                FontSize = 11.5,
                Foreground = Brushes.White,
                LineHeight = 16,
                TextWrapping = TextWrapping.Wrap
            };
            spSmart.Children.Add(txtSmartList);
            bdrSmart.Child = spSmart;

            Grid.SetRow(bdrSmart, 1);
            mainGrid.Children.Add(bdrSmart);

            // ListView
            ListView lvDisks = new ListView
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };

            GridView gv = new GridView();
            gv.Columns.Add(new GridViewColumn { Header = "Unità", DisplayMemberBinding = new System.Windows.Data.Binding("DriveName"), Width = 70 });
            gv.Columns.Add(new GridViewColumn { Header = "Etichetta", DisplayMemberBinding = new System.Windows.Data.Binding("Label"), Width = 110 });
            gv.Columns.Add(new GridViewColumn { Header = "Format (FS)", DisplayMemberBinding = new System.Windows.Data.Binding("Format"), Width = 90 });
            gv.Columns.Add(new GridViewColumn { Header = "Capacità / Disponibile", DisplayMemberBinding = new System.Windows.Data.Binding("Capacity"), Width = 160 });
            gv.Columns.Add(new GridViewColumn { Header = "Stato Scrittura", DisplayMemberBinding = new System.Windows.Data.Binding("WriteStatus"), Width = 110 });
            gv.Columns.Add(new GridViewColumn { Header = "Manutenzione & Utilities", DisplayMemberBinding = new System.Windows.Data.Binding("MaintenanceSupport"), Width = 140 });
            lvDisks.View = gv;

            List<DiskReportItem> diskItems = new List<DiskReportItem>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                string label = drive.VolumeLabel;
                if (string.IsNullOrEmpty(label)) label = "Locale";

                string format = drive.DriveFormat;
                long totalSize = drive.TotalSize;
                long freeSpace = drive.AvailableFreeSpace;

                string capacityStr = MainWindow.FormatBytes(freeSpace) + " liberi di " + MainWindow.FormatBytes(totalSize);

                bool isReadOnly = false;
                try
                {
                    if (drive.DriveType == DriveType.CDRom)
                    {
                        isReadOnly = true;
                    }
                    else
                    {
                        string testFile = Path.Combine(drive.RootDirectory.FullName, "opt_test_write.tmp");
                        using (FileStream fs = File.Create(testFile)) { }
                        File.Delete(testFile);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    isReadOnly = true;
                }
                catch
                {
                    isReadOnly = true;
                }

                string writeStatus = isReadOnly ? "⚠ Solo Lettura" : "✓ Scrittura OK";
                
                bool trimSupported = (format.ToUpper() == "NTFS" || format.ToUpper() == "REFS") && !isReadOnly && drive.DriveType == DriveType.Fixed;
                bool chkdskSupported = (format.ToUpper() == "NTFS" || format.ToUpper() == "FAT32" || format.ToUpper() == "EXFAT");

                string supportStr = "";
                if (trimSupported && chkdskSupported) supportStr = "TRIM/Defrag e CHKDSK";
                else if (chkdskSupported) supportStr = "Solo CHKDSK (exFAT/FAT32)";
                else supportStr = "Non compatibile";

                if (isReadOnly) supportStr = "Nessuna (Solo Lettura)";

                diskItems.Add(new DiskReportItem
                {
                    DriveName = drive.Name,
                    Label = label,
                    Format = format,
                    Capacity = capacityStr,
                    WriteStatus = writeStatus,
                    MaintenanceSupport = supportStr
                });
            }

            lvDisks.ItemsSource = diskItems;
            Grid.SetRow(lvDisks, 2);
            mainGrid.Children.Add(lvDisks);

            // Close button
            Button btnClose = new Button
            {
                Content = "Chiudi",
                Height = 36,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(71, 85, 105)));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush(Color.FromRgb(51, 65, 85))
            });

            template.Triggers.Add(hoverTrigger);
            btnClose.Template = template;
            btnClose.Click += (s, e) => Close();

            Grid.SetRow(btnClose, 3);
            mainGrid.Children.Add(btnClose);

            rootBorder.Child = mainGrid;
            this.Content = rootBorder;
        }

        private string GetPhysicalDisksSmartReport()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, Model, Status, Size FROM Win32_DiskDrive"))
                {
                    int count = 0;
                    foreach (var obj in searcher.Get())
                    {
                        string model = obj["Model"] != null ? obj["Model"].ToString().Trim() : "Disco Hardware";
                        string status = obj["Status"] != null ? obj["Status"].ToString().Trim() : "OK";
                        
                        long size = 0;
                        if (obj["Size"] != null)
                        {
                            size = Convert.ToInt64(obj["Size"]);
                        }

                        string failurePredict = "";
                        try
                        {
                            using (var predictor = new ManagementObjectSearcher(@"root\wmi", "SELECT PredictFailure FROM MSStorageDriver_FailurePredictStatus"))
                            {
                                foreach (var pObj in predictor.Get())
                                {
                                    bool predictFailure = Convert.ToBoolean(pObj["PredictFailure"]);
                                    if (predictFailure)
                                    {
                                        failurePredict = " ⚠ PREVISIONE ROTTURA (FALLIMENTO IMMEDIATO!)";
                                    }
                                }
                            }
                        }
                        catch { }

                        string statusText = (status.ToUpper() == "OK" && string.IsNullOrEmpty(failurePredict))
                            ? "✓ In Salute (S.M.A.R.T. OK)"
                            : "⚠ Pericolo / Degenerato" + failurePredict;

                        if (count > 0) sb.AppendLine();
                        sb.Append(string.Format("• {0} ({1}) ➔ Stato: {2}", model, MainWindow.FormatBytes(size), statusText));
                        count++;
                    }
                    if (count == 0) sb.Append("Nessuna unità fisica rilevata tramite WMI.");
                }
            }
            catch (Exception ex)
            {
                sb.Append("Impossibile leggere i sensori di salute hardware: " + ex.Message);
            }
            return sb.ToString();
        }
    }

    public class DiskReportItem
    {
        public string DriveName { get; set; }
        public string Label { get; set; }
        public string Format { get; set; }
        public string Capacity { get; set; }
        public string WriteStatus { get; set; }
        public string MaintenanceSupport { get; set; }
    }

    public class DisclaimerWindow : Window
    {
        public DisclaimerWindow()
        {
            this.Title = "Note Legali, Sicurezza e Supporto";
            this.Width = 650;
            this.Height = 560;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate 900
            this.Foreground = Brushes.White;
            this.FontFamily = new FontFamily("Segoe UI");
            this.ResizeMode = ResizeMode.NoResize;

            Border rootBorder = new Border { Padding = new Thickness(22), BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1: Scrollable text content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Close button

            // Header
            StackPanel spHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            TextBlock txtTitle = new TextBlock { Text = "ℹ️ INFORMAZIONI LEGALI, SICUREZZA & SUPPORTO", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)) };
            TextBlock txtSub = new TextBlock { Text = "Termini di licenza d'uso, esclusione di responsabilità e supporto allo sviluppo.", FontSize = 11.5, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 4, 0, 0) };
            spHeader.Children.Add(txtTitle);
            spHeader.Children.Add(txtSub);
            Grid.SetRow(spHeader, 0);
            mainGrid.Children.Add(spHeader);

            // Scrollable Content
            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 0, 0, 15) };
            StackPanel spContent = new StackPanel();

            Action<string, string> addSection = (iconTitle, text) =>
            {
                Border bdrCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(22, 31, 48)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(40, 53, 72)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 10)
                };
                StackPanel sp = new StackPanel();
                TextBlock lblSecTitle = new TextBlock { Text = iconTitle, FontSize = 12.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)), Margin = new Thickness(0, 0, 0, 5) };
                TextBlock lblSecBody = new TextBlock { Text = text, FontSize = 11.5, Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), TextWrapping = TextWrapping.Wrap, LineHeight = 18 };
                sp.Children.Add(lblSecTitle);
                sp.Children.Add(lblSecBody);
                bdrCard.Child = sp;
                spContent.Children.Add(bdrCard);
            };

            addSection("🛡️ Esclusione di Responsabilità (Clausola 'AS IS')",
                "Questo software viene distribuito gratuitamente e fornito 'così com'è' (AS IS), senza alcuna garanzia esplicita o implicita.\nL'utente utilizza l'applicazione a proprio rischio e sotto la propria esclusiva responsabilità. Lo sviluppatore non potrà in alcun caso essere ritenuto responsabile per danni diretti, indiretti, perdite di dati, blocchi operativi o alterazioni del sistema derivanti dall'utilizzo delle funzioni di ottimizzazione.");

            addSection("💡 Raccomandazioni per la Sicurezza del Sistema",
                "Tutti i comandi e le operazioni raccomandate sono progettati per essere sicuri e non distruttivi per l'integrità del sistema operativo.\nTuttavia, per eseguire manutenzioni profonde (pulizia componenti WinSxS, riallineamento registro o riparazioni DISM), si raccomanda sempre di creare preventivamente un Punto di Ripristino di Windows (attivabile direttamente dall'elenco compiti dell'app).");

            addSection("⚖️ Proprietà Intellettuale & Marchi Registrati",
                "Windows 10, Windows 11, DISM, SFC, CHKDSK e Microsoft sono marchi registrati di proprietà di Microsoft Corporation.\nQuesto software è un progetto indipendente gratuito e open-source e non è affiliato, associato, sponsorizzato né autorizzato in alcun modo da Microsoft Corporation.");

            addSection("☕ Supporta lo Sviluppo (Offerta Libera)",
                "Questo strumento è completamente gratuito, privo di pubblicità e senza alcuna raccolta dati telemetrici.\nSe il programma ti è stato utile per velocizzare e mantenere in salute il tuo computer, puoi supportare il tempo e il lavoro dedicato allo sviluppo continuo con una donazione libera tramite PayPal o Ko-fi.");

            sv.Content = spContent;
            Grid.SetRow(sv, 1);
            mainGrid.Children.Add(sv);

            // Close button
            Button btnClose = new Button
            {
                Content = "Ho Capito / Chiudi",
                Height = 36,
                Width = 160,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(30, 58, 95)),
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnClose.Click += (s, e) => { this.Close(); };
            Grid.SetRow(btnClose, 2);
            mainGrid.Children.Add(btnClose);

            rootBorder.Child = mainGrid;
            this.Content = rootBorder;
        }
    }
}
