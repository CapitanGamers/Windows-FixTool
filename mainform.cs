using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Principal;

namespace FixTool
{
    public class LanguageDialog : Form
    {
        public bool SelectedPersian { get; private set; }

        public LanguageDialog()
        {
            this.Text = "Select Language / انتخاب زبان";
            this.Size = new Size(350, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            var lbl = new Label
            {
                Text = "Please choose your language\nلطفاً زبان خود را انتخاب کنید",
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            var btnFa = new Button { Text = "Persian (فارسی)", Size = new Size(130, 40), Location = new Point(30, 80), BackColor = Color.DodgerBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnEn = new Button { Text = "English", Size = new Size(130, 40), Location = new Point(180, 80), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnFa.Click += (s, e) => { SelectedPersian = true; this.DialogResult = DialogResult.OK; };
            btnEn.Click += (s, e) => { SelectedPersian = false; this.DialogResult = DialogResult.OK; };

            this.Controls.AddRange(new Control[] { lbl, btnFa, btnEn });
        }
    }

    public partial class MainForm : Form
    {
        private string winVer = "";
        private string winBuild = "";
        private string winDisplayVersion = "";
        private int winMajorVersion = 0;
        private bool isFa = false;

        public MainForm()
        {
            using var langDb = new LanguageDialog();
            if (langDb.ShowDialog() != DialogResult.OK)
                Environment.Exit(0);

            isFa = langDb.SelectedPersian;

            DetectWindowsVersion();

            if (winMajorVersion < 6)
            {
                string msg = isFa
                    ? "این ابزار روی ویندوز XP و Vista پشتیبانی محدودی دارد.\n\nآیا ادامه می‌دهید؟"
                    : "Limited support on Windows XP/Vista.\n\nContinue?";

                if (MessageBox.Show(msg, "هشدار", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    Environment.Exit(0);
            }

            InitializeComponents();
        }

        private void DetectWindowsVersion()
        {
            try
            {
                winVer = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "Unknown")?.ToString() ?? "Unknown";
                winBuild = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", "0")?.ToString() ?? "0";
                winDisplayVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion", "")?.ToString() ?? "";

                if (int.TryParse(winBuild, out int b) && b >= 22000 && winVer.Contains("Windows 10"))
                    winVer = "Windows 11";

                winMajorVersion = Environment.OSVersion.Version.Major;
            }
            catch
            {
                winVer = "Unknown";
                winBuild = "0";
            }
        }

        private bool IsWindows11() => int.TryParse(winBuild, out int b) && b >= 22000;
        private bool IsModernWindows() => winMajorVersion >= 6;

        private void InitializeComponents()
{
    string ver = $"{winVer}";
    if (!string.IsNullOrEmpty(winDisplayVersion)) ver += $" ({winDisplayVersion})";
    ver += $" - Build {winBuild}";

    this.Text = isFa ? $"ابزار FixTool ({ver})" : $"FixTool Pro ({ver})";
    this.Size = new Size(1100, 800);
    this.MinimumSize = new Size(950, 700);
    this.BackColor = Color.FromArgb(20, 20, 20);
    this.RightToLeft = isFa ? RightToLeft.Yes : RightToLeft.No;
    this.RightToLeftLayout = isFa;
    this.StartPosition = FormStartPosition.CenterScreen;

    var mainGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(15) };
    mainGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
    mainGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
    this.Controls.Add(mainGrid);

    var row1 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
    row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
    row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
    row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

    row1.Controls.Add(CreateCard(isFa ? "دستورات سیستمی" : "System Commands", new List<(string, Action<Button>)>
    {
        (isFa ? "بررسی و تعمیر دیسک (CHKDSK)" : "Check Disk (CHKDSK)", btn => RunCmd("cmd.exe", "/c chkdsk C: /f", btn)),
        (isFa ? "اسکن فایل‌های سیستمی (SFC)" : "System Scan (SFC)", btn => RunCmd("sfc.exe", "/scannow", btn)),
        (isFa ? "ترمیم ایمیج ویندوز (DISM)" : "Repair Image (DISM)", btn => RunCmd("DISM.exe", "/Online /Cleanup-Image /RestoreHealth", btn)),
        (isFa ? "بررسی سلامت کامل سیستم" : "Full System Repair (SFC + DISM)", btn => RunMulti(new[] { ("sfc.exe", "/scannow"), ("DISM.exe", "/Online /Cleanup-Image /RestoreHealth") }, btn)),
        (isFa ? "ریست تنظیمات بوت" : "Reset Boot Configuration", btn => RunMulti(new[] { ("cmd.exe", "/c bootrec /fixmbr"), ("cmd.exe", "/c bootrec /fixboot") }, btn)),
        (isFa ? "بررسی رم (mdsched)" : "RAM Diagnostic (mdsched)", btn => RunCmd("mdsched.exe", "", btn)),
        (isFa ? "ری‌استارت اکسپلورر" : "Restart Explorer", btn => RunCmd("cmd.exe", "/c taskkill /f /im explorer.exe & start explorer.exe", btn)),
        (isFa ? "پاک‌سازی صف پرینتر" : "Clear Print Spooler", btn => RunMulti(new[] { ("net", "stop spooler"), ("cmd.exe", "/c del /Q /F /S \"%systemroot%\\System32\\Spool\\Printers\\*.*\""), ("net", "start spooler") }, btn))
    }), 0, 0);

    row1.Controls.Add(CreateCard(isFa ? "مدیریت دستگاه و ویندوز" : "Windows Management", new List<(string, Action<Button>)>
    {
        (isFa ? "تسک منیجر" : "Task Manager", btn => RunCmd("taskmgr.exe", "", btn)),
        (isFa ? "ویرایشگر رجیستری" : "Registry Editor", btn => RunCmd("regedit.exe", "", btn)),
        (isFa ? "مدیریت دستگاه‌ها" : "Device Manager", btn => RunCmd("devmgmt.msc", "", btn)),
        (isFa ? "مدیریت کامپیوتر" : "Computer Management", btn => RunCmd("compmgmt.msc", "", btn)),
        (isFa ? "مشاهده رویدادها (Event Viewer)" : "Event Viewer", btn => RunCmd("eventvwr.msc", "", btn)),
        (isFa ? "مدیریت دیسک‌ها" : "Disk Management", btn => RunCmd("diskmgmt.msc", "", btn)),
        (isFa ? "مدیریت سرویس‌ها" : "Services", btn => RunCmd("services.msc", "", btn)),
        (isFa ? "کنترل پنل" : "Control Panel", btn => RunCmd("control.exe", "", btn))
    }), 1, 0);

    var maintenance = new List<(string, Action<Button>)>
    {
        (isFa ? "پاک‌سازی هوشمند هارد" : "Smart Disk Cleanup", btn => SmartDiskCleanup(btn)),
        (isFa ? "حذف فایل‌های Temp" : "Delete Temp Files", btn => { if (Confirm("حذف تمام فایل‌های موقت؟", "Delete all temp files?")) RunMulti(new[] { ("cmd.exe", "/c del /q /f /s \"%temp%\\*\""), ("cmd.exe", "/c del /q /f /s \"C:\\Windows\\Temp\\*\"") }, btn); }),
        (isFa ? "تخلیه سطل زباله" : "Empty Recycle Bin", btn => { if (Confirm("تخلیه سطل زباله؟", "Empty Recycle Bin?")) RunCmd("cmd.exe", "/c rd /s /q %systemdrive%\\$Recycle.bin", btn); }),
        (isFa ? "پاک‌سازی فایل‌های قدیمی ویندوز" : "Clean Old Windows Files", btn => RunCmd("cleanmgr.exe", "/sagerun:1", btn)),
        (isFa ? "حذف کش Microsoft Store" : "Clear Microsoft Store Cache", btn => RunCmd("wsreset.exe", "", btn)),
        (isFa ? "ریست استور ویندوز" : "Reset MS Store", btn => RunCmd("wsreset.exe", "", btn)),
        (isFa ? "پاک‌سازی کش مرورگرها" : "Clear Browser Cache", btn => { if (Confirm("پاک‌سازی کش مرورگر؟", "Clear browser cache?")) ClearBrowserCache(btn); })
    };
    if (IsModernWindows()) maintenance.Add((isFa ? "بهینه‌سازی درایو" : "Optimize Drive", btn => RunCmd("defrag.exe", "C: /O", btn)));

    row1.Controls.Add(CreateCard(isFa ? "نگهداری و پاک‌سازی" : "Maintenance & Cleanup", maintenance), 2, 0);

    mainGrid.Controls.Add(row1, 0, 0);

    var row2 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
    row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
    row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
    row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
    row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));

    row2.Controls.Add(CreateCard(isFa ? "شبکه و اینترنت" : "Network & Internet", new List<(string, Action<Button>)>
    {
        (isFa ? "پاک‌سازی کش DNS" : "Flush DNS", btn => RunCmd("ipconfig", "/flushdns", btn)),
        (isFa ? "نمایش وضعیت IP" : "IP Config All", btn => RunCmd("cmd.exe", "/k ipconfig /all", btn)),
        (isFa ? "ریست کامل شبکه" : "Network Reset", btn => { if (Confirm("ریست کامل شبکه؟", "Reset network?")) RunMulti(new[] { ("netsh", "int ip reset"), ("netsh", "winsock reset") }, btn); }),
        (isFa ? "ریست کامل Winsock + TCP/IP" : "Full Winsock + TCP/IP Reset", btn => RunMulti(new[] { ("netsh", "int ip reset"), ("netsh", "winsock reset"), ("netsh", "int tcp reset") }, btn)),
        (isFa ? "ریست فایروال" : "Firewall Reset", btn => RunCmd("netsh", "advfirewall reset", btn)),
        (isFa ? "تست پینگ گوگل" : "Ping Test", btn => RunCmd("cmd.exe", "/k ping 8.8.8.8 -t", btn)),
        (isFa ? "نمایش پورت‌ها و اتصالات" : "Show Open Ports (netstat)", btn => RunCmd("cmd.exe", "/k netstat -ano", btn))
    }), 1, 0);

    var advanced = new List<(string, Action<Button>)>
    {
        (isFa ? "تعمیر هوشمند آپدیت ویندوز" : "Smart Fix Windows Update", btn => SmartFixWindowsUpdate(btn)),
        (isFa ? "غیرفعال‌سازی آپدیت" : "Disable Win Update", btn => RunMulti(new[] { ("net", "stop wuauserv"), ("sc", "config wuauserv start=disabled") }, btn)),
        (isFa ? "اطلاعات سیستم" : "System Information", btn => RunCmd("msinfo32.exe", "", btn)),
        (isFa ? "System Configuration (msconfig)" : "System Configuration", btn => RunCmd("msconfig.exe", "", btn)),
        (isFa ? "مشاهده رویدادها (Event Viewer)" : "Event Viewer", btn => RunCmd("eventvwr.msc", "", btn))
    };
    if (IsModernWindows())
    {
        advanced.Add((isFa ? "بروزرسانی Policy" : "Force GPUpdate", btn => RunCmd("gpupdate.exe", "/force", btn)));
        advanced.Add((isFa ? "نسخه دایرکت ایکس" : "DirectX Diag", btn => RunCmd("dxdiag.exe", "", btn)));
    }

    row2.Controls.Add(CreateCard(isFa ? "تنظیمات پیشرفته" : "Advanced Settings", advanced), 2, 0);

    mainGrid.Controls.Add(row2, 0, 1);
}

private GroupBox CreateCard(string title, List<(string Text, Action<Button> Action)> items)
{
    GroupBox gb = new GroupBox
    {
        Text = "  " + title + "  ",
        Dock = DockStyle.Fill,
        ForeColor = Color.Gold,
        Font = new Font("Segoe UI", 11, FontStyle.Bold),
        Margin = new Padding(8),
        Padding = new Padding(10, 25, 10, 10),
        BackColor = Color.FromArgb(25, 25, 25)
    };

    FlowLayoutPanel pnl = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = false,
        Padding = new Padding(8)
    };

    void ResizeButtons()
    {
        int newWidth = pnl.ClientSize.Width - 20;
        if (newWidth < 150) newWidth = 150;

        foreach (Control ctrl in pnl.Controls)
        {
            if (ctrl is Button btn)
                btn.Width = newWidth;
        }
    }

    pnl.Resize += (s, e) => ResizeButtons();

    foreach (var item in items)
    {
        Button btn = new Button
        {
            Text = item.Text,
            Height = 40,
            Width = 270,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 0, 0, 6)
        };

        btn.FlatAppearance.BorderColor = Color.DodgerBlue;
        btn.FlatAppearance.MouseOverBackColor = Color.DodgerBlue;
        btn.FlatAppearance.BorderSize = 1;

        btn.Click += (s, e) => item.Action(btn);
        pnl.Controls.Add(btn);
    }

    pnl.SizeChanged += (s, e) => ResizeButtons();

    gb.Controls.Add(pnl);
    return gb;
}

        private bool Confirm(string fa, string en)
        {
            return MessageBox.Show(isFa ? fa : en, isFa ? "تأیید" : "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private async void RunCmd(string file, string args, Button sender)
        {
            string original = sender.Text;
            try
            {
                sender.Text = isFa ? "⏳ در حال اجرا..." : "⏳ Running...";
                sender.Enabled = false;

                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo(file, args) { UseShellExecute = true, Verb = "runas" };
                    using var p = Process.Start(psi);
                    if (p != null && !args.Contains("/k") && !file.EndsWith(".msc"))
                        p.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(isFa ? $"خطا: {ex.Message}" : ex.Message);
            }
            finally
            {
                sender.Text = original;
                sender.Enabled = true;
            }
        }

        private async void RunMulti((string file, string args)[] commands, Button sender)
        {
            string original = sender.Text;
            try
            {
                sender.Text = isFa ? "⏳ منتظر بمانید..." : "⏳ Please wait...";
                sender.Enabled = false;

                await Task.Run(() =>
                {
                    foreach (var (file, args) in commands)
                    {
                        try
                        {
                            var psi = new ProcessStartInfo(file, args) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
                            Process.Start(psi)?.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(isFa ? $"خطا در {file}: {ex.Message}" : ex.Message);
                        }
                    }
                });
                MessageBox.Show(isFa ? "عملیات با موفقیت انجام شد." : "Completed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(isFa ? ex.Message : ex.Message);
            }
            finally
            {
                sender.Text = original;
                sender.Enabled = true;
            }
        }

        private void ClearBrowserCache(Button btn)
        {
            RunMulti(new[]
            {
                ("cmd.exe", "/c taskkill /F /IM chrome.exe /T"),
                ("cmd.exe", "/c rd /s /q \"%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Cache\""),
                ("cmd.exe", "/c taskkill /F /IM msedge.exe /T"),
                ("cmd.exe", "/c rd /s /q \"%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Cache\"")
            }, btn);
        }

        private void SmartFixWindowsUpdate(Button btn)
        {
            if (!Confirm("تعمیر آپدیت ویندوز؟ (ممکن است چند دقیقه طول بکشد)", "Fix Windows Update? (May take a few minutes)")) return;

            if (IsWindows11())
            {
                RunMulti(new[]
                {
                    ("net", "stop wuauserv"), ("net", "stop bits"), ("net", "stop cryptSvc"),
                    ("cmd.exe", "/c rd /s /q \"%windir%\\SoftwareDistribution\""),
                    ("cmd.exe", "/c rd /s /q \"%windir%\\System32\\catroot2\""),
                    ("net", "start cryptSvc"), ("net", "start bits"), ("net", "start wuauserv")
                }, btn);
            }
            else if (IsModernWindows())
            {
                RunMulti(new[]
                {
                    ("net", "stop wuauserv"), ("net", "stop bits"),
                    ("cmd.exe", "/c rd /s /q \"%windir%\\SoftwareDistribution\""),
                    ("net", "start wuauserv"), ("net", "start bits")
                }, btn);
            }
            else
            {
                MessageBox.Show(isFa ? "این دستور روی ویندوز شما پشتیبانی نمی‌شود." : "Not supported on this Windows version.");
            }
        }

        private void SmartDiskCleanup(Button btn)
        {
            if (winMajorVersion >= 10 && Confirm("استفاده از Storage Sense؟ (توصیه می‌شود)", "Use Storage Sense? (Recommended)"))
            {
                RunCmd("cmd.exe", "/c start ms-settings:storagesense", btn);
                return;
            }
            RunCmd("cleanmgr.exe", "/sagerun:1", btn);
        }
    }
}
