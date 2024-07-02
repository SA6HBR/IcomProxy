using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Media;
using System.Windows.Forms;

namespace Icom_Proxy
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
         }
        private void Form1_Load(object sender, EventArgs e)
        {
            
            ToolTip ToolTip1 = new ToolTip();
            ToolTip1.SetToolTip(this.com0comButton, "com0com");
            ToolTip1.SetToolTip(this.SoundButton, "Sound control");
            ToolTip1.SetToolTip(this.DeviceButton, "Device manager");
            ToolTip1.SetToolTip(this.RefreshUsbButton, "Refresh Usblist");

            ComPortUpdate("Start");

            Program1SerialPort.PinChanged += new SerialPinChangedEventHandler(Program1SerialPort_PinChanged);
            Program2SerialPort.PinChanged += new SerialPinChangedEventHandler(Program2SerialPort_PinChanged);
            Program3SerialPort.PinChanged += new SerialPinChangedEventHandler(Program3SerialPort_PinChanged);

            RadioSerialPort.ReadTimeout = RadioSerialPort.WriteTimeout = 1000;
            Program1SerialPort.ReadTimeout = Program1SerialPort.WriteTimeout = 1000;
            Program2SerialPort.ReadTimeout = Program2SerialPort.WriteTimeout = 1000;
            Program3SerialPort.ReadTimeout = Program3SerialPort.WriteTimeout = 1000;

            RadioBackgroundWorker.WorkerSupportsCancellation = true;
            RadioBackgroundWorker.WorkerReportsProgress = true;
            RadioBackgroundWorker.DoWork += RadioBackgroundWorker_DoWork;
            RadioBackgroundWorker.ProgressChanged += RadioBackgroundWorker_ProgressChanged;
            
            Program1BackgroundWorker.WorkerSupportsCancellation = true;
            Program1BackgroundWorker.WorkerReportsProgress = true;
            Program1BackgroundWorker.DoWork += Program1BackgroundWorker_DoWork;
            Program1BackgroundWorker.ProgressChanged += Program1BackgroundWorker_ProgressChanged;

            Program2BackgroundWorker.WorkerSupportsCancellation = true;
            Program2BackgroundWorker.WorkerReportsProgress = true;
            Program2BackgroundWorker.DoWork += Program2BackgroundWorker_DoWork;
            Program2BackgroundWorker.ProgressChanged += Program2BackgroundWorker_ProgressChanged;

            Program3BackgroundWorker.WorkerSupportsCancellation = true;
            Program3BackgroundWorker.WorkerReportsProgress = true;
            Program3BackgroundWorker.DoWork += Program3BackgroundWorker_DoWork;
            Program3BackgroundWorker.ProgressChanged += Program3BackgroundWorker_ProgressChanged;

            TimerTX.Tick += TimerTX_of;

            TimerFindMyRadio.Tick += TimerFindMyRadioLoop;
            TimerFindMyRadio.Interval = 200;

            TimerDummyLoad.Tick += TimerDummyLoadLoop;
            TimerDummyLoad.Interval = 10000;

            TimerCheckProgram.Tick += TimerCheckProgramLoop;
            TimerCheckProgram.Interval = 5000;
            TimerCheckProgram.Enabled = true;

            this.Text = Version.NameAndNumber;

            SerialPort_value.saveSettings = 0;
            GetSettings();
            SerialPort_value.saveSettings = 1;
            ProgramInfoBoxes_Update("", null);
            ToneInfoUpdate();
            tabControl1.TabPages.Remove(tabPageTone);
        }

        public static SerialPort RadioSerialPort = new SerialPort();
        public static SerialPort Program1SerialPort = new SerialPort();
        public static SerialPort Program2SerialPort = new SerialPort();
        public static SerialPort Program3SerialPort = new SerialPort();

        public static System.Windows.Forms.Timer TimerTX = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer TimerFindMyRadio = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer TimerDummyLoad = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer TimerCheckProgram = new System.Windows.Forms.Timer();

        public static BackgroundWorker RadioBackgroundWorker = new BackgroundWorker();
        public static BackgroundWorker Program1BackgroundWorker = new BackgroundWorker();
        public static BackgroundWorker Program2BackgroundWorker = new BackgroundWorker();
        public static BackgroundWorker Program3BackgroundWorker = new BackgroundWorker();

        private System.Media.SoundPlayer sPlayer;
 
        static class SerialPort_value
        {
            public static string portRadioName = "Radio";
            public static string portProgram1Name = "Program 1";
            public static string portProgram2Name = "Program 2";
            public static string portProgram3Name = "Program 3";

            public static int saveSettings = 0;
            
            public static int statusRadioReceiving;
            public static int statusProgram1Receiving;
            public static int statusProgram2Receiving;
            public static int statusProgram3Receiving;

            public static byte[] dataRadioReceiving = new byte[0];
            public static byte[] dataProgram1Receiving = new byte[0];
            public static byte[] dataProgram2Receiving = new byte[0];
            public static byte[] dataProgram3Receiving = new byte[0];

        }
        public class SerialPort_Data
        {
            public byte[] Data_sp1 { get; set; }
            public byte[] Data_sp2 { get; set; }
            public byte[] Data_sp3 { get; set; }
            public byte[] Data_sp4 { get; set; }
        }

        private void DebugLogTextInsert(string program, string text)
        {
            string timestamp = "";
            string timestampFormat = "yyyy-MM-dd HH:mm:ss";

            if(LogDateCheckBox.Checked && !LogTimeCheckBox.Checked) { timestampFormat = "yyyy-MM-dd"; }
            else if (!LogDateCheckBox.Checked && LogTimeCheckBox.Checked) { timestampFormat = "HH:mm:ss"; }

            if (LogDateCheckBox.Checked || LogTimeCheckBox.Checked) { timestamp = DateTime.Now.ToString(timestampFormat) + " - "; }
            
            DebugLogTextBox.Text = DebugLogTextBox.Text.Insert(0, timestamp + program.PadRight(10) + ": " + text + "\r\n");
            DebugLogRemoveLines(int.Parse(DebugLogLinesTextBox.Text));
        }
        private void DebugLogRemoveLines(int rows)
        {
  
            if (DebugLogTextBox.Lines.Length > rows)
            {
                DebugLogTextBox.Lines = DebugLogTextBox.Lines.Take(rows).ToArray();
            }
        }
        private void ComPortUpdate(string program)
        {
            int com0comCount = 0;
            int comCount = 0;
            
            RadioComPortList.Items.Clear();
            Program1Com0comList.Items.Clear();
            Program2Com0comList.Items.Clear();
            Program3Com0comList.Items.Clear();

            RadioComPortList.Sorted = true;
            Program1Com0comList.Sorted = true;
            Program2Com0comList.Sorted = true;
            Program3Com0comList.Sorted = true;

//            RadioComPortList.Items.AddRange(SerialPort.GetPortNames());

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\cimv2",
                "SELECT * FROM Win32_PnPEntity where Caption is not null");

            foreach (ManagementObject CNCA in searcher.Get())
            {
                if (CNCA["Caption"].ToString().IndexOf("(COM") > 0 && CNCA["Caption"].ToString().ToLower().IndexOf("com0com") == -1)
                {

                    int startA = CNCA["Caption"].ToString().IndexOf("(COM") + 1;
                    int endA = CNCA["Caption"].ToString().IndexOf(")", startA);
                    string comA = CNCA["Caption"].ToString().Substring(startA, endA - startA).PadRight(5);
                    RadioComPortList.Items.Add(comA);

                    DebugLogTextInsert(CNCA["Manufacturer"].ToString(), comA);

                    comCount += 1;
                }

                if (CNCA["Caption"].ToString().IndexOf("(COM") == -1 && CNCA["DeviceID"].ToString().Length > 23)
                {
                    if (CNCA["DeviceID"].ToString().Substring(0,23) == @"USB\VID_0557&PID_2008\5")
                    {
                        DebugLogTextInsert("Driver missing", "ATEN USB to Serial Bridge");
                    }
                    else if (CNCA["DeviceID"].ToString() == @"FTDIBUS\VID_0403+PID_6001+A50285BIA\0000")
                    {
                        DebugLogTextInsert("Driver missing", "FTDI USB Serial Port");
                    }
                    else if (CNCA["DeviceID"].ToString() == @"USB\VID_10C4&PID_EA60\0001")
                    {
                        DebugLogTextInsert("Driver missing", "Silicon Labs CP210x");
                    }
                    else if (CNCA["DeviceID"].ToString() == @"USB\VID_067B&PID_2303\5&399BC254&0&5")
                    {
                        DebugLogTextInsert("Driver missing", "Prolific - Don't use this");
                    }
                }

                if (CNCA["DeviceID"].ToString().IndexOf("CNCA") > 0)
                {
                    foreach (ManagementObject CNCB in searcher.Get())
                    {
                        try
                        {
                            if (CNCB["DeviceID"].ToString().IndexOf("CNCB") > 0 && CNCA["DeviceID"].ToString().Substring(17) == CNCB["DeviceID"].ToString().Substring(17))
                            {
                                int startA = CNCA["Caption"].ToString().IndexOf("(COM") + 1;
                                int startB = CNCB["Caption"].ToString().IndexOf("(COM") + 1;
                                int endA = CNCA["Caption"].ToString().IndexOf(")", startA);
                                int endB = CNCB["Caption"].ToString().IndexOf(")", startB);
                                string comA = CNCA["Caption"].ToString().Substring(startA, endA - startA).PadRight(5);
                                string comB = CNCB["Caption"].ToString().Substring(startB, endB - startB).PadRight(5);
                                string com0comPair = comA + " - " + comB;
                                DebugLogTextInsert("com0com", com0comPair);

                                Program1Com0comList.Items.Add(com0comPair);
                                Program2Com0comList.Items.Add(com0comPair);
                                Program3Com0comList.Items.Add(com0comPair);

                                com0comCount += 1;
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
            }

            if (com0comCount == 0)
            {
                DebugLogTextInsert("Error", "No com0com for program");
            }

            if (comCount == 0)
            {
                DebugLogTextInsert("Error", "No com-port for radio");
            }

            DebugLogTextInsert(program, "Check Comport");

        }
        private void TimerTX_of(object sender, EventArgs e)
        {
            this.FunctionTX(false, "Timer");
            TimerTX.Enabled = false;
        }
        private void TimerFindMyRadioLoop(object sender, EventArgs e)
        {
            var Initiate = new byte[Radio.CIV.Initiate.Length];

            Initiate = Radio.CIV.Initiate;

            Initiate[2] = Radio.CIV.Adress;


            if (RadioHexTextBox.TextLength == 0 && Radio.CIV.Adress != 0xff && RadioSerialPort.IsOpen)
            {
                DebugLogTextInsert("FindMyRadio", BitConverter.ToString(Initiate, 0, Initiate.Length).Replace("-", " "));
                RadioSerialPortWrite(Initiate, 0, Initiate.Length);
            }
            else if (RadioHexTextBox.TextLength > 0)
            {
                DebugLogTextInsert("CI-V Adress", RadioHexTextBox.Text);
                TimerFindMyRadio.Enabled = false;
            }
            else
            {
                DebugLogTextInsert("Error", "No CI-V Adress!");
                TimerFindMyRadio.Enabled = false;
            }

            Radio.CIV.Adress +=1;
        }
        private void TimerDummyLoadLoop(object sender, EventArgs e)
        {
            TimerDummyLoad.Enabled = false;
            this.StopDummyLoad();
        }
        private void TimerCheckProgramLoop(object sender, EventArgs e)
        {
            var procs = Process.GetProcesses();
            string procsTitle = "Icom-Proxy";
            int procsCount = 0;

            foreach (var proc in procs)
            {
                if (proc.MainWindowTitle.Length >= procsTitle.Length && proc.MainWindowTitle.Substring(0, procsTitle.Length) == procsTitle)
                {
                    procsCount += 1;
                }

                //                DebugLogTextInsert("xxx", proc.MainWindowTitle);


            }

            if (procsCount > 1)
            {
                DebugLogTextInsert("Error", "More than one " + procsTitle);
                TimerCheckProgram.Enabled = false;
                MessageBox.Show(
                                "Only one " + procsTitle,
                                "ERROR",
                                MessageBoxButtons.OK,
                                //MessageBoxIcon.Warning // for Warning  
                                MessageBoxIcon.Error // for Error 
                                                     //MessageBoxIcon.Information  // for Information
                                                     //MessageBoxIcon.Question // for Question
                                );
                Application.Exit();
            }
        }

        private static void RadioBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!RadioBackgroundWorker.CancellationPending)
            {
                try
                {
                    if (RadioSerialPort.IsOpen)
                    {
                      var c = RadioSerialPort.Read(buffer, 0, buffer.Length);
                      RadioBackgroundWorker.ReportProgress(0, new SerialPort_Data() { Data_sp1 = buffer.Take(c).ToArray() });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("RadioBackgroundWorker_DoWork: " + ex.Message);
                }
            }

        }
        private static void Program1BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!Program1BackgroundWorker.CancellationPending)
            {
                try
                {
                    if (Program1SerialPort.IsOpen)
                    {
                       var c = Program1SerialPort.Read(buffer, 0, buffer.Length);
                       Program1BackgroundWorker.ReportProgress(0, new SerialPort_Data() { Data_sp2 = buffer.Take(c).ToArray() });

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Program1BackgroundWorker_DoWork: " + ex.Message);
                }
            }

        }
        private static void Program2BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!Program2BackgroundWorker.CancellationPending)
            {
                try
                {
                    if (Program2SerialPort.IsOpen)
                    {
                        var c = Program2SerialPort.Read(buffer, 0, buffer.Length);
                        Program2BackgroundWorker.ReportProgress(0, new SerialPort_Data() { Data_sp3 = buffer.Take(c).ToArray() });

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Program2BackgroundWorker_DoWork: " + ex.Message);
                }
            }

        }
        private static void Program3BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!Program3BackgroundWorker.CancellationPending)
            {
                try
                {
                    if (Program3SerialPort.IsOpen)
                    {
                        var c = Program3SerialPort.Read(buffer, 0, buffer.Length);
                        Program3BackgroundWorker.ReportProgress(0, new SerialPort_Data() { Data_sp4 = buffer.Take(c).ToArray() });

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Program3BackgroundWorker_DoWork: " + ex.Message);
                }
            }

        }

        private void RadioBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp1.Length; i++)
            {
                if (sp.Data_sp1[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.statusRadioReceiving += 1;
                }
                if (SerialPort_value.statusRadioReceiving > 0 && sp.Data_sp1.Length > i)
                {
                    Array.Resize(ref SerialPort_value.dataRadioReceiving, SerialPort_value.dataRadioReceiving.Length + 1);
                    SerialPort_value.dataRadioReceiving[SerialPort_value.dataRadioReceiving.Length - 1] = sp.Data_sp1[i];
                }
                if (sp.Data_sp1[i].ToString("x2").Equals("fd"))
                {
                    if (Program1SerialPort.IsOpen && Program1FeedBackCheckBox.Checked)
                    {
                        try
                        {
                            
                           Program1SerialPort.Write(SerialPort_value.dataRadioReceiving, 0, SerialPort_value.dataRadioReceiving.Length);
                        }
                        catch (Exception)
                        {
                            DebugLogTextInsert(SerialPort_value.portProgram1Name, "Write-error");
                        }
                    }
                    if (Program2SerialPort.IsOpen && Program2FeedBackCheckBox.Checked)
                    {
                        try
                        {
                            Program2SerialPort.Write(SerialPort_value.dataRadioReceiving, 0, SerialPort_value.dataRadioReceiving.Length);
                        }
                        catch (Exception)
                        {
                            DebugLogTextInsert(SerialPort_value.portProgram2Name, "Write-error");
                        }
                    }
                    if (Program3SerialPort.IsOpen && Program3FeedBackCheckBox.Checked)
                    {
                        try
                        {
                            Program3SerialPort.Write(SerialPort_value.dataRadioReceiving, 0, SerialPort_value.dataRadioReceiving.Length);
                        }
                        catch (Exception)
                        {
                            DebugLogTextInsert(SerialPort_value.portProgram3Name, "Write-error");
                        }
                    }
                    
                
                    if (RadioHexTextBox.Text == "" 
                            && SerialPort_value.dataRadioReceiving[3].ToString("X2") != "00" 
                            && SerialPort_value.dataRadioReceiving[3].ToString("X2") != "E0")
                    {
                        RadioHexTextBox.Text = SerialPort_value.dataRadioReceiving[3].ToString("X2");
                    }

                    if (SerialPort_value.dataRadioReceiving.Length >= 4 &&
                        RadioHexTextBox.Text != SerialPort_value.dataRadioReceiving[3].ToString("X2")
                            && RadioHexTextBox.Text != SerialPort_value.dataRadioReceiving[4].ToString("X2")
                            && SerialPort_value.dataRadioReceiving[3].ToString("X2") != "E0" 
                            && RadioHexTextBox.TextLength > 0)
                    {
                        DebugLogTextInsert(SerialPort_value.portRadioName + " : Error HEX? ", RadioHexTextBox.Text + " <> " + SerialPort_value.dataRadioReceiving[3].ToString("X2"));
                    }

                    SerialPort_value.statusRadioReceiving = 0;
                    DebugLogTextInsert(SerialPort_value.portRadioName, BitConverter.ToString(SerialPort_value.dataRadioReceiving, 0, SerialPort_value.dataRadioReceiving.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.dataRadioReceiving, 0, SerialPort_value.dataRadioReceiving.Length);
                    Array.Resize(ref SerialPort_value.dataRadioReceiving, 0);
                }
                
            }
        }
        private void Program1BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp2.Length; i++)
            {
                if (sp.Data_sp2[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.statusProgram1Receiving += 1;
                }
                if (SerialPort_value.statusProgram1Receiving > 0 && sp.Data_sp2.Length > i)
                {
                    Array.Resize(ref SerialPort_value.dataProgram1Receiving, SerialPort_value.dataProgram1Receiving.Length + 1);
                    SerialPort_value.dataProgram1Receiving[SerialPort_value.dataProgram1Receiving.Length - 1] = sp.Data_sp2[i];
                }
                if (sp.Data_sp2[i].ToString("x2").Equals("fd"))
                {
                    if (RadioHexTextBox.TextLength > 0 & checkBoxForceCIV.Checked)
                    {
                        SerialPort_value.dataProgram1Receiving[2] = Convert.ToByte(RadioHexTextBox.Text, 16);
                    }

                    if (RadioSerialPort.IsOpen) RadioSerialPortWrite(SerialPort_value.dataProgram1Receiving, 0, SerialPort_value.dataProgram1Receiving.Length);

                    FunctionSendCIV(SerialPort_value.dataProgram1Receiving, SerialPort_value.portProgram1Name);

                    if (RadioHexTextBox.Text != SerialPort_value.dataProgram1Receiving[2].ToString("X2") && RadioHexTextBox.Text != "" && checkBoxForceCIV.Checked == false)
                    {
                        DebugLogTextInsert(SerialPort_value.portProgram1Name + " : Error HEX? ", RadioHexTextBox.Text + " <> " + SerialPort_value.dataProgram1Receiving[2].ToString("X2"));
                    }

                    SerialPort_value.statusProgram1Receiving = 0;
                    DebugLogTextInsert(SerialPort_value.portProgram1Name, BitConverter.ToString(SerialPort_value.dataProgram1Receiving, 0, SerialPort_value.dataProgram1Receiving.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.dataProgram1Receiving, 0, SerialPort_value.dataProgram1Receiving.Length);
                    Array.Resize(ref SerialPort_value.dataProgram1Receiving, 0);
                }
            }
        }
        private void Program2BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp3.Length; i++)
            {
                if (sp.Data_sp3[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.statusProgram2Receiving += 1;
                }
                if (SerialPort_value.statusProgram2Receiving > 0 && sp.Data_sp3.Length > i)
                {
                    Array.Resize(ref SerialPort_value.dataProgram2Receiving, SerialPort_value.dataProgram2Receiving.Length + 1);
                    SerialPort_value.dataProgram2Receiving[SerialPort_value.dataProgram2Receiving.Length - 1] = sp.Data_sp3[i];
                }
                if (sp.Data_sp3[i].ToString("x2").Equals("fd"))
                {
                    if (RadioHexTextBox.TextLength > 0 && checkBoxForceCIV.Checked)
                    {
                        SerialPort_value.dataProgram2Receiving[2] = Convert.ToByte(RadioHexTextBox.Text, 16);
                    }
                    
                    if (RadioSerialPort.IsOpen) RadioSerialPortWrite(SerialPort_value.dataProgram2Receiving, 0, SerialPort_value.dataProgram2Receiving.Length);

                    FunctionSendCIV(SerialPort_value.dataProgram2Receiving, SerialPort_value.portProgram2Name);

                    if (RadioHexTextBox.Text != SerialPort_value.dataProgram2Receiving[2].ToString("X2") && RadioHexTextBox.Text != "" && checkBoxForceCIV.Checked == false)
                    {
                        DebugLogTextInsert(SerialPort_value.portProgram2Name + " : Error HEX? ", RadioHexTextBox.Text + " <> " + SerialPort_value.dataProgram2Receiving[2].ToString("X2"));
                    }
                    SerialPort_value.statusProgram2Receiving = 0;
                    DebugLogTextInsert(SerialPort_value.portProgram2Name, BitConverter.ToString(SerialPort_value.dataProgram2Receiving, 0, SerialPort_value.dataProgram2Receiving.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.dataProgram2Receiving, 0, SerialPort_value.dataProgram2Receiving.Length);
                    Array.Resize(ref SerialPort_value.dataProgram2Receiving, 0);
                }
            }
        }
        private void Program3BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp4.Length; i++)
            {
                if (sp.Data_sp4[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.statusProgram3Receiving += 1;
                }
                if (SerialPort_value.statusProgram3Receiving > 0 && sp.Data_sp4.Length > i)
                {
                    Array.Resize(ref SerialPort_value.dataProgram3Receiving, SerialPort_value.dataProgram3Receiving.Length + 1);
                    SerialPort_value.dataProgram3Receiving[SerialPort_value.dataProgram3Receiving.Length - 1] = sp.Data_sp4[i];
                }
                if (sp.Data_sp4[i].ToString("x2").Equals("fd"))
                {
                    if (RadioHexTextBox.TextLength > 0 && checkBoxForceCIV.Checked)
                    {
                        SerialPort_value.dataProgram3Receiving[2] = Convert.ToByte(RadioHexTextBox.Text, 16);
                    }
                    
                    if (RadioSerialPort.IsOpen) RadioSerialPortWrite(SerialPort_value.dataProgram3Receiving, 0, SerialPort_value.dataProgram3Receiving.Length);

                    FunctionSendCIV(SerialPort_value.dataProgram3Receiving, SerialPort_value.portProgram3Name);

                    if (RadioHexTextBox.Text != SerialPort_value.dataProgram3Receiving[2].ToString("X2") && RadioHexTextBox.Text != "" && checkBoxForceCIV.Checked == false)
                    {
                        DebugLogTextInsert(SerialPort_value.portProgram3Name + " : Error HEX? ", RadioHexTextBox.Text + " <> " + SerialPort_value.dataProgram3Receiving[2].ToString("X2"));
                    }

                    SerialPort_value.statusProgram3Receiving = 0;
                    DebugLogTextInsert(SerialPort_value.portProgram3Name, BitConverter.ToString(SerialPort_value.dataProgram3Receiving, 0, SerialPort_value.dataProgram3Receiving.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.dataProgram3Receiving, 0, SerialPort_value.dataProgram3Receiving.Length);
                    Array.Resize(ref SerialPort_value.dataProgram3Receiving, 0);
                }
            }
        }

        private void RadioConnect(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();

                RadioSerialPort.BaudRate = int.Parse(RadioBaudList.Text);
                RadioSerialPort.PortName = RadioComPortList.Text;
                RadioSerialPort.DtrEnable = true;
                RadioSerialPort.Open();
                RadioSerialPort.DiscardInBuffer();
                RadioSerialPort.DiscardOutBuffer();

                RadioBackgroundWorker.RunWorkerAsync();

                if (RadioHexTextBox.TextLength == 0) 
                {
                    Radio.CIV.Adress = 00;
                    TimerFindMyRadio.Enabled = true; 
                }

                RadioButton(true);

            }
            catch (Exception ex)
            {
                if (RadioSerialPort.IsOpen) { RadioSerialPort.Close(); }

                Debug.WriteLine("RadioConnect: " + ex.Message);
                MessageBox.Show(ex.Message, "RadioComPortError " + SerialPort_value.portRadioName);

                RadioButton(false);
            }
        }
        private void RadioDisconnect(object sender, EventArgs e)
        {

            if (RadioBackgroundWorker.IsBusy)
            {
                RadioBackgroundWorker.CancelAsync();

                while (RadioBackgroundWorker.IsBusy)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(20);
                }
            }

            if (RadioSerialPort.IsOpen) { RadioSerialPort.Close(); }

            RadioButton(false);
        }
        private void RadioButton(bool active) 
        {
            RadioConnectButton.Enabled = !active;
            RadioDisconnectButton.Enabled = active;
            RadioComPortList.Enabled = !active;
            RadioBaudList.Enabled = !active;

            TXButton.Enabled = active;

            StartDummyLoadButton.Enabled = active;
            StopDummyLoadButton.Enabled = active;

            if (active)
            {
                RadioLed.BackColor = Color.LightBlue;
            }
            else
            {
                RadioLed.BackColor = default;
                RadioLed.UseVisualStyleBackColor = true;
            }
        }

        private void Program1Connect(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();

                Program1SerialPort.BaudRate = int.Parse(RadioBaudList.Text);
                Program1SerialPort.PortName = Program1Com0comList.Text.Substring(0,5);
                Program1SerialPort.DtrEnable = true;
                Program1SerialPort.Open();
                Program1SerialPort.DiscardInBuffer();
                Program1SerialPort.DiscardOutBuffer();

                Program1BackgroundWorker.RunWorkerAsync();
                Program1Button(true);

            }
            catch (Exception ex)
            {
                if (Program1SerialPort.IsOpen) Program1SerialPort.Close();

                Program1Button(false);

                Debug.WriteLine("Program1Connect: " + ex.Message);
                MessageBox.Show(ex.Message, "Program1ComPortError " + SerialPort_value.portProgram1Name);
            }

        }
        private void Program1Disconnect(object sender, EventArgs e)
        {

            Program1BackgroundWorker.CancelAsync();

            while (Program1BackgroundWorker.IsBusy)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            Program1SerialPort.DtrEnable = false;
            Program1SerialPort.Close();
            
            Program1Button(false);
        }
        private void Program1Button(bool active)
        {
            Program1ConnectButton.Enabled = !active;
            Program1DisconnectButton.Enabled = active;
            Program1Com0comList.Enabled = !active;

            if (active)
            {
                Program1Led.BackColor = Color.LightBlue;
            }
            else
            {
                Program1Led.BackColor = default;
                Program1Led.UseVisualStyleBackColor = true;
            }
        }

        private void Program2Connect(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();

                Program2SerialPort.BaudRate = int.Parse(RadioBaudList.Text);
                Program2SerialPort.PortName = Program2Com0comList.Text.Substring(0, 5);
                Program2SerialPort.DtrEnable = true;
                Program2SerialPort.Open();
                Program2SerialPort.DiscardInBuffer();
                Program2SerialPort.DiscardOutBuffer();

                Program2BackgroundWorker.RunWorkerAsync();
                Program2Button(true);
            }
            catch (Exception ex)
            {
                if (Program2SerialPort.IsOpen) Program2SerialPort.Close();

                Program2Button(false);

                Debug.WriteLine("Program2Connect: " + ex.Message);
                MessageBox.Show(ex.Message, "Program2ComPortError " + SerialPort_value.portProgram2Name);
            }

        }
        private void Program2Disconnect(object sender, EventArgs e)
        {

            Program2BackgroundWorker.CancelAsync();

            while (Program2BackgroundWorker.IsBusy)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            Program2SerialPort.DtrEnable = false;
            Program2SerialPort.Close();

            Program2Button(false);
        }
        private void Program2Button(bool active)
        {
            Program2ConnectButton.Enabled = !active;
            Program2DisconnectButton.Enabled = active;
            Program2Com0comList.Enabled = !active;

            if (active)
            {
                Program2Led.BackColor = Color.LightBlue;
            }
            else
            {
                Program2Led.BackColor = default;
                Program2Led.UseVisualStyleBackColor = true;
            }
        }

        private void Program3Connect(object sender, EventArgs e)
        {
            
            try
            {
                SaveSettings();

                Program3SerialPort.BaudRate = int.Parse(RadioBaudList.Text);
                Program3SerialPort.PortName = Program3Com0comList.Text.Substring(0, 5);
                Program3SerialPort.DtrEnable = true;
                Program3SerialPort.Open();
                Program3SerialPort.DiscardInBuffer();
                Program3SerialPort.DiscardOutBuffer();

                Program3BackgroundWorker.RunWorkerAsync();
                Program3Button(true);
            }
            catch (Exception ex)
            {
                if (Program3SerialPort.IsOpen) Program3SerialPort.Close();

                Program3Button(false);

                Debug.WriteLine("Program3Connect: " + ex.Message);
                MessageBox.Show(ex.Message, "Program3ComPortError " + SerialPort_value.portProgram3Name);
            }

        }
        private void Program3Disconnect(object sender, EventArgs e)
        {

            Program3BackgroundWorker.CancelAsync();

            while (Program3BackgroundWorker.IsBusy)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            Program3SerialPort.DtrEnable = false;
            Program3SerialPort.Close();

            Program3Button(false);
        }
        private void Program3Button(bool active)
        {
            Program3ConnectButton.Enabled = !active;
            Program3DisconnectButton.Enabled = active;
            Program3Com0comList.Enabled = !active;

            if (active)
            {
                Program3Led.BackColor = Color.LightBlue;
            }
            else
            {
                Program3Led.BackColor = default;
                Program3Led.UseVisualStyleBackColor = true;
            }
        }


        private void TXPress(object sender, KeyEventArgs e)
        {
            this.FunctionTX(true, "Button");
        }
        private void TXRelease(object sender, KeyEventArgs e)
        {
            this.FunctionTX(false, "Button");
        }
        private void TXPress(object sender, MouseEventArgs e)
        {
            this.FunctionTX(true, "Button");
        }
        private void TXRelease(object sender, MouseEventArgs e)
        {
            this.FunctionTX(false, "Button");
        }

        private void Program1SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {

            if (e.EventType == SerialPinChange.CtsChanged)
            {
                FunctionTX(Program1SerialPort.CtsHolding, SerialPort_value.portProgram1Name);
            }
        }
        private void Program2SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            
            if (e.EventType == SerialPinChange.CtsChanged)
            {
                FunctionTX(Program2SerialPort.CtsHolding, SerialPort_value.portProgram2Name);
            }
        }
        private void Program3SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            
            if (e.EventType == SerialPinChange.CtsChanged)
            {
                FunctionTX(Program3SerialPort.CtsHolding, SerialPort_value.portProgram3Name);
            }
        }

        private void FunctionSendCIV(byte[] Data, string sender)
        {

            if (Data.Length == 8)
            {

                var PttOn = new byte[Radio.CIV.PttOn.Length];
                var PttOff = new byte[Radio.CIV.PttOff.Length];

                PttOn = Radio.CIV.PttOn;
                PttOff = Radio.CIV.PttOff;

                PttOn[2] = Data[2];
                PttOff[2] = Data[2];


                Invoke((MethodInvoker)delegate
                {

                    if (RadioSerialPort.IsOpen)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(PttOn, Data))
                        {
                            TXButton.BackColor = Color.Red;

                                RadioSerialPort.RtsEnable = true;
                                TimerTX.Enabled = true;
                                DebugLogTextInsert(sender, "PttCIV2RTS: on");
                        }
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(PttOff, Data))
                        {
                            TXButton.BackColor = default;
                            TXButton.UseVisualStyleBackColor = true;

                                RadioSerialPort.RtsEnable = false;
                                TimerTX.Enabled = false;
                                DebugLogTextInsert(sender, "PttCIV2RTS: off");
                        }
                    }

                });
            }

        }
        private void FunctionTX(Boolean button, string sender)
        {
            var PttOn = new byte[Radio.CIV.PttOn.Length];
            var PttOff = new byte[Radio.CIV.PttOff.Length];

            PttOn = Radio.CIV.PttOn;
            PttOff = Radio.CIV.PttOff;

            Invoke((MethodInvoker)delegate
            {
                if (RadioHexTextBox.TextLength > 0)
                {
                    PttOn[2]  = Convert.ToByte(RadioHexTextBox.Text, 16);
                    PttOff[2] = Convert.ToByte(RadioHexTextBox.Text, 16);
                }

                if (button)
                {
                    if (RadioSerialPort.IsOpen)
                    {
                        TXButton.BackColor = Color.Red;
                        RadioSerialPort.RtsEnable = true;
                        RadioSerialPortWrite(PttOn, 0, PttOn.Length);                        
                        DebugLogTextInsert(sender, BitConverter.ToString(PttOn, 0, PttOn.Length).Replace("-", " "));
                        TimerTX.Enabled = true;
                    }
                    else
                    {
                        DebugLogTextInsert(sender, "Error: Radio not active");
                    }

                }
                else
                {
                    if (RadioSerialPort.IsOpen)
                    {
                        TXButton.BackColor = default;
                        TXButton.UseVisualStyleBackColor = true;
                        RadioSerialPort.RtsEnable = false;
                        RadioSerialPortWrite(PttOff, 0, PttOff.Length);
                        DebugLogTextInsert(sender, BitConverter.ToString(PttOff, 0, PttOff.Length).Replace("-", " "));
                        TimerTX.Enabled = false;
                    }
                    else
                    {
                        DebugLogTextInsert(sender, "Error: Radio not active");
                    }
                }

            });

        }
        private void RadioSerialPortWrite(byte[] sendByte, int byteStart, int byteLenght)
        {
            try
            {
                RadioSerialPort.Write(sendByte, byteStart, byteLenght);
            }
            catch (Exception)
            {
                DebugLogTextInsert("Radio", "Write-error");
            }
        }

        private void SaveSettings()
        {
            if (checkBoxSaveAppData.Checked == false && SerialPort_value.saveSettings == 1)
            {
                try
                {
                    Properties.Settings.Default.BaudComboBox = RadioBaudList.Text;

                    Properties.Settings.Default.ComPortNumberRadio = RadioComPortList.Text;
                    Properties.Settings.Default.ComPortNumberProgram1 = Program1Com0comList.Text;
                    Properties.Settings.Default.ComPortNumberProgram2 = Program2Com0comList.Text;
                    Properties.Settings.Default.ComPortNumberProgram3 = Program3Com0comList.Text;

                    Properties.Settings.Default.checkBox_FeedBackProgram1 = Program1FeedBackCheckBox.Checked;
                    Properties.Settings.Default.checkBox_FeedBackProgram2 = Program2FeedBackCheckBox.Checked;
                    Properties.Settings.Default.checkBox_FeedBackProgram3 = Program3FeedBackCheckBox.Checked;

                    Properties.Settings.Default.checkBoxForceCIV = checkBoxForceCIV.Checked;

                    Properties.Settings.Default.port_nameProgram1 = SerialPort_value.portProgram1Name;
                    Properties.Settings.Default.port_nameProgram2 = SerialPort_value.portProgram2Name;
                    Properties.Settings.Default.port_nameProgram3 = SerialPort_value.portProgram3Name;

                    Properties.Settings.Default.Ptt_timeout = TimerTX.Interval / 1000;

                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SaveSettings: " + ex.Message);
                    MessageBox.Show(ex.Message, "SaveSettings");
                }

            }

        }
        private void GetSettings()
        {
            if (checkBoxSaveAppData.Checked == false)
            {
                try
                {
                    RadioBaudList.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Icom_Proxy.Properties.Settings.Default, "BaudComboBox", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
                    RadioBaudList.Text = global::Icom_Proxy.Properties.Settings.Default.BaudComboBox;

                    RadioComPortList.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumberRadio;
                    Program1Com0comList.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumberProgram1;
                    Program2Com0comList.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumberProgram2;
                    Program3Com0comList.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumberProgram3;

                    Program1FeedBackCheckBox.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_FeedBackProgram1;
                    Program2FeedBackCheckBox.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_FeedBackProgram2;
                    Program3FeedBackCheckBox.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_FeedBackProgram3;
                    checkBoxForceCIV.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBoxForceCIV;

                    if (global::Icom_Proxy.Properties.Settings.Default.port_nameProgram1 != "")
                    { Program1NameTextBox.Text = SerialPort_value.portProgram1Name = global::Icom_Proxy.Properties.Settings.Default.port_nameProgram1; }
                    if (global::Icom_Proxy.Properties.Settings.Default.port_nameProgram2 != "")
                    { Program2NameTextBox.Text = SerialPort_value.portProgram2Name = global::Icom_Proxy.Properties.Settings.Default.port_nameProgram2; }
                    if (global::Icom_Proxy.Properties.Settings.Default.port_nameProgram3 != "")
                    { Program3NameTextBox.Text = SerialPort_value.portProgram3Name = global::Icom_Proxy.Properties.Settings.Default.port_nameProgram3; }

                    //Program1NameTextBox.Text = SerialPort_value.portProgram1Name;
                    //Program2NameTextBox.Text = SerialPort_value.portProgram2Name;
                    //Program3NameTextBox.Text = SerialPort_value.portProgram3Name;

                    if (global::Icom_Proxy.Properties.Settings.Default.Ptt_timeout != 0)
                    {
                        RadioTxTimeoutTextBox.Text = global::Icom_Proxy.Properties.Settings.Default.Ptt_timeout.ToString();
                        TimerTX.Interval    = global::Icom_Proxy.Properties.Settings.Default.Ptt_timeout * 1000;
                     }
                    else
                    {
                        RadioTxTimeoutTextBox.Text = "300";
                        TimerTX.Interval = 300 * 1000;
                    }



                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetSettings: " + ex.Message);
                    MessageBox.Show(ex.Message, "GetSettings");
                }
            }
            
        }

        static void StartProcess(string _FileName, string _WorkingDirectory="")
        {
            Process proc = new Process {
                StartInfo = 
                {
                    FileName = _FileName,
                    WorkingDirectory = _WorkingDirectory,
                }
            };
            proc.Start();
        }

        private void DevButtonClick(object sender, EventArgs e)
        {
            StartProcess("mmsys.cpl");

        }
        private void SoundButtonClick(object sender, EventArgs e)
        {
            StartProcess("devmgmt.msc");
        }
        private void Com0comButtonClick(object sender, EventArgs e)
        {
            string pathx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") + @"\com0com\";
            string pathx64 = Environment.GetEnvironmentVariable("ProgramFiles") + @"\com0com\";
            string exe = @"setupg.exe";

            if (File.Exists(pathx86 + exe)) {StartProcess(pathx86 + exe, pathx86);}
            else if (File.Exists(pathx64 + exe)) {StartProcess(pathx64 + exe, pathx64);}
            else MessageBox.Show("Cannot find com0com, maybe it not installed?", "com0com");
        }

        private void ProgramName_Update(object sender, EventArgs e)
        {
            if (Program1NameTextBox.Text != "") 
            {
                SerialPort_value.portProgram1Name = Program1NameTextBox.Text;
                if (SerialPort_value.portProgram1Name.Replace(" ","").ToLower() =="varac")
                {
                    Program1FeedBackCheckBox.Checked = true;
                }
            }
            else
            {
                SerialPort_value.portProgram1Name = "Program 1";
            }

            if (Program2NameTextBox.Text != "")
            {
                SerialPort_value.portProgram2Name = Program2NameTextBox.Text;
                if (SerialPort_value.portProgram2Name.Replace(" ", "").ToLower() == "varac")
                {
                    Program2FeedBackCheckBox.Checked = true;
                }
            }
            else
            {
                SerialPort_value.portProgram2Name = "Program 2";
            }

            if (Program3NameTextBox.Text != "")
            {
                SerialPort_value.portProgram3Name = Program3NameTextBox.Text;
                if (SerialPort_value.portProgram3Name.Replace(" ", "").ToLower() == "varac")
                {
                    Program3FeedBackCheckBox.Checked = true;
                }
            }
            else
            {
                SerialPort_value.portProgram3Name = "Program 3";
            }

            ProgramInfoBoxes_Update("",null);
            SaveSettings();

        }

        private void ToneInfoUpdate() 
        {
            toneInfoBox.Text = "1 Begin with setting your soundcard to Windows default.\n"
                             + "2 Check one, two or many tones.\n"
                             + "3 Tonelength is " + ToneLengthBox .Text + " seconds after which it will automatic end,\n"
                             + "    but you can always stop it before that time.";
        }


        private void RadioTxTimeoutTextBox_Changed(object sender, EventArgs e)
        {
            int value;
            
            if (System.Text.RegularExpressions.Regex.IsMatch(RadioTxTimeoutTextBox.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                RadioTxTimeoutTextBox.Text = RadioTxTimeoutTextBox.Text.Remove(RadioTxTimeoutTextBox.Text.Length - 1);
            }
            
            if (int.TryParse(RadioTxTimeoutTextBox.Text, out value))
            {
                TimerTX.Interval = value*1000;
                SaveSettings();
            }

            
        }

        private void CheckBox_DL_Enabled(bool TrueOrFalse)
        {
            checkBox_DL_0100.Enabled = TrueOrFalse;
            checkBox_DL_0200.Enabled = TrueOrFalse;
            checkBox_DL_0300.Enabled = TrueOrFalse;
            checkBox_DL_0400.Enabled = TrueOrFalse;
            checkBox_DL_0500.Enabled = TrueOrFalse;
            checkBox_DL_0600.Enabled = TrueOrFalse;
            checkBox_DL_0700.Enabled = TrueOrFalse;
            checkBox_DL_0800.Enabled = TrueOrFalse;
            checkBox_DL_0900.Enabled = TrueOrFalse;
            checkBox_DL_1000.Enabled = TrueOrFalse;
            checkBox_DL_1100.Enabled = TrueOrFalse;
            checkBox_DL_1200.Enabled = TrueOrFalse;
            checkBox_DL_1300.Enabled = TrueOrFalse;
            checkBox_DL_1400.Enabled = TrueOrFalse;
            checkBox_DL_1500.Enabled = TrueOrFalse;
            checkBox_DL_1600.Enabled = TrueOrFalse;
            checkBox_DL_1700.Enabled = TrueOrFalse;
            checkBox_DL_1800.Enabled = TrueOrFalse;
            checkBox_DL_1900.Enabled = TrueOrFalse;
            checkBox_DL_2000.Enabled = TrueOrFalse;
            checkBox_DL_2100.Enabled = TrueOrFalse;
            checkBox_DL_2200.Enabled = TrueOrFalse;
            checkBox_DL_2300.Enabled = TrueOrFalse;
            checkBox_DL_2400.Enabled = TrueOrFalse;
            checkBox_DL_2500.Enabled = TrueOrFalse;
            checkBox_DL_2600.Enabled = TrueOrFalse;
            checkBox_DL_2700.Enabled = TrueOrFalse;
            checkBox_DL_2800.Enabled = TrueOrFalse;
            checkBox_DL_2900.Enabled = TrueOrFalse;

            TXButton.Enabled = TrueOrFalse;
            com0comButton.Enabled = TrueOrFalse;
            SoundButton.Enabled = TrueOrFalse;
            DeviceButton.Enabled = TrueOrFalse;

            ToneLengthBox.Enabled = TrueOrFalse;

            tabPageProgram.Enabled = TrueOrFalse;



        }

        MemoryStream mStrm;
        BinaryWriter writer;

        private void StartDummyLoad()
        {
            int sDuration = 10;
            
            if (int.TryParse(ToneLengthBox.Text, out sDuration))
            {
                if ((sDuration < 1) || (sDuration > 3600))
                {
                    sDuration = 60;
                }
            }

            mStrm = new MemoryStream();
            writer = new BinaryWriter(mStrm);

            PlayFile(MakeFileTot(8820*5));
            CheckBox_DL_Enabled(false);

            this.FunctionTX(true, "DummyLoad");

            TimerDummyLoad.Interval = sDuration * 1000;
            TimerDummyLoad.Enabled = true;
            


        }

        private void StopDummyLoad()
        {
            CheckBox_DL_Enabled(true);
            writer.Close();
            mStrm.Close();
            this.FunctionTX(false, "DummyLoad");
            sPlayer.Dispose();
            sPlayer.Stop();
            TimerDummyLoad.Enabled = false;
        }

        private void StartDummyLoad_Click(object sender, EventArgs e)
        {
            StartDummyLoad();
        }
        private void StopDummyLoad_Click(object sender, EventArgs e)
        {
            StopDummyLoad();
        }
        private void TextBoxDLTimeOut_TextChanged(object sender, EventArgs e)
        {
            int value;

            if (System.Text.RegularExpressions.Regex.IsMatch(ToneLengthBox.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                ToneLengthBox.Text = ToneLengthBox.Text.Remove(ToneLengthBox.Text.Length - 1);
            }

            if (int.TryParse(ToneLengthBox.Text, out value))
            {
                TimerDummyLoad.Interval = value * 1000 + 1;
                SaveSettings();
            }

            ToneInfoUpdate();
        }
        public int[] PreWaveFile(int samples)
        {
            int TextRIFF = 0x46464952; // = encoding.GetBytes("RIFF")
            int TextWAVE = 0x45564157; // = encoding.GetBytes("WAVE")
            int Textfmt = 0x20746D66; // = encoding.GetBytes("fmt ")
            int Textdata = 0x61746164; // = encoding.GetBytes("data")

            int waveSize = 4;
            int headerSize = 8;
            int formatChunkSize = 16;
            int tracks = 1;
            int bitsPerSample = 16;
            int frameSize = tracks * ((bitsPerSample + 7) / 8);
            int formatType = 1;
            int samplesPerSecond = 44100;
            int bytesPerSecond = samplesPerSecond * frameSize;
            int dataChunkSize = samples * frameSize;
            int fileSize = waveSize + headerSize + formatChunkSize + headerSize + dataChunkSize;

            int[] returnArray = new int[13];

            returnArray[00] = TextRIFF;
            returnArray[01] = fileSize;
            returnArray[02] = TextWAVE;
            returnArray[03] = Textfmt;
            returnArray[04] = formatChunkSize;
            returnArray[05] = formatType;
            returnArray[06] = tracks;
            returnArray[07] = samplesPerSecond;
            returnArray[08] = bytesPerSecond;
            returnArray[09] = frameSize;
            returnArray[10] = bitsPerSample;
            returnArray[11] = Textdata;
            returnArray[12] = dataChunkSize;

            return returnArray;

        }
        public int[] MakeTone(int samples)
        {
            int[] tone = CheckTone();

            int countTone = tone.Sum();

            double amplitude = 32768 / countTone;

            int samplesPerSecond = 44100;
            
            int[] s2 = new int[samples + 13];
            
                for (int step = 1; step < samples; step++)
                {

                int arrayStep = step + 13;

                    for (int toneNo = 1; toneNo <= 29; toneNo++)
                    {
                        s2[arrayStep] = s2[arrayStep] + (int)((tone[toneNo]) * amplitude * Math.Sin((100 * toneNo) * 2 * Math.PI / (double)samplesPerSecond * (double)step));
                    }
                    if(s2[arrayStep] == 0) 
                    {
                    //
                    }
                    s2[arrayStep] = (short)(s2[arrayStep]);
                }
            return s2;
        }
        public int[] MakeFileTot(int samples, UInt16 volume = 16383)
        {

            int[] s1 = PreWaveFile(samples);
            int[] s2 = MakeTone(samples);

            Array.Resize(ref s1, s1.Length + s2.Length);

            s2.CopyTo(s1, 13);

            return s1;
        }
        public int[] CheckTone()
        {
            int[] tone = new int[30];

            if (checkBox_DL_0100.Checked) { tone[01] = 1; }
            if (checkBox_DL_0200.Checked) { tone[02] = 1; }
            if (checkBox_DL_0300.Checked) { tone[03] = 1; }
            if (checkBox_DL_0400.Checked) { tone[04] = 1; }
            if (checkBox_DL_0500.Checked) { tone[05] = 1; }
            if (checkBox_DL_0600.Checked) { tone[06] = 1; }
            if (checkBox_DL_0700.Checked) { tone[07] = 1; }
            if (checkBox_DL_0800.Checked) { tone[08] = 1; }
            if (checkBox_DL_0900.Checked) { tone[09] = 1; }
            if (checkBox_DL_1000.Checked) { tone[10] = 1; }
            if (checkBox_DL_1100.Checked) { tone[11] = 1; }
            if (checkBox_DL_1200.Checked) { tone[12] = 1; }
            if (checkBox_DL_1300.Checked) { tone[13] = 1; }
            if (checkBox_DL_1400.Checked) { tone[14] = 1; }
            if (checkBox_DL_1500.Checked) { tone[15] = 1; }
            if (checkBox_DL_1600.Checked) { tone[16] = 1; }
            if (checkBox_DL_1700.Checked) { tone[17] = 1; }
            if (checkBox_DL_1800.Checked) { tone[18] = 1; }
            if (checkBox_DL_1900.Checked) { tone[19] = 1; }
            if (checkBox_DL_2000.Checked) { tone[20] = 1; }
            if (checkBox_DL_2100.Checked) { tone[21] = 1; }
            if (checkBox_DL_2200.Checked) { tone[22] = 1; }
            if (checkBox_DL_2300.Checked) { tone[23] = 1; }
            if (checkBox_DL_2400.Checked) { tone[24] = 1; }
            if (checkBox_DL_2500.Checked) { tone[25] = 1; }
            if (checkBox_DL_2600.Checked) { tone[26] = 1; }
            if (checkBox_DL_2700.Checked) { tone[27] = 1; }
            if (checkBox_DL_2800.Checked) { tone[28] = 1; }
            if (checkBox_DL_2900.Checked) { tone[29] = 1; }

            if (tone.Sum() == 0)
            {
                tone[10] = 1;
            }

            return tone;
        }
        public void PlayFile(int[] playFile)
        {

            for (int step = 0; step < playFile.Length; step++)
            {
                if (new[] { 0, 1, 2, 3, 4, 7, 8, 11, 12 }.Contains(step))
                {
                    writer.Write(playFile[step]);
                }
                else
                {
                    writer.Write(Convert.ToInt16(playFile[step]));
                }
            }

           
            mStrm.Seek(0, SeekOrigin.Begin);

            sPlayer = new SoundPlayer(mStrm);
            sPlayer.PlayLooping();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            ComPortUpdate("Button");
        }
        private void TrashButton_Click(object sender, EventArgs e)
        {
            DebugLogRemoveLines(0);

        }

        private void ProgramInfoBoxes_Update(object sender, EventArgs e)
        {
            if (Program1Com0comList.Text != "" && Program1Com0comList.Text.Length == 13)
            {
                Program1infoBox.Text = "Info: \r\nWith " + Program1NameTextBox.Text + " use " + Program1Com0comList.Text.Substring(8, 5).Trim();

                if (Program1FeedBackCheckBox.Checked && SerialPort_value.portProgram1Name.Replace(" ", "").ToLower() != "varac")
                {
                    Program1infoBox.Text += "\r\n\r\nUse only Feedback if " + Program1NameTextBox.Text + " needs it.";
                }
            }
            else
            {
                Program1infoBox.Text = "";
            }

            if (Program2Com0comList.Text != "" && Program2Com0comList.Text.Length == 13)
            {
                Program2infoBox.Text = "Info: \r\nWith " + Program2NameTextBox.Text + " use " + Program2Com0comList.Text.Substring(8, 5).Trim();

                if (Program2FeedBackCheckBox.Checked && SerialPort_value.portProgram2Name.Replace(" ", "").ToLower() != "varac")
                {
                    Program2infoBox.Text += "\r\n\r\nUse only Feedback if " + Program2NameTextBox.Text + " needs it.";
                }
            }
            else
            {
                Program2infoBox.Text = "";
            }

            if (Program3Com0comList.Text != "" && Program3Com0comList.Text.Length == 13)
            {
                Program3infoBox.Text = "Info: \r\nWith " + Program3NameTextBox.Text + " use " + Program3Com0comList.Text.Substring(8, 5).Trim();

                if (Program3FeedBackCheckBox.Checked && SerialPort_value.portProgram3Name.Replace(" ", "").ToLower() != "varac")
                {
                    Program3infoBox.Text += "\r\n\r\nUse only Feedback if " + Program3NameTextBox.Text + " needs it.";
                }
            }
            else
            {
                Program3infoBox.Text = "";
            }

        }

        private void OpenExplorerForConfigFile(object sender, EventArgs e)
        {
            var path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            StartProcess(Path.GetDirectoryName(path));
        }

        private void ShowToneTestButton_Click(object sender, EventArgs e)
        {
            var tabPage = tabControl1.TabPages[tabPageTone.Name];
            if (tabPage == null)
            {
                tabControl1.TabPages.Add(tabPageTone);
            }

            tabControl1.SelectedTab = tabPageTone;
        }
    }
}

