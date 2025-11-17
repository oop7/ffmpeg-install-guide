using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FFmpegInstaller
{
    public enum FFmpegBuildType
    {
        Full,
        Essentials,
        Shared
    }

    public enum InstallationScope
    {
        User,
        System
    }

    public class BuildInfo
    {
        public FFmpegBuildType Type { get; set; }
        public string Name { get; set; }
        public string DownloadUrl { get; set; }
        public string HashUrl { get; set; }
        public string Description { get; set; }
        public string ApproximateSize { get; set; }
        public string FileName { get; set; }
    }

    public partial class MainForm : Form
    {
        private const string VERSION_URL = "https://www.gyan.dev/ffmpeg/builds/release-version";
        private const string PORTABLE_7Z_URL = "https://www.7-zip.org/a/7za920.zip";
        private const string APP_UPDATE_URL = "https://api.github.com/repos/oop7/ffmpeg-install-guide/releases/latest";
        private const string REPO_URL = "https://github.com/oop7/ffmpeg-install-guide";
        private const string CURRENT_VERSION = "2.5.7";

        private static readonly Dictionary<FFmpegBuildType, BuildInfo> BuildInfos = new Dictionary<FFmpegBuildType, BuildInfo>
        {
            {
                FFmpegBuildType.Full, new BuildInfo
                {
                    Type = FFmpegBuildType.Full,
                    Name = "Full",
                    DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-full.7z",
                    HashUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-full.7z.sha256",
                    Description = "Complete build with all libraries and codecs (recommended for most users)",
                    ApproximateSize = "~60 MB",
                    FileName = "ffmpeg-release-full.7z"
                }
            },
            {
                FFmpegBuildType.Essentials, new BuildInfo
                {
                    Type = FFmpegBuildType.Essentials,
                    Name = "Essentials",
                    DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.7z",
                    HashUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.7z.sha256",
                    Description = "Minimal build with essential codecs only (recommended for YTSage users)",
                    ApproximateSize = "~30 MB",
                    FileName = "ffmpeg-release-essentials.7z"
                }
            },
            {
                FFmpegBuildType.Shared, new BuildInfo
                {
                    Type = FFmpegBuildType.Shared,
                    Name = "Shared",
                    DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-full-shared.7z",
                    HashUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-full-shared.7z.sha256",
                    Description = "Full build with shared libraries (for developers)",
                    ApproximateSize = "~50 MB",
                    FileName = "ffmpeg-release-full-shared.7z"
                }
            }
        };

        private readonly string extractDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg");
        private readonly string tempExtractDir = Path.Combine(Path.GetTempPath(), "ffmpeg-extract");
        private readonly string portable7z = Path.Combine(Path.GetTempPath(), "7z-zip\\7za.exe");

        private readonly HttpClient httpClient = new HttpClient();
        private string latestVersion = "Unknown";
        private string expectedHash = null;
        private bool isInstalling = false;
        private FFmpegBuildType selectedBuildType = FFmpegBuildType.Full;
        private InstallationScope installationScope = InstallationScope.User;
        private string tempFile;

        // UI Controls
        private Label titleLabel;
        private Label versionLabel;
        private Label hashLabel;
        private Label statusLabel;
        private Label speedLabel;
        private ProgressBar progressBar;
        private Button installButton;
        private Button aboutButton;
        private Button exitButton;
        private TextBox logTextBox;
        private Panel headerPanel;
        private Panel buttonPanel;

        public MainForm()
        {
            InitializeComponent();
            
            // Check if restarted with --system flag (after UAC elevation)
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "--system")
            {
                // Skip dialog, directly set to system-wide
                installationScope = InstallationScope.System;
                LogMessage("Installation scope: system-wide (elevated)");
                statusLabel.Text = "Ready to install (system-wide)";
            }
            else
            {
                // Show dialog for user to choose
                ShowInstallationScopeDialog();
            }
            
            CheckAdminPrivileges();
            _ = LoadVersionInfoAsync();
            _ = CheckForUpdatesAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "FFmpeg Installer";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Load custom icon
            try
            {
                // Try to load from embedded resource first (for single file exe)
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("FFmpegInstaller.Icons.app_icon.ico"))
                {
                    if (stream != null)
                    {
                        // Try different sizes to find the best match
                        var originalIcon = new Icon(stream);

                        // Try 48x48 first (common for desktop icons), then 32x32, then original
                        try
                        {
                            this.Icon = new Icon(originalIcon, 48, 48);
                        }
                        catch
                        {
                            try
                            {
                                this.Icon = new Icon(originalIcon, 32, 32);
                            }
                            catch
                            {
                                this.Icon = originalIcon; // Use original if sizing fails
                            }
                        }
                    }
                    else
                    {
                        // Fallback to file system
                        string iconPath = Path.Combine(Application.StartupPath, "Icons", "app_icon.ico");
                        if (File.Exists(iconPath))
                        {
                            var iconFromFile = new Icon(iconPath);
                            try
                            {
                                this.Icon = new Icon(iconFromFile, 48, 48);
                            }
                            catch
                            {
                                this.Icon = new Icon(iconFromFile, 32, 32);
                            }
                        }
                        else
                        {
                            this.Icon = SystemIcons.Application;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Icon = SystemIcons.Application;
                LogMessage($"Icon loading failed: {ex.Message}");
            }

            // Header Panel
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            titleLabel = new Label
            {
                Text = "FFmpeg Installer",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(300, 30)
            };

            versionLabel = new Label
            {
                Text = "Loading version information...",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(20, 55),
                Size = new Size(500, 20)
            };

            hashLabel = new Label
            {
                Text = "Select a build to see hash information",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(20, 75),
                Size = new Size(500, 20)
            };

            headerPanel.Controls.AddRange(new Control[] { titleLabel, versionLabel, hashLabel });

            // Status and Progress
            statusLabel = new Label
            {
                Text = "Ready to install",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 140),
                Size = new Size(400, 20)
            };

            speedLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(430, 140),
                Size = new Size(130, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            progressBar = new ProgressBar
            {
                Location = new Point(20, 170),
                Size = new Size(540, 25),
                Style = ProgressBarStyle.Continuous
            };

            // Log TextBox
            logTextBox = new TextBox
            {
                Location = new Point(20, 210),
                Size = new Size(540, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.White
            };

            // Button Panel
            buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            installButton = new Button
            {
                Text = "Install FFmpeg",
                Size = new Size(120, 35),
                Location = new Point(280, 12),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            installButton.Click += InstallButton_Click;

            aboutButton = new Button
            {
                Text = "About",
                Size = new Size(80, 35),
                Location = new Point(410, 12),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            aboutButton.Click += AboutButton_Click;

            exitButton = new Button
            {
                Text = "Exit",
                Size = new Size(80, 35),
                Location = new Point(500, 12),
                BackColor = Color.FromArgb(160, 160, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            exitButton.Click += (s, e) => this.Close();

            buttonPanel.Controls.AddRange(new Control[] { installButton, aboutButton, exitButton });

            // Add all controls to form
            this.Controls.AddRange(new Control[] { headerPanel, statusLabel, speedLabel, progressBar, logTextBox, buttonPanel });
        }

        private void ShowInstallationScopeDialog()
        {
            var scopeForm = new Form
            {
                Text = "Installation Scope",
                Size = new Size(500, 330),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var titleLabel = new Label
            {
                Text = "Choose Installation Scope",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(440, 25)
            };

            var descLabel = new Label
            {
                Text = "How would you like to install FFmpeg?",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(20, 50),
                Size = new Size(440, 20)
            };

            var userRadio = new RadioButton
            {
                Text = "User Installation (Recommended)",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(30, 85),
                Size = new Size(250, 20),
                Checked = true
            };

            var userDesc = new Label
            {
                Text = "• No administrator privileges required\n• Available only for your user account\n• Installed in your AppData folder",
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = Color.DarkGray,
                Location = new Point(50, 108),
                Size = new Size(400, 50)
            };

            var systemRadio = new RadioButton
            {
                Text = "System-wide Installation",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(30, 160),
                Size = new Size(250, 20)
            };

            var systemDesc = new Label
            {
                Text = "• Requires administrator privileges\n• Available for all users on this computer\n• Installed in Program Files or similar",
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = Color.DarkGray,
                Location = new Point(50, 183),
                Size = new Size(400, 50)
            };

            var continueButton = new Button
            {
                Text = "Continue",
                Location = new Point(280, 245),
                Size = new Size(100, 35),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            continueButton.FlatAppearance.BorderSize = 0;
            scopeForm.AcceptButton = continueButton;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(390, 245),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };

            scopeForm.Controls.AddRange(new Control[] {
                titleLabel, descLabel,
                userRadio, userDesc,
                systemRadio, systemDesc,
                continueButton, cancelButton
            });

            var result = scopeForm.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                installationScope = userRadio.Checked ? InstallationScope.User : InstallationScope.System;
                var scopeText = installationScope == InstallationScope.User ? "user-level" : "system-wide";
                LogMessage($"Installation scope selected: {scopeText}");
                statusLabel.Text = $"Ready to install ({scopeText})";
            }
            else
            {
                LogMessage("Installation cancelled by user");
                Application.Exit();
            }
        }

        private void CheckAdminPrivileges()
        {
            // Only require admin if system-wide installation is selected
            if (installationScope == InstallationScope.System)
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        // Restart the application with admin privileges
                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = Application.ExecutablePath,
                                UseShellExecute = true,
                                Verb = "runas", // Request elevation
                                Arguments = "--system" // Pass flag to indicate system-wide install
                            };
                            
                            Process.Start(startInfo);
                            Application.Exit();
                        }
                        catch (Exception ex)
                        {
                            // User cancelled UAC prompt or other error
                            LogMessage($"Failed to elevate privileges: {ex.Message}");
                            MessageBox.Show("Administrator privileges are required for system-wide installation.\n\nPlease approve the UAC prompt or choose User Installation instead.",
                                "Administrator Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Application.Exit();
                        }
                    }
                    else
                    {
                        LogMessage("System-wide installation with administrator privileges");
                    }
                }
            }
            else
            {
                LogMessage("User installation selected - administrator privileges not required");
            }
        }

        private async Task LoadVersionInfoAsync()
        {
            try
            {
                // Get version
                latestVersion = (await httpClient.GetStringAsync(VERSION_URL)).Trim();
                versionLabel.Text = $"Latest Version: {latestVersion}";
                LogMessage($"Latest FFmpeg version: {latestVersion}");
            }
            catch (Exception ex)
            {
                versionLabel.Text = "Version: Could not fetch";
                LogMessage($"Warning: Could not fetch version info: {ex.Message}");
            }
        }

        private async Task LoadHashForBuildAsync(FFmpegBuildType buildType)
        {
            try
            {
                var buildInfo = BuildInfos[buildType];
                var hashResponse = await httpClient.GetStringAsync(buildInfo.HashUrl);
                expectedHash = hashResponse.Trim().Split()[0];
                hashLabel.Text = $"SHA256: {expectedHash}";
                LogMessage($"Expected hash for {buildInfo.Name} build: {expectedHash}");
            }
            catch (Exception ex)
            {
                hashLabel.Text = "Hash: Could not fetch";
                LogMessage($"Warning: Could not fetch hash info: {ex.Message}");
            }
        }

        private async void InstallButton_Click(object sender, EventArgs e)
        {
            if (isInstalling) return;

            // Show build selection dialog
            var selectedBuild = ShowBuildSelectionDialog();
            if (selectedBuild == null)
            {
                LogMessage("Installation cancelled by user");
                return;
            }

            selectedBuildType = selectedBuild.Value;
            var buildInfo = BuildInfos[selectedBuildType];
            tempFile = Path.Combine(Path.GetTempPath(), buildInfo.FileName);

            // Load hash for selected build
            await LoadHashForBuildAsync(selectedBuildType);

            var result = MessageBox.Show(
                $"This will install FFmpeg {latestVersion} ({buildInfo.Name} build) to:\n{extractDir}\n\nAnd add it to your system PATH.\n\nProceed?",
                "Confirm Installation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                await InstallFFmpegAsync();
            }
        }

        private async Task InstallFFmpegAsync()
        {
            isInstalling = true;
            installButton.Enabled = false;
            progressBar.Value = 0;

            try
            {
                // Step 1: Clean up previous installation
                UpdateStatus("Cleaning up previous installation...");
                CleanupPreviousInstallation();
                progressBar.Value = 10;

                var downloaded = false;
                if (File.Exists(tempFile))
                {
                    if (VerifyFileHash())
                    {
                        UpdateStatus("✓ There has downloaded file and hash verification successful");
                        downloaded = true;
                        progressBar.Value = 40;
                    }
                    else
                    {
                        File.Delete(tempFile);
                    }
                }

                if (!downloaded)
                {
                    // Step 2: Download FFmpeg
                    var currentBuild = BuildInfos[selectedBuildType];
                    UpdateStatus($"Downloading FFmpeg ({currentBuild.Name} build)...");
                    await DownloadFFmpegAsync();
                    progressBar.Value = 40;

                    // Step 3: Verify hash
                    UpdateStatus("Verifying file integrity...");
                    if (!VerifyFileHash())
                    {
                        throw new Exception("File integrity verification failed");
                    }
                }

                progressBar.Value = 50;

                // Step 4: Extract files
                UpdateStatus("Extracting files...");
                await ExtractFilesAsync();
                progressBar.Value = 80;

                // Step 5: Install and configure
                UpdateStatus("Installing and configuring...");
                InstallAndConfigure();
                progressBar.Value = 95;

                // Step 6: Test installation
                UpdateStatus("Testing installation...");
                TestInstallation();
                progressBar.Value = 100;

                var buildInfo = BuildInfos[selectedBuildType];
                var scopeText = installationScope == InstallationScope.User ? "user-level" : "system-wide";
                UpdateStatus($"Installation of {buildInfo.Name} build completed successfully!");
                LogMessage($"✓ FFmpeg {buildInfo.Name} build ({scopeText}) installation completed successfully!");

                var restartMessage = installationScope == InstallationScope.User
                    ? "Please restart your command prompt or applications to use ffmpeg."
                    : "Please restart your command prompt to use ffmpeg.";

                MessageBox.Show($"FFmpeg ({buildInfo.Name} build) has been installed successfully as a {scopeText} installation!\n\n{restartMessage}",
                    "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Installation failed: {ex.Message}");
                LogMessage($"✗ Installation failed: {ex.Message}");
                MessageBox.Show($"Installation failed:\n\n{ex.Message}", "Installation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                //CleanupTempFiles();//don't clearnup,because the downloaded file can be use next time.
                isInstalling = false;
                installButton.Enabled = true;
            }
        }

        private void CleanupPreviousInstallation()
        {
            try
            {
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                    LogMessage("Previous installation cleaned up");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not clean up previous installation: {ex.Message}");
            }
        }

        private async Task DownloadFFmpegAsync()
        {
            try
            {
                var buildInfo = BuildInfos[selectedBuildType];
                LogMessage($"Starting download of {buildInfo.Name} build...");


                using (var response = await httpClient.GetAsync(buildInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var downloadedBytes = 0L;
                    var startTime = DateTime.Now;
                    var lastUpdateTime = startTime;
                    var lastDownloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            var currentTime = DateTime.Now;
                            var timeElapsed = currentTime - lastUpdateTime;

                            // Update speed every 500ms
                            if (timeElapsed.TotalMilliseconds >= 500)
                            {
                                var bytesSinceLastUpdate = downloadedBytes - lastDownloadedBytes;
                                var speedBytesPerSecond = bytesSinceLastUpdate / timeElapsed.TotalSeconds;
                                var speedText = FormatSpeed(speedBytesPerSecond);

                                UpdateSpeedDisplay(speedText);

                                lastUpdateTime = currentTime;
                                lastDownloadedBytes = downloadedBytes;
                            }

                            if (totalBytes > 0)
                            {
                                var progress = (int)((downloadedBytes * 30) / totalBytes) + 10; // 10-40%
                                progressBar.Value = Math.Min(progress, 40);
                            }
                        }
                    }
                }

                // Clear speed display when download is complete
                UpdateSpeedDisplay("");

                var fileInfo = new FileInfo(tempFile);
                LogMessage($"Download completed: {fileInfo.Length / 1024 / 1024:F2} MB");
            }
            catch (Exception ex)
            {
                UpdateSpeedDisplay("");
                throw new Exception($"Download failed: {ex.Message}");
            }
        }

        private bool VerifyFileHash()
        {
            if (string.IsNullOrEmpty(expectedHash))
            {
                LogMessage("Skipping hash verification (hash not available)");
                return true;
            }

            try
            {
                using (var sha256 = SHA256.Create())
                using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                {
                    var hashBytes = sha256.ComputeHash(fileStream);
                    var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    if (actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        LogMessage("✓ File hash verification successful");
                        return true;
                    }
                    else
                    {
                        LogMessage($"✗ Hash mismatch!");
                        LogMessage($"Expected: {expectedHash}");
                        LogMessage($"Actual:   {actualHash}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Hash verification error: {ex.Message}");
                return false;
            }
        }

        private async Task ExtractFilesAsync()
        {
            // Clean up temp extract directory
            if (Directory.Exists(tempExtractDir))
            {
                Directory.Delete(tempExtractDir, true);
            }
            Directory.CreateDirectory(tempExtractDir);

            // Try multiple extraction methods
            if (await TryExtractWithPortable7z())
            {
                LogMessage("✓ Extraction completed using portable 7z");
                return;
            }

            if (await TryExtractWithComObject())
            {
                LogMessage("✓ Extraction completed using COM object");
                return;
            }

            if (TryExtractAsZip())
            {
                LogMessage("✓ Extraction completed using ZIP method");
                return;
            }

            throw new Exception("All extraction methods failed");
        }

        private async Task<bool> TryExtractWithPortable7z()
        {
            try
            {
                LogMessage("Attempting extraction with portable 7z...");

                if (!File.Exists(portable7z))
                {
                    LogMessage("Downloading portable 7z...");
                    await DownloadPortable7z();
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = portable7z,
                    Arguments = $"x \"{tempFile}\" -o\"{tempExtractDir}\" -y",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    bool success = process.ExitCode == 0;
                    if (!success && !string.IsNullOrEmpty(error))
                    {
                        LogMessage($"Portable 7z extraction failed: {Environment.NewLine}{error}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Portable 7z extraction failed: {ex.Message}");
                return false;
            }
        }

        private async Task DownloadPortable7z()
        {
            var tempZipFile = Path.Combine(Path.GetTempPath(), "7za.zip");

            using (var response = await httpClient.GetAsync(PORTABLE_7Z_URL))
            {
                response.EnsureSuccessStatusCode();
                using (var fileStream = new FileStream(tempZipFile, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            ZipFile.ExtractToDirectory(tempZipFile, Path.GetTempPath() + "7z-zip");
            //File.Delete(tempZipFile);
        }

        private async Task<bool> TryExtractWithComObject()
        {
            try
            {
                LogMessage("Attempting extraction with COM object...");

                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                dynamic zip = shell.NameSpace(tempFile);
                dynamic dest = shell.NameSpace(tempExtractDir);

                if (zip == null) return false;

                dest.CopyHere(zip.Items(), 4);

                // Wait for extraction to complete
                var timeout = DateTime.Now.AddSeconds(60);
                while (DateTime.Now < timeout)
                {
                    if (Directory.GetDirectories(tempExtractDir).Length > 0 ||
                        Directory.GetFiles(tempExtractDir).Length > 0)
                    {
                        return true;
                    }
                    await Task.Delay(1000);
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"COM object extraction failed: {ex.Message}");
                return false;
            }
        }

        private bool TryExtractAsZip()
        {
            try
            {
                LogMessage("Attempting extraction as ZIP...");

                var tempZipFile = Path.ChangeExtension(tempFile, ".zip");
                File.Copy(tempFile, tempZipFile, true);

                ZipFile.ExtractToDirectory(tempZipFile, tempExtractDir);
                File.Delete(tempZipFile);

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"ZIP extraction failed: {ex.Message}");
                return false;
            }
        }

        private void InstallAndConfigure()
        {
            // Find extracted FFmpeg folder
            var ffmpegDir = FindFFmpegDirectory(tempExtractDir);
            if (ffmpegDir == null)
            {
                throw new Exception("FFmpeg directory not found in extracted files");
            }

            LogMessage($"Found FFmpeg at: {ffmpegDir}");

            // Copy to final destination
            Directory.CreateDirectory(extractDir);
            CopyDirectory(ffmpegDir, Path.Combine(extractDir, Path.GetFileName(ffmpegDir)));

            // Find bin directory
            var binDir = Path.Combine(extractDir, Path.GetFileName(ffmpegDir), "bin");
            if (!Directory.Exists(binDir) || !File.Exists(Path.Combine(binDir, "ffmpeg.exe")))
            {
                throw new Exception("FFmpeg executable not found after installation");
            }

            // Add to PATH
            AddToSystemPath(binDir);
            LogMessage($"Added to system PATH: {binDir}");
        }

        private string FindFFmpegDirectory(string searchDir)
        {
            // Look for directory containing bin/ffmpeg.exe
            foreach (var dir in Directory.GetDirectories(searchDir, "*", SearchOption.AllDirectories))
            {
                var binPath = Path.Combine(dir, "bin", "ffmpeg.exe");
                if (File.Exists(binPath))
                {
                    return dir;
                }
            }
            return null;
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private void AddToSystemPath(string path)
        {
            try
            {
                RegistryKey key;
                string pathType;

                if (installationScope == InstallationScope.User)
                {
                    // User-level PATH (HKEY_CURRENT_USER)
                    key = Registry.CurrentUser.OpenSubKey("Environment", true);
                    pathType = "user PATH";
                }
                else
                {
                    // System-level PATH (HKEY_LOCAL_MACHINE)
                    key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", true);
                    pathType = "system PATH";
                }

                using (key)
                {
                    var currentPath = key.GetValue("PATH", "", RegistryValueOptions.DoNotExpandEnvironmentNames).ToString();

                    if (!currentPath.Contains(path))
                    {
                        var newPath = string.IsNullOrEmpty(currentPath) ? path : $"{currentPath};{path}";
                        key.SetValue("PATH", newPath, RegistryValueKind.ExpandString);
                        LogMessage($"Successfully added to {pathType}");
                    }
                    else
                    {
                        LogMessage($"Path already exists in {pathType}");
                    }
                }

                // Broadcast environment change
                SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SETTINGCHANGE,
                    IntPtr.Zero,
                    "Environment",
                    SMTO_ABORTIFHUNG,
                    5000,
                    out _
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update PATH: {ex.Message}");
            }
        }

        // P/Invoke for broadcasting environment changes
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult
        );

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        private void TestInstallation()
        {
            try
            {
                var ffmpegDir = Directory.GetDirectories(extractDir).First();
                var ffmpegExe = Path.Combine(ffmpegDir, "bin", "ffmpeg.exe");

                if (File.Exists(ffmpegExe))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegExe,
                        Arguments = "-version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            var versionLine = output.Split('\n').FirstOrDefault(line => line.StartsWith("ffmpeg version"));
                            if (versionLine != null)
                            {
                                LogMessage($"✓ FFmpeg is working: {versionLine.Split(' ')[2]}");
                            }
                            else
                            {
                                LogMessage("✓ FFmpeg is working correctly");
                            }
                        }
                        else
                        {
                            LogMessage("⚠ FFmpeg test failed, but installation appears complete");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠ Could not test installation: {ex.Message}");
            }
        }

        private void CleanupTempFiles()
        {
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
                if (File.Exists(portable7z)) File.Delete(portable7z);
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Could not clean up all temp files: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }

            statusLabel.Text = message;
            LogMessage(message);
        }

        private void UpdateSpeedDisplay(string speedText)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateSpeedDisplay), speedText);
                return;
            }

            speedLabel.Text = speedText;
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024)
                return $"{bytesPerSecond:F0} B/s";
            else if (bytesPerSecond < 1024 * 1024)
                return $"{bytesPerSecond / 1024:F1} KB/s";
            else if (bytesPerSecond < 1024 * 1024 * 1024)
                return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
            else
                return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logTextBox.AppendText($"[{timestamp}] {message}\r\n");
            logTextBox.ScrollToCaret();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            httpClient?.Dispose();
            base.OnFormClosed(e);
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                LogMessage("Checking for application updates...");

                // Add User-Agent header to avoid some rate limiting
                httpClient.DefaultRequestHeaders.Add("User-Agent", "FFmpegInstaller/2.5");

                var response = await httpClient.GetStringAsync(APP_UPDATE_URL);
                dynamic releaseInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                string latestVersion = releaseInfo.tag_name;
                if (latestVersion.StartsWith("v"))
                    latestVersion = latestVersion.Substring(1);

                var current = new Version(CURRENT_VERSION);
                var latest = new Version(latestVersion);

                if (latest > current)
                {
                    LogMessage($"Update available: v{latestVersion}");

                    var result = MessageBox.Show(
                        $"A new version (v{latestVersion}) is available!\n\n" +
                        $"Current version: v{CURRENT_VERSION}\n" +
                        $"Latest version: v{latestVersion}\n\n" +
                        "Would you like to visit the download page?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = REPO_URL + "/releases/latest",
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    LogMessage("Application is up to date");
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                LogMessage("Update check temporarily unavailable (rate limited). You can manually check for updates at the GitHub repository.");
            }
            catch (Exception ex)
            {
                LogMessage($"Could not check for updates: {ex.Message}");
            }
        }

        private FFmpegBuildType? ShowBuildSelectionDialog()
        {
            var buildForm = new Form
            {
                Text = "Select FFmpeg Build",
                Size = new Size(500, 380),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            // Title
            var titleLabel = new Label
            {
                Text = "Choose FFmpeg Build Type",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(450, 30),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Select the build that best suits your needs:",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 55),
                Size = new Size(450, 20),
                ForeColor = Color.Gray
            };

            // Radio buttons for each build
            var radioButtons = new Dictionary<FFmpegBuildType, RadioButton>();
            int yPosition = 90;

            foreach (var buildInfo in BuildInfos.Values.OrderBy(b => b.Type))
            {
                var radioButton = new RadioButton
                {
                    Location = new Point(30, yPosition),
                    Size = new Size(430, 20),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Text = $"{buildInfo.Name} ({buildInfo.ApproximateSize})",
                    Checked = buildInfo.Type == FFmpegBuildType.Full
                };

                var descriptionLabel = new Label
                {
                    Location = new Point(50, yPosition + 25),
                    Size = new Size(410, 40),
                    Font = new Font("Segoe UI", 9),
                    Text = buildInfo.Description,
                    ForeColor = Color.FromArgb(64, 64, 64)
                };

                radioButtons[buildInfo.Type] = radioButton;
                buildForm.Controls.Add(radioButton);
                buildForm.Controls.Add(descriptionLabel);

                yPosition += 70;
            }

            // Continue button
            var continueButton = new Button
            {
                Text = "Continue",
                Size = new Size(100, 35),
                Location = new Point(270, 300),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            // Cancel button
            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(380, 300),
                BackColor = Color.FromArgb(160, 160, 160),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                DialogResult = DialogResult.Cancel
            };

            buildForm.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, continueButton, cancelButton });
            buildForm.AcceptButton = continueButton;
            buildForm.CancelButton = cancelButton;

            var result = buildForm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                foreach (var kvp in radioButtons)
                {
                    if (kvp.Value.Checked)
                    {
                        return kvp.Key;
                    }
                }
            }

            return null;
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            var aboutForm = new Form
            {
                Text = "About FFmpeg Installer",
                Size = new Size(450, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            // Icon
            var iconPictureBox = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = this.Icon?.ToBitmap()
            };

            // Title
            var titleLabel = new Label
            {
                Text = "FFmpeg Installer",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(80, 20),
                Size = new Size(300, 30),
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Version
            var versionLabel = new Label
            {
                Text = $"Version {CURRENT_VERSION}",
                Font = new Font("Segoe UI", 10),
                Location = new Point(80, 50),
                Size = new Size(300, 20),
                ForeColor = Color.Gray
            };

            // Description
            var descriptionLabel = new Label
            {
                Text = "A Windows installer for FFmpeg with automatic PATH configuration.\n" +
                       "This tool simplifies the installation of FFmpeg on Windows systems.",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 90),
                Size = new Size(400, 40),
                ForeColor = Color.Black
            };

            // Developer info
            var developerLabel = new Label
            {
                Text = "Developed by: oop7",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 140),
                Size = new Size(300, 20),
                ForeColor = Color.Black
            };

            // Repository link
            var repoLinkLabel = new LinkLabel
            {
                Text = "GitHub Repository",
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 165),
                Size = new Size(150, 20),
                LinkColor = Color.FromArgb(0, 120, 215)
            };
            repoLinkLabel.Click += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = REPO_URL,
                    UseShellExecute = true
                });
            };

            // Copyright
            var copyrightLabel = new Label
            {
                Text = "Copyright © 2025. All rights reserved.",
                Font = new Font("Segoe UI", 8),
                Location = new Point(20, 195),
                Size = new Size(300, 20),
                ForeColor = Color.Gray
            };

            // Check Updates button
            var checkUpdatesButton = new Button
            {
                Text = "Check Updates",
                Size = new Size(110, 30),
                Location = new Point(220, 220),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            checkUpdatesButton.Click += async (s, e) =>
            {
                checkUpdatesButton.Enabled = false;
                checkUpdatesButton.Text = "Checking...";
                await CheckForUpdatesAsync();
                checkUpdatesButton.Enabled = true;
                checkUpdatesButton.Text = "Check Updates";
            };

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(80, 30),
                Location = new Point(340, 220),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            closeButton.Click += (s, e) => aboutForm.Close();

            aboutForm.Controls.AddRange(new Control[]
            {
                iconPictureBox, titleLabel, versionLabel, descriptionLabel,
                developerLabel, repoLinkLabel, copyrightLabel, checkUpdatesButton, closeButton
            });

            aboutForm.ShowDialog(this);
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}