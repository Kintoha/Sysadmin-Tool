using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.Management;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

enum RecycleFlags : int
{
    SHERB_NOCONFIRMATION = 0x00000001,
    SHERB_NOPROGRESSUI = 0x00000001,
    SHERB_NOSOUND = 0x00000004
}

namespace Project_23
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Определение, установлен ли MS Word.
            using (var regWord = Registry.ClassesRoot.OpenSubKey("Word.Application"))
            {
                if (regWord == null) { tbMSWord.Text = "Не установлен"; }
                else { tbMSWord.Text = "Установлен"; }
            }

            // Определение версии Java.
            rtbJava.Text = getJavaVersionInformation();

            // Определение версии DirectX.
            GetDirectxMajorVersion();

            // Определение версии Internet Explorer.
            tbIE.Text = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Internet Explorer").GetValue("Version").ToString();

            // Информация о процессоре.
            determineCPU();

            // Определение материнской платы.
            ManagementObjectSearcher motherboard = new ManagementObjectSearcher("root\\CIMV2", "SELECT SerialNumber, Manufacturer, Product, Version FROM Win32_BaseBoard");
            foreach (ManagementObject item in motherboard.Get())
            {
                tbManufacturer.Text = item["Manufacturer"].ToString();
                tbProduct.Text = item["Product"].ToString();
                tbSerialNumber.Text = item["SerialNumber"].ToString();
            }

            // Определение ОС.
            tbOSVersion.Text = Environment.OSVersion.ToString();

            // Определение имени компьютера.
            tbNamePC.Text += Environment.MachineName.ToString();
           
            // Оределение имени пользователя.
            tbUser.Text += Environment.UserName.ToString();

            // Информация о видеокартах.
            determineVideoController();

            // Информация об ОЗУ.
            determinePhysicalMemory();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Определение свободного ОЗУ.
            PerformanceCounter ram = new PerformanceCounter("Memory", "Available MBytes");
            tsslRAM.Text = "Свободно RAM: " + ram.NextValue().ToString() + " МБ ";

            // Определение времени работы системы.
            int systemUptime = Environment.TickCount;
            tbSysTime.Text = Convert.ToString(systemUptime / 1000) + " сек. (" + Convert.ToString(systemUptime / 1000 / 60 / 60) + " ч.)";

            // Определение загрузки ЦП.
            tspbCPU.Value = (int)(performanceCounter1.NextValue());
            tsslCPU.Text = " Загрузка CPU: " + tspbCPU.Value.ToString() + "% ";
        }

        #region VIDEO CONTROLLER

        private void determineVideoController()
        {
            ManagementObjectSearcher VideoController = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController");
            foreach (ManagementObject queryObj in VideoController.Get())
            {
                lvVideoController.Items.Add(new ListViewItem(new string[] 
                { 
                    Convert.ToString(queryObj["Caption"]), Convert.ToString((Math.Round(System.Convert.ToDouble(queryObj["AdapterRAM"]) / 1024 / 1024, 2)) + " MB") 
                }));
            }

        }

        #endregion VIDEO CONTROLLER

        #region PHYSICAL MEMORY

        private void determinePhysicalMemory()
        {
            ManagementObjectSearcher PhysicalMemory = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PhysicalMemory");
            foreach (ManagementObject queryObj in PhysicalMemory.Get())
            {
                lvPhysicalMemory.Items.Add(new ListViewItem(new string[] 
                { 
                    Convert.ToString(queryObj["BankLabel"]), (Math.Round(System.Convert.ToDouble(queryObj["Capacity"]) / 1024 / 1024 / 1024, 2) + " GB"), Convert.ToString(queryObj["Speed"]) 
                }));
            }
        }

        #endregion PHYSICAL MEMORY

        #region CPU

        private void determineCPU()
        {
            ManagementObjectSearcher CPU = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
            foreach (ManagementObject queryObj in CPU.Get())
            {
                tbCPU.Text = Convert.ToString(queryObj["Name"]);
            }

            tbProcessorCount.Text = Environment.ProcessorCount.ToString();
        }

        #endregion CPU

        #region DIRECTX

        private void GetDirectxMajorVersion()
        {
            int directxMajorVersion = 0;

            var OSVersion = Environment.OSVersion;

            if (OSVersion.Version.Major >= 6)
            {
                if (OSVersion.Version.Major > 6 || OSVersion.Version.Minor >= 1)
                {
                    directxMajorVersion = 11;
                }
                else
                {
                    directxMajorVersion = 10;
                }
            }
            else
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
                {
                    string versionStr = key.GetValue("Version") as string;
                    if (!string.IsNullOrEmpty(versionStr))
                    {
                        var versionComponents = versionStr.Split('.');
                        if (versionComponents.Length > 1)
                        {
                            int directXLevel;
                            if (int.TryParse(versionComponents[1], out directXLevel))
                            {
                                directxMajorVersion = directXLevel;
                            }
                        }
                    }
                }
            }
            tbDirectX.Text = directxMajorVersion.ToString();
        }

        #endregion DIRECTX

        #region NETWORK

        private void getCurrentIP()
        {
            string serviceURL = "http://2ip.ru/";
            string IP = "";
            try
            {
                tbIP.Invoke(new Action<string>((s) => tbIP.Text = s), "Определяется...");
                WebClient wc = new WebClient();
                string requestResult = Encoding.UTF8.GetString(wc.DownloadData(serviceURL));
                if (!string.IsNullOrEmpty(requestResult))
                {
                    string[] split1 = requestResult.ToUpper().Split(new string[] { "BIG" }, new StringSplitOptions());
                    split1 = split1[1].Split('<', '>');
                    IP = split1[1];
                }
                tbIP.Invoke(new Action<string>((s) => tbIP.Text = s), IP);
            }
            catch
            {
                tbIP.Invoke(new Action<string>((s) => tbIP.Text = s), "Не удаётся определить внешний IP");
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            networkSettings();

            InternetConnection inet = new InternetConnection();
            inet.Init();
            if (inet.IsInternetConnected == true)
            {
                lOnline.Text = "Доступ в интернет";
            }
            else { lOnline.Text = "Без доступа в интернет"; }
        }

        private void determineIP_Click(object sender, EventArgs e)
        {
            new Thread(() => getCurrentIP()).Start();
        }

        private void networkSettings()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    tbMAC.Text = nic.GetPhysicalAddress().ToString();
                    break;
                }
            }

            tbLocalIP.Text = System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList[0].ToString();

            InternetConnection inet = new InternetConnection();
            inet.Init();
            if (inet.IsUsingModem == true) { tbModem.Text = "Да"; }
            else { tbModem.Text = "Нет"; }

            if (inet.IsUsingLAN == true) { tbLAN.Text = "Да"; }
            else { tbLAN.Text = "Нет"; }

            if (inet.IsUsingProxy == true) { tbProxy.Text = "Да"; }
            else { tbProxy.Text = "Нет"; }

            if (inet.IsRasEnabled == true) { tbRAS.Text = "Да"; }
            else { tbRAS.Text = "Нет"; }
        }

        #endregion NETWORK

        #region JAVA

        static string getJavaVersionInformation()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("java", "-version");

                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                return proc.StandardError.ReadToEnd();

            }
            catch
            {
                return "Java: не установлена";
            }
        }

        #endregion JAVA

        #region OTHER

        #region EMPTY TRASH

        [DllImport("Shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        private void tsmEmptyTrash_Click(object sender, EventArgs e)
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOSOUND | RecycleFlags.SHERB_NOCONFIRMATION);
        }
        
        #endregion EMPTY TRASH

        private void tsmScreenshot_Click(object sender, EventArgs e)
        {
            if (saveScreenshot.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                Thread.Sleep(2000);
                gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
                bmpScreenshot.Save(saveScreenshot.FileName, ImageFormat.Png);
            }
        }

        private void tsmExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion OTHER

        #region FAST START

        private void tsmTaskManagerWindows_Click(object sender, EventArgs e)
        {
            Process.Start("taskmgr");
        }

        private void tsmControlPanel_Click(object sender, EventArgs e)
        {
            Process.Start("control");
        }

        private void tsmCMD_Click(object sender, EventArgs e)
        {
            Process.Start("cmd");
        }

        private void tsmUAC_Click(object sender, EventArgs e)
        {
            Process.Start("UserAccountControlSettings");
        }

        private void tsmOWBP_Click(object sender, EventArgs e)
        {
            Process.Start("SystemPropertiesPerformance.exe");
        }

        private void tsmSoundRecorder_Click(object sender, EventArgs e)
        {
            Process.Start("SoundRecorder");
        }

        private void tsmSndVol_Click(object sender, EventArgs e)
        {
            Process.Start("SndVol");
        }

        private void tsmServices_Click(object sender, EventArgs e)
        {
            Process.Start("services.msc");
        }

        private void tsmResMon_Click(object sender, EventArgs e)
        {
            Process.Start("resmon");
        }

        private void tsmRegedt32_Click(object sender, EventArgs e)
        {
            Process.Start("regedt32");
        }

        private void tsmPerfMon_Click(object sender, EventArgs e)
        {
            Process.Start("perfmon.msc");
        }

        private void tsmOSK_Click(object sender, EventArgs e)
        {
            Process.Start("osk");
        }

        private void tsmOptionalFeatures_Click(object sender, EventArgs e)
        {
            Process.Start("OptionalFeatures");
        }

        private void MSTsc_Click(object sender, EventArgs e)
        {
            Process.Start("mstsc");
        }

        private void tsmMSInfo32_Click(object sender, EventArgs e)
        {
            Process.Start("msinfo32");
        }

        private void tsmMSConfig_Click(object sender, EventArgs e)
        {
            Process.Start("msconfig");
        }

        private void tsmEventVWR_Click(object sender, EventArgs e)
        {
            Process.Start("eventvwr.msc");
        }

        private void tsmDevMgmt_Click(object sender, EventArgs e)
        {
            Process.Start("devmgmt.msc");
        }

        private void tsmCompMgmt_Click(object sender, EventArgs e)
        {
            Process.Start("compmgmt.msc");
        }

        private void bScreenSaver_Click(object sender, EventArgs e)
        {
            Process.Start("ssText3d.scr");
        }

        #endregion FAST START

        #region POWER MANAGEMENT

        private void bReboot_Click(object sender, EventArgs e)
        {
            PowerManagement.halt(true, false);
        }

        private void bBlock_Click(object sender, EventArgs e)
        {
            PowerManagement.Lock();
        }

        private void bPower_Click(object sender, EventArgs e)
        {
            PowerManagement.halt(false, false);
        }

        #endregion POWER MANAGEMENT

    }
}