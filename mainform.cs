using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FixTool {
    public class MainForm : Form {
        private readonly string windowsVersion;
        private readonly string buildNumber;
        
        public MainForm() {
            GetWindowsVersion(out windowsVersion, out buildNumber);
            
            Text = $"FixTool - ابزار عیب‌یابی (Windows {windowsVersion})";
            Width = 440;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new System.Drawing.Font("Segoe UI", 10);
            BackColor = Color.Black;
            ForeColor = Color.White;

            var panel = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(20),
                AutoScroll = true,
                BackColor = Color.Black
            };
            Controls.Add(panel);

            panel.Controls.Add(new Label { Text = "دستورات سیستمی:", AutoSize = true, Margin = new Padding(5), ForeColor = Color.White });
            panel.Controls.Add(CreateButton("بررسی دیسک (chkdsk C:)", (s,e) => RunCmd("chkdsk", "C: /f")));
            panel.Controls.Add(CreateButton("اسکن و ترمیم فایل‌های سیستمی (sfc /scannow)", (s,e) => RunCmd("sfc", "/scannow")));
            if (IsVistaOrNewer()) {
                panel.Controls.Add(CreateButton("اسکن و ترمیم تصاویر ویندوز (DISM)", (s,e) => RunCmd("DISM", "/Online /Cleanup-Image /RestoreHealth")));
            }
            panel.Controls.Add(CreateButton("بررسی خطاهای حافظه (mdsched)", (s,e) => RunCmd("mdsched", "")));
            
            panel.Controls.Add(new Label { Text = "مدیریت دستگاه‌ها:", AutoSize = true, Margin = new Padding(5, 15, 5, 5), ForeColor = Color.White });
            panel.Controls.Add(CreateButton("Device Manager (devmgmt.msc)", (s,e) => RunCmd("devmgmt.msc", "")));
            panel.Controls.Add(CreateButton("Disk Management (diskmgmt.msc)", (s,e) => RunCmd("diskmgmt.msc", "")));
            panel.Controls.Add(CreateButton("Computer Management (compmgmt.msc)", (s,e) => RunCmd("compmgmt.msc", "")));
            panel.Controls.Add(CreateButton("Services (services.msc)", (s,e) => RunCmd("services.msc", "")));
            panel.Controls.Add(CreateButton("Event Viewer (eventvwr.msc)", (s,e) => RunCmd("eventvwr.msc", "")));

            panel.Controls.Add(new Label { Text = "دستورات شبکه:", AutoSize = true, Margin = new Padding(5, 15, 5, 5), ForeColor = Color.White });
            panel.Controls.Add(CreateButton("پاک‌سازی DNS (ipconfig /flushdns)", (s,e) => RunCmd("ipconfig", "/flushdns")));
            panel.Controls.Add(CreateButton("ریست شبکه (netsh winsock reset)", (s,e) => RunCmd("netsh", "winsock reset")));
            panel.Controls.Add(CreateButton("تجدید IP (ipconfig /release /renew)", (s,e) => RunMultiCmd(new[] {
                ("ipconfig", "/release"),
                ("ipconfig", "/renew")
            })));
            panel.Controls.Add(CreateButton("ریست کامل TCP/IP", (s,e) => RunMultiCmd(new[] {
                ("netsh", "int ip reset"),
                ("netsh", "winsock reset"),
                ("ipconfig", "/release"),
                ("ipconfig", "/renew"),
                ("ipconfig", "/flushdns")
            })));
            if (IsVistaOrNewer()) {
                panel.Controls.Add(CreateButton("Network Adapter Reset", (s,e) => RunMultiCmd(new[] {
                    ("netsh", "int ip reset"),
                    ("netsh", "int ipv4 reset"),
                    ("netsh", "int ipv6 reset"),
                    ("netsh", "winsock reset")
                })));
            }
            panel.Controls.Add(CreateButton("Network Statistics", (s,e) => RunCmd("netstat", "-ab")));
            panel.Controls.Add(CreateButton("Network Connections", (s,e) => RunCmd("ncpa.cpl", "")));
            panel.Controls.Add(CreateButton("Ping 8.8.8.8", (s,e) => RunCmd("ping", "8.8.8.8 -t")));
            panel.Controls.Add(CreateButton("Trace Google.com", (s,e) => RunCmd("tracert", "google.com")));
            panel.Controls.Add(CreateButton("Nslookup", (s,e) => RunCmd("nslookup", "")));
            
            if (IsVistaOrNewer()) {
                panel.Controls.Add(CreateButton("DNS Settings", (s,e) => RunCmd("rundll32.exe", "shell32.dll,Control_RunDLL ncpa.cpl,,2")));
            } else {
                panel.Controls.Add(CreateButton("DNS Settings", (s,e) => RunCmd("control", "netconnections")));
            }
            
            panel.Controls.Add(CreateButton("Proxy Settings", (s,e) => RunCmd("rundll32.exe", "shell32.dll,Control_RunDLL inetcpl.cpl,,4")));

            panel.Controls.Add(new Label { Text = "دستورات نگهداری:", AutoSize = true, Margin = new Padding(5, 15, 5, 5), ForeColor = Color.White });
            panel.Controls.Add(CreateButton("پاکسازی فایل‌های موقت (cleanmgr)", (s,e) => RunCmd("cleanmgr", "/sagerun:1")));
            panel.Controls.Add(CreateButton("Disk Defragment (dfrgui)", (s,e) => RunCmd("dfrgui", "")));
            
            if (IsVistaOrNewer()) {
                panel.Controls.Add(CreateButton("Windows Update Check", (s,e) => RunCmd("wuauclt", "/detectnow")));
            }
            
            panel.Controls.Add(CreateButton("System Configuration (msconfig)", (s,e) => RunCmd("msconfig", "")));
            
            if (IsVistaOrNewer()) {
                panel.Controls.Add(CreateButton("Resource Monitor", (s,e) => RunCmd("resmon", "")));
                panel.Controls.Add(CreateButton("Performance Monitor", (s,e) => RunCmd("perfmon", "")));
            }
            
            panel.Controls.Add(CreateButton("Power Configuration", (s,e) => RunCmd("powercfg.cpl", "")));

            panel.Controls.Add(new Label { Text = "تنظیمات سیستمی:", AutoSize = true, Margin = new Padding(5, 15, 5, 5), ForeColor = Color.White });
            
            if (IsVistaOrNewer()) {
                panel.Controls.Add(CreateButton("توقف Windows Update", (s,e) => {
                    if(IsWindows10OrNewer()) {
                        RunMultiCmd(new[] {
                            ("net", "stop wuauserv"),
                            ("sc", "config wuauserv start=disabled"),
                            ("reg", "add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v DoNotConnectToWindowsUpdateInternetLocations /t REG_DWORD /d 1 /f"),
                            ("reg", "add HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate /v DisableWindowsUpdateAccess /t REG_DWORD /d 1 /f")
                        });
                    } else {
                        RunMultiCmd(new[] {
                            ("net", "stop wuauserv"),
                            ("sc", "config wuauserv start=disabled")
                        });
                    }
                }));
            }

            panel.Controls.Add(CreateButton("Clear Browser Cache", (s,e) => {
                if(IsWindows10OrNewer()) {
                    RunMultiCmd(new[] {
                        ("RunDll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 255"),
                        ("taskkill", "/F /IM chrome.exe"),
                        ("taskkill", "/F /IM firefox.exe"), 
                        ("taskkill", "/F /IM msedge.exe"),
                        ("rd", "/s /q %LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Cache"),
                        ("rd", "/s /q %LOCALAPPDATA%\\Mozilla\\Firefox\\Profiles\\*.default\\cache2"),
                        ("rd", "/s /q %LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Cache")
                    });
                } else {
                    RunMultiCmd(new[] {
                        ("RunDll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 255"),
                        ("taskkill", "/F /IM iexplore.exe"),
                        ("rd", "/s /q %LOCALAPPDATA%\\Temporary Internet Files")
                    });
                }
            }));

            panel.Controls.Add(new Label { Text = "عیب‌یابی:", AutoSize = true, Margin = new Padding(5, 15, 5, 5), ForeColor = Color.White });
            if (IsVistaOrNewer()) {
                panel.Controls.Add(CreateButton("Windows Troubleshooter", (s,e) => RunCmd("control", "/name Microsoft.Troubleshooting")));
            }
            panel.Controls.Add(CreateButton("DirectX Diagnostic (dxdiag)", (s,e) => RunCmd("dxdiag", "")));
            panel.Controls.Add(CreateButton("System Information (msinfo32)", (s,e) => RunCmd("msinfo32", "")));
            panel.Controls.Add(CreateButton("Memory Diagnostic (mdsched)", (s,e) => RunCmd("mdsched", "")));
        }

        private void GetWindowsVersion(out string version, out string build) {
            version = "Unknown";
            build = "Unknown";
            try {
                string productName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString();
                string buildNumber = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", "").ToString();
                
                if (productName.Contains("Windows 11")) version = "11";
                else if (productName.Contains("Windows 10")) version = "10";
                else if (productName.Contains("Windows 8.1")) version = "8.1";
                else if (productName.Contains("Windows 8")) version = "8";
                else if (productName.Contains("Windows 7")) version = "7";
                else if (productName.Contains("Windows Vista")) version = "Vista";
                else if (productName.Contains("Windows XP")) version = "XP";
                
                build = buildNumber;
            }
            catch { }
        }

        private bool IsWindows10OrNewer() {
            return windowsVersion == "10" || windowsVersion == "11";
        }

        private bool IsVistaOrNewer() {
            return windowsVersion != "XP" && windowsVersion != "Unknown";
        }

        private Button CreateButton(string text, EventHandler onClick) {
            var btn = new Button {
                Text = text,
                Width = 380,
                Height = 40,
                Margin = new Padding(5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Black,
                ForeColor = Color.White,
                FlatAppearance = {
                    BorderColor = Color.DodgerBlue,
                    BorderSize = 1,
                    MouseOverBackColor = Color.DodgerBlue,
                    MouseDownBackColor = Color.RoyalBlue
                }
            };
            btn.Click += onClick;
            btn.MouseEnter += (s, e) => btn.ForeColor = Color.Black;
            btn.MouseLeave += (s, e) => btn.ForeColor = Color.White;
            return btn;
        }

        private void RunCmd(string file, string args) {
            try {
                var psi = new ProcessStartInfo(file, args) {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                var p = Process.Start(psi);
                if (p != null && !file.EndsWith(".msc") && !file.EndsWith(".cpl")) {
                    p.WaitForExit();
                    MessageBox.Show($"فرمان `{file} {args}` با کد خروج {p.ExitCode} اجرا شد", "نتیجه");
                }
            }
            catch (Exception ex) {
                MessageBox.Show("خطا: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunMultiCmd((string file, string args)[] commands) {
            try {
                foreach (var (file, args) in commands) {
                    var psi = new ProcessStartInfo(file, args) {
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    var p = Process.Start(psi);
                    if (p != null) {
                        p.WaitForExit();
                    }
                }
                MessageBox.Show("تمام دستورات با موفقیت اجرا شدند", "نتیجه");
            }
            catch (Exception ex) {
                MessageBox.Show("خطا: " + ex.Message, "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}