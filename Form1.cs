using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Xml;
using System.Media;
using System.Management;

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

            comportUpdate("Start");

            SerialPort2.PinChanged += new SerialPinChangedEventHandler(SerialPort2_PinChanged);
            SerialPort3.PinChanged += new SerialPinChangedEventHandler(SerialPort3_PinChanged);
            SerialPort4.PinChanged += new SerialPinChangedEventHandler(SerialPort4_PinChanged);

            SerialPort1.ReadTimeout = SerialPort1.WriteTimeout = 1000;
            SerialPort2.ReadTimeout = SerialPort2.WriteTimeout = 1000;
            SerialPort3.ReadTimeout = SerialPort3.WriteTimeout = 1000;
            SerialPort4.ReadTimeout = SerialPort4.WriteTimeout = 1000;

            BackgroundWorker1.WorkerSupportsCancellation = true;
            BackgroundWorker1.WorkerReportsProgress = true;
            BackgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            BackgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            
            BackgroundWorker2.WorkerSupportsCancellation = true;
            BackgroundWorker2.WorkerReportsProgress = true;
            BackgroundWorker2.DoWork += BackgroundWorker2_DoWork;
            BackgroundWorker2.ProgressChanged += BackgroundWorker2_ProgressChanged;

            BackgroundWorker3.WorkerSupportsCancellation = true;
            BackgroundWorker3.WorkerReportsProgress = true;
            BackgroundWorker3.DoWork += BackgroundWorker3_DoWork;
            BackgroundWorker3.ProgressChanged += BackgroundWorker3_ProgressChanged;

            BackgroundWorker4.WorkerSupportsCancellation = true;
            BackgroundWorker4.WorkerReportsProgress = true;
            BackgroundWorker4.DoWork += BackgroundWorker4_DoWork;
            BackgroundWorker4.ProgressChanged += BackgroundWorker4_ProgressChanged;

            TimerPTT_CIV.Tick += TimerPTT_CIV_of;

            TimerPTT_RTS.Tick += TimerPTT_RTS_of;

            TimerFindMyRadio.Tick += TimerFindMyRadioLoop;
            TimerFindMyRadio.Interval = 200;

            TimerDummyLoad.Tick += TimerDummyLoadLoop;
            TimerDummyLoad.Interval = 10000;

            this.Text = Version.NameAndNumber;

            SerialPort_value.saveSettings = 0;
            GetSettings();
            SerialPort_value.saveSettings = 1;

        }

        public static SerialPort SerialPort1 = new SerialPort();
        public static SerialPort SerialPort2 = new SerialPort();
        public static SerialPort SerialPort3 = new SerialPort();
        public static SerialPort SerialPort4 = new SerialPort();

        public static System.Windows.Forms.Timer Timer1 = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer TimerPTT_CIV = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer TimerPTT_RTS = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer TimerFindMyRadio = new System.Windows.Forms.Timer();
        public static System.Windows.Forms.Timer TimerDummyLoad = new System.Windows.Forms.Timer();

        public static BackgroundWorker BackgroundWorker1 = new BackgroundWorker();
        public static BackgroundWorker BackgroundWorker2 = new BackgroundWorker();
        public static BackgroundWorker BackgroundWorker3 = new BackgroundWorker();
        public static BackgroundWorker BackgroundWorker4 = new BackgroundWorker();

        private System.Media.SoundPlayer sPlayer;
 

        static class SerialPort_value
        {
            public static string port_name1 = "Radio";
            public static string port_name2 = "Program 1";
            public static string port_name3 = "Program 2";
            public static string port_name4 = "Program 3";

            public static int saveSettings = 0;
            
            public static int status_receiving1;
            public static int status_receiving2;
            public static int status_receiving3;
            public static int status_receiving4;

            public static byte[] data_receiving1 = new byte[0];
            public static byte[] data_receiving2 = new byte[0];
            public static byte[] data_receiving3 = new byte[0];
            public static byte[] data_receiving4 = new byte[0];

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
            textBox1.Text = textBox1.Text.Insert(0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + program.PadRight(10) + ": " + text + "\r\n");
            DebugLogRemoveLines(50);
        }
        private void DebugLogRemoveLines(int rows)
        {
  
            if (textBox1.Lines.Length > rows)
            {
                textBox1.Lines = textBox1.Lines.Take(rows).ToArray();
            }
        }
        private void comportUpdate(string program)
        {
            ComPortNumber1.Items.Clear();
            ComPortNumber2.Items.Clear();
            ComPortNumber3.Items.Clear();
            ComPortNumber4.Items.Clear();

            ComPortNumber1.Sorted = true;
            ComPortNumber2.Sorted = true;
            ComPortNumber3.Sorted = true;
            ComPortNumber4.Sorted = true;

            ComPortNumber1.Items.AddRange(SerialPort.GetPortNames());
            ComPortNumber2.Items.AddRange(SerialPort.GetPortNames());
            ComPortNumber3.Items.AddRange(SerialPort.GetPortNames());
            ComPortNumber4.Items.AddRange(SerialPort.GetPortNames());


            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\cimv2",
                "SELECT * FROM Win32_PnPEntity where Caption is not null");

            foreach (ManagementObject CNCA in searcher.Get())
            {
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
                                DebugLogTextInsert("com0com", CNCA["Caption"].ToString().Substring(startA, endA - startA) + " - " + CNCB["Caption"].ToString().Substring(startB, endB - startB));
                            }
                        }
                        catch (Exception)
                        {

                            throw;
                        }


                    }
                }

            }

            foreach (ManagementObject queryObj in searcher.Get())
            {
                if ((queryObj["Caption"].ToString().IndexOf("(COM") > 0) && (queryObj["Caption"].ToString().IndexOf("com0com") < 0))
                {
                    DebugLogTextInsert("Serial interface", queryObj["Caption"].ToString());
                }
            }


            DebugLogTextInsert(program, "Check Comport");
        }
        public void DebugMessage(object sender, EventArgs e)
        {
            string header = "sender: " + sender.ToString();
            string body;

            if (e is null)
            {
                body = "EventArgs: null";
            }
            else
            {
                body = "EventArgs: " + e.ToString();
            }

            DebugLogTextInsert(header, body);
            Debug.WriteLine(header + " - " + body);
            MessageBox.Show(header, body);
        }

        public void SaveSettings(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void TimerPTT_CIV_of(object sender, EventArgs e)
        {
            // Send a PTT of via CIV
            this.FunctionPttCIV(false, "Timer");
            TimerPTT_CIV.Enabled = false;
        }
        private void TimerPTT_RTS_of(object sender, EventArgs e)
        {
            // Send a PTT of via RTS
            this.FunctionPttRts(false, "Timer");
            TimerPTT_RTS.Enabled = false;
        }
        private void TimerFindMyRadioLoop(object sender, EventArgs e)
        {
            var Initiate = new byte[Radio.CIV.Initiate.Length];

            Initiate = Radio.CIV.Initiate;

            Initiate[2] = Radio.CIV.Adress;


            if (textBox_hex.TextLength == 0 && Radio.CIV.Adress != 0xff && SerialPort1.IsOpen)
            {
                DebugLogTextInsert("FindMyRadio", BitConverter.ToString(Initiate, 0, Initiate.Length).Replace("-", " "));
                SerialPort1.Write(Initiate, 0, Initiate.Length);
            }
            else if (textBox_hex.TextLength > 0)
            {
                DebugLogTextInsert("CI-V Adress", textBox_hex.Text);
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
            this.stopDummyLoad();
        }


        private static void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!BackgroundWorker1.CancellationPending)
            {
                try
                {
                    if (SerialPort1.IsOpen)
                    {
                      var c = SerialPort1.Read(buffer, 0, buffer.Length);
                      BackgroundWorker1.ReportProgress(0, new SerialPort_Data() { Data_sp1 = buffer.Take(c).ToArray() });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("BackgroundWorker1_DoWork: " + ex.Message);
                }
            }

        }
        private static void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!BackgroundWorker2.CancellationPending)
            {
                try
                {
                    if (SerialPort2.IsOpen)
                    {
                       var c = SerialPort2.Read(buffer, 0, buffer.Length);
                       BackgroundWorker2.ReportProgress(0, new SerialPort_Data() { Data_sp2 = buffer.Take(c).ToArray() });

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("BackgroundWorker2_DoWork: " + ex.Message);
                }
            }

        }
        private static void BackgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!BackgroundWorker3.CancellationPending)
            {
                try
                {
                    if (SerialPort3.IsOpen)
                    {
                        var c = SerialPort3.Read(buffer, 0, buffer.Length);
                        BackgroundWorker3.ReportProgress(0, new SerialPort_Data() { Data_sp3 = buffer.Take(c).ToArray() });

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("BackgroundWorker3_DoWork: " + ex.Message);
                }
            }

        }
        private static void BackgroundWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            var buffer = new byte[4096];

            while (!BackgroundWorker4.CancellationPending)
            {
                try
                {
                    if (SerialPort4.IsOpen)
                    {
                        var c = SerialPort4.Read(buffer, 0, buffer.Length);
                        BackgroundWorker4.ReportProgress(0, new SerialPort_Data() { Data_sp4 = buffer.Take(c).ToArray() });

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("BackgroundWorker4_DoWork: " + ex.Message);
                }
            }

        }



        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp1.Length; i++)
            {
                if (sp.Data_sp1[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.status_receiving1 += 1;
                }
                if (SerialPort_value.status_receiving1 > 0 && sp.Data_sp1.Length > i)
                {
                    Array.Resize(ref SerialPort_value.data_receiving1, SerialPort_value.data_receiving1.Length + 1);
                    SerialPort_value.data_receiving1[SerialPort_value.data_receiving1.Length - 1] = sp.Data_sp1[i];
                }
                if (sp.Data_sp1[i].ToString("x2").Equals("fd"))
                {
                        if (SerialPort2.IsOpen && checkBox_FeedBack2.Checked) SerialPort2.Write(SerialPort_value.data_receiving1, 0, SerialPort_value.data_receiving1.Length);
                        if (SerialPort3.IsOpen && checkBox_FeedBack3.Checked) SerialPort3.Write(SerialPort_value.data_receiving1, 0, SerialPort_value.data_receiving1.Length);
                        if (SerialPort4.IsOpen && checkBox_FeedBack4.Checked) SerialPort4.Write(SerialPort_value.data_receiving1, 0, SerialPort_value.data_receiving1.Length);
                
                    if (textBox_hex.Text == "" 
                            && SerialPort_value.data_receiving1[3].ToString("X2") != "00" 
                            && SerialPort_value.data_receiving1[3].ToString("X2") != "E0")
                    {
                        textBox_hex.Text = SerialPort_value.data_receiving1[3].ToString("X2");
                    }

                    if (textBox_hex.Text != SerialPort_value.data_receiving1[3].ToString("X2")
                            && textBox_hex.Text != SerialPort_value.data_receiving1[4].ToString("X2")
                            && SerialPort_value.data_receiving1[3].ToString("X2") != "E0" 
                            && textBox_hex.TextLength > 0)
                    {
                        DebugLogTextInsert(SerialPort_value.port_name1 + " : Error HEX? ", textBox_hex.Text + " <> " + SerialPort_value.data_receiving1[3].ToString("X2"));
                    }

                    SerialPort_value.status_receiving1 = 0;
                    DebugLogTextInsert(SerialPort_value.port_name1, BitConverter.ToString(SerialPort_value.data_receiving1, 0, SerialPort_value.data_receiving1.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.data_receiving1, 0, SerialPort_value.data_receiving1.Length);
                    Array.Resize(ref SerialPort_value.data_receiving1, 0);
                }
                
            }
        }
        private void BackgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp2.Length; i++)
            {
                if (sp.Data_sp2[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.status_receiving2 += 1;
                }
                if (SerialPort_value.status_receiving2 > 0 && sp.Data_sp2.Length > i)
                {
                    Array.Resize(ref SerialPort_value.data_receiving2, SerialPort_value.data_receiving2.Length + 1);
                    SerialPort_value.data_receiving2[SerialPort_value.data_receiving2.Length - 1] = sp.Data_sp2[i];
                }
                if (sp.Data_sp2[i].ToString("x2").Equals("fd"))
                {
                    if (textBox_hex.TextLength > 0 & checkBoxForceCIV.Checked)
                    {
                        SerialPort_value.data_receiving2[2] = Convert.ToByte(textBox_hex.Text, 16);
                    }

                    if (SerialPort1.IsOpen) SerialPort1.Write(SerialPort_value.data_receiving2, 0, SerialPort_value.data_receiving2.Length);

                    PttCIV2RTS(SerialPort_value.data_receiving2, SerialPort_value.port_name2);

                    if (textBox_hex.Text != SerialPort_value.data_receiving2[2].ToString("X2") && textBox_hex.Text != "" && checkBoxForceCIV.Checked == false)
                    {
                        DebugLogTextInsert(SerialPort_value.port_name2 + " : Error HEX? ", textBox_hex.Text + " <> " + SerialPort_value.data_receiving2[2].ToString("X2"));
                    }

                    SerialPort_value.status_receiving2 = 0;
                    DebugLogTextInsert(SerialPort_value.port_name2, BitConverter.ToString(SerialPort_value.data_receiving2, 0, SerialPort_value.data_receiving2.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.data_receiving2, 0, SerialPort_value.data_receiving2.Length);
                    Array.Resize(ref SerialPort_value.data_receiving2, 0);
                }
            }
        }
        private void BackgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp3.Length; i++)
            {
                if (sp.Data_sp3[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.status_receiving3 += 1;
                }
                if (SerialPort_value.status_receiving3 > 0 && sp.Data_sp3.Length > i)
                {
                    Array.Resize(ref SerialPort_value.data_receiving3, SerialPort_value.data_receiving3.Length + 1);
                    SerialPort_value.data_receiving3[SerialPort_value.data_receiving3.Length - 1] = sp.Data_sp3[i];
                }
                if (sp.Data_sp3[i].ToString("x2").Equals("fd"))
                {
                    if (textBox_hex.TextLength > 0 && checkBoxForceCIV.Checked)
                    {
                        SerialPort_value.data_receiving3[2] = Convert.ToByte(textBox_hex.Text, 16);
                    }
                    
                    if (SerialPort1.IsOpen) SerialPort1.Write(SerialPort_value.data_receiving3, 0, SerialPort_value.data_receiving3.Length);

                    PttCIV2RTS(SerialPort_value.data_receiving3, SerialPort_value.port_name3);

                    if (textBox_hex.Text != SerialPort_value.data_receiving3[2].ToString("X2") && textBox_hex.Text != "" && checkBoxForceCIV.Checked == false)
                    {
                        DebugLogTextInsert(SerialPort_value.port_name3 + " : Error HEX? ", textBox_hex.Text + " <> " + SerialPort_value.data_receiving3[2].ToString("X2"));
                    }
                    SerialPort_value.status_receiving3 = 0;
                    DebugLogTextInsert(SerialPort_value.port_name3, BitConverter.ToString(SerialPort_value.data_receiving3, 0, SerialPort_value.data_receiving3.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.data_receiving3, 0, SerialPort_value.data_receiving3.Length);
                    Array.Resize(ref SerialPort_value.data_receiving3, 0);
                }
            }
        }
        private void BackgroundWorker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var sp = e.UserState as SerialPort_Data;

            for (int i = 0; i < sp.Data_sp4.Length; i++)
            {
                if (sp.Data_sp4[i].ToString("x2").Equals("fe"))
                {
                    SerialPort_value.status_receiving4 += 1;
                }
                if (SerialPort_value.status_receiving4 > 0 && sp.Data_sp4.Length > i)
                {
                    Array.Resize(ref SerialPort_value.data_receiving4, SerialPort_value.data_receiving4.Length + 1);
                    SerialPort_value.data_receiving4[SerialPort_value.data_receiving4.Length - 1] = sp.Data_sp4[i];
                }
                if (sp.Data_sp4[i].ToString("x2").Equals("fd"))
                {
                    if (textBox_hex.TextLength > 0 && checkBoxForceCIV.Checked)
                    {
                        SerialPort_value.data_receiving4[2] = Convert.ToByte(textBox_hex.Text, 16);
                    }
                    
                    if (SerialPort1.IsOpen) SerialPort1.Write(SerialPort_value.data_receiving4, 0, SerialPort_value.data_receiving4.Length);

                    PttCIV2RTS(SerialPort_value.data_receiving4, SerialPort_value.port_name4);

                    if (textBox_hex.Text != SerialPort_value.data_receiving4[2].ToString("X2") && textBox_hex.Text != "" && checkBoxForceCIV.Checked == false)
                    {
                        DebugLogTextInsert(SerialPort_value.port_name4 + " : Error HEX? ", textBox_hex.Text + " <> " + SerialPort_value.data_receiving4[2].ToString("X2"));
                    }

                    SerialPort_value.status_receiving4 = 0;
                    DebugLogTextInsert(SerialPort_value.port_name4, BitConverter.ToString(SerialPort_value.data_receiving4, 0, SerialPort_value.data_receiving4.Length).Replace("-", " "));
                    Array.Clear(SerialPort_value.data_receiving4, 0, SerialPort_value.data_receiving4.Length);
                    Array.Resize(ref SerialPort_value.data_receiving4, 0);
                }
            }
        }

        private void ComPortConnect1_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();

                SerialPort1.BaudRate = int.Parse(BaudComboBox.Text);
                SerialPort1.PortName = ComPortNumber1.Text;
                SerialPort1.DtrEnable = true;
                SerialPort1.Open();
                SerialPort1.DiscardInBuffer();
                SerialPort1.DiscardOutBuffer();

                BackgroundWorker1.RunWorkerAsync();

                if (textBox_hex.TextLength == 0) 
                {
                    Radio.CIV.Adress = 00;
                    TimerFindMyRadio.Enabled = true; 
                }

                ComPortConnect1.Enabled = false;
                ComPortDisconnect1.Enabled = true;
                PttButtonRTS.Enabled = true;
                PttButtonCIV.Enabled = true;

                ComPortNumber1.Enabled = false;
                BaudComboBox.Enabled = false;

                buttonStartDummyLoad.Enabled = true;
                buttonStopDummyLoad.Enabled = true;


            }
            catch (Exception ex)
            {
                if (SerialPort1.IsOpen) SerialPort1.Close();

                ComPortConnect1.Enabled = true;
                ComPortDisconnect1.Enabled = false;

                ComPortNumber1.Enabled = true;
                BaudComboBox.Enabled = true;

                buttonStartDummyLoad.Enabled = false;
                buttonStopDummyLoad.Enabled = false;

                Debug.WriteLine("ComPortConnect1_Click: " + ex.Message);
                MessageBox.Show(ex.Message, "ComPortError " + SerialPort_value.port_name1);
            }
        }
        private void ComPortConnect2_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();

                SerialPort2.BaudRate = int.Parse(BaudComboBox.Text);
                SerialPort2.PortName = ComPortNumber2.Text;
                SerialPort2.DtrEnable = true;
                SerialPort2.Open();
                SerialPort2.DiscardInBuffer();
                SerialPort2.DiscardOutBuffer();


                BackgroundWorker2.RunWorkerAsync();

                ComPortConnect2.Enabled = false;
                ComPortDisconnect2.Enabled = true;

                ComPortNumber2.Enabled = false;
           
            }
            catch (Exception ex)
            {
                if (SerialPort2.IsOpen) SerialPort2.Close();

                ComPortConnect2.Enabled = true;
                ComPortDisconnect2.Enabled = false;

                ComPortNumber2.Enabled = true;

                Debug.WriteLine("ComPortConnect2_Click: " + ex.Message);
                MessageBox.Show(ex.Message, "ComPortError " + SerialPort_value.port_name2);
            }

        }
        private void ComPortConnect3_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();

                SerialPort3.BaudRate = int.Parse(BaudComboBox.Text);
                SerialPort3.PortName = ComPortNumber3.Text;
                SerialPort3.DtrEnable = true;
                SerialPort3.Open();
                SerialPort3.DiscardInBuffer();
                SerialPort3.DiscardOutBuffer();

                BackgroundWorker3.RunWorkerAsync();

                ComPortConnect3.Enabled = false;
                ComPortDisconnect3.Enabled = true;

                ComPortNumber3.Enabled = false;

            }
            catch (Exception ex)
            {
                if (SerialPort3.IsOpen) SerialPort3.Close();

                ComPortConnect3.Enabled = true;
                ComPortDisconnect3.Enabled = false;

                ComPortNumber3.Enabled = true;

                Debug.WriteLine("ComPortConnect3_Click: " + ex.Message);
                MessageBox.Show(ex.Message, "ComPortError " + SerialPort_value.port_name3);
            }

        }
        private void ComPortConnect4_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettings();

                SerialPort4.BaudRate = int.Parse(BaudComboBox.Text);
                SerialPort4.PortName = ComPortNumber4.Text;
                SerialPort4.DtrEnable = true;
                SerialPort4.Open();
                SerialPort4.DiscardInBuffer();
                SerialPort4.DiscardOutBuffer();

                BackgroundWorker4.RunWorkerAsync();

                ComPortConnect4.Enabled = false;
                ComPortDisconnect4.Enabled = true;

                ComPortNumber4.Enabled = false;

            }
            catch (Exception ex)
            {
                if (SerialPort4.IsOpen) SerialPort4.Close();

                ComPortConnect4.Enabled = true;
                ComPortDisconnect4.Enabled = false;

                ComPortNumber4.Enabled = true;


                Debug.WriteLine("ComPortConnect4_Click: " + ex.Message);
                MessageBox.Show(ex.Message, "ComPortError " + SerialPort_value.port_name4);
            }

        }

        private void ComPortDisconnect1_Click(object sender, EventArgs e)
        {

            if (BackgroundWorker1.IsBusy)
            {
                BackgroundWorker1.CancelAsync();
                
                while (BackgroundWorker1.IsBusy)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(20);
                }

            }

            SerialPort1.DtrEnable = false;
            SerialPort1.Close();
            ComPortConnect1.Enabled = true;
            ComPortDisconnect1.Enabled = false;
            PttButtonRTS.Enabled = false;
            PttButtonCIV.Enabled = false;

            buttonStartDummyLoad.Enabled = false;
            buttonStopDummyLoad.Enabled = false;

            ComPortNumber1.Enabled = true;
            BaudComboBox.Enabled = true;

        }
        private void ComPortDisconnect2_Click(object sender, EventArgs e)
        {

            BackgroundWorker2.CancelAsync();

            while (BackgroundWorker2.IsBusy)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            SerialPort2.DtrEnable = false;
            SerialPort2.Close();
            ComPortConnect2.Enabled = true;
            ComPortDisconnect2.Enabled = false;

            ComPortNumber2.Enabled = true;

        }
        private void ComPortDisconnect3_Click(object sender, EventArgs e)
        {

            BackgroundWorker3.CancelAsync();

            while (BackgroundWorker3.IsBusy)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            SerialPort3.DtrEnable = false;
            SerialPort3.Close();
            ComPortConnect3.Enabled = true;
            ComPortDisconnect3.Enabled = false;

            ComPortNumber3.Enabled = true;

        }
        private void ComPortDisconnect4_Click(object sender, EventArgs e)
        {

            BackgroundWorker4.CancelAsync();

            while (BackgroundWorker4.IsBusy)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            SerialPort4.DtrEnable = false;
            SerialPort4.Close();
            ComPortConnect4.Enabled = true;
            ComPortDisconnect4.Enabled = false;

            ComPortNumber4.Enabled = true;

        }

        private void PttPress(object sender, KeyEventArgs e)
        {
            this.FunctionPttRts(true, "Button");
        }
        private void PttRelease(object sender, KeyEventArgs e)
        {
            this.FunctionPttRts(false, "Button");
        }
        private void PttPress(object sender, MouseEventArgs e)
        {
            this.FunctionPttRts(true, "Button");
        }
        private void PttRelease(object sender, MouseEventArgs e)
        {
            this.FunctionPttRts(false, "Button");
        }

        private void PttPressCIV(object sender, KeyEventArgs e)
        {
            this.FunctionPttCIV(true, "Button");
        }
        private void PttReleaseCIV(object sender, KeyEventArgs e)
        {
            this.FunctionPttCIV(false, "Button");
        }
        private void PttPressCIV(object sender, MouseEventArgs e)
        {
            this.FunctionPttCIV(true, "Button");
        }
        private void PttReleaseCIV(object sender, MouseEventArgs e)
        {
            this.FunctionPttCIV(false, "Button");
        }

        private void SerialPort2_PinChanged(object sender, SerialPinChangedEventArgs e)
        {

            if (e.EventType == SerialPinChange.CtsChanged)
            {
                FunctionPttRts(SerialPort2.CtsHolding, SerialPort_value.port_name2);
            }
        }
        private void SerialPort3_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            
            if (e.EventType == SerialPinChange.CtsChanged)
            {
                FunctionPttRts(SerialPort3.CtsHolding, SerialPort_value.port_name3);
            }
        }
        private void SerialPort4_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            
            if (e.EventType == SerialPinChange.CtsChanged)
            {
                FunctionPttRts(SerialPort4.CtsHolding, SerialPort_value.port_name4);
            }
        }

        private void PttCIV2RTS(byte[] Data, string sender)
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

                    if (SerialPort1.IsOpen)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(PttOn, Data))
                        {
                            PttButtonCIV.BackColor = Color.Red;
                            if (checkBox_PTT_RTS.Checked)
                            {
                                PttButtonRTS.BackColor = Color.Red;
                                SerialPort1.RtsEnable = true;
                                TimerPTT_RTS.Enabled = true;
                                DebugLogTextInsert(sender, "PttCIV2RTS: on");
                            }
                        }
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(PttOff, Data))
                        {
                            PttButtonCIV.BackColor = default(Color);
                            PttButtonCIV.UseVisualStyleBackColor = true;
                            if (checkBox_PTT_RTS.Checked)
                            {
                                PttButtonRTS.BackColor = default(Color);
                                PttButtonRTS.UseVisualStyleBackColor = true;
                                SerialPort1.RtsEnable = false;
                                TimerPTT_RTS.Enabled = false;
                                DebugLogTextInsert(sender, "PttCIV2RTS: off");
                            }
                        }
                    }

                });
            }

        }
        private void PttRTS2CIV(Boolean button, string sender) 
        {
            if (checkBox_PTT_CIV.Checked && sender != "Button")
            {
                FunctionPttCIV(button, sender + " - PttRTS2CIV");
            }
        }
        private void FunctionPttRts(Boolean button, string sender)
        {

            Invoke((MethodInvoker)delegate
            {
                if (SerialPort1.IsOpen)
                {
                    if (button)
                    {
                        PttButtonRTS.BackColor = Color.Red;
                        SerialPort1.RtsEnable = true;
                        TimerPTT_RTS.Enabled = true;
                        DebugLogTextInsert(sender, "PTT/RTS: on");
                    }
                    else
                    {
                        PttButtonRTS.BackColor = default(Color);
                        PttButtonRTS.UseVisualStyleBackColor = true;
                        SerialPort1.RtsEnable = false;
                        TimerPTT_RTS.Enabled = false;
                        DebugLogTextInsert(sender, "PTT/RTS: off");
                    }

                    PttRTS2CIV(button, sender);
                }

            });


        }
        private void FunctionPttCIV(Boolean button, string sender)
        {
            var PttOn = new byte[Radio.CIV.PttOn.Length];
            var PttOff = new byte[Radio.CIV.PttOff.Length];

            PttOn = Radio.CIV.PttOn;
            PttOff = Radio.CIV.PttOff;

            Invoke((MethodInvoker)delegate
            {
                if (textBox_hex.TextLength > 0)
                {
                    PttOn[2]  = Convert.ToByte(textBox_hex.Text, 16);
                    PttOff[2] = Convert.ToByte(textBox_hex.Text, 16);
                }

                if (button)
                {
                    if (SerialPort1.IsOpen)
                    {
                        PttButtonCIV.BackColor = Color.Red;
                        SerialPort1.Write(PttOn, 0, PttOn.Length);
                        DebugLogTextInsert(sender, BitConverter.ToString(PttOn, 0, PttOn.Length).Replace("-", " "));
                        TimerPTT_CIV.Enabled = true;
                    }
                    else
                    {
                        DebugLogTextInsert(sender, "Error: Radio not active");
                    }

                }
                else
                {
                    if (SerialPort1.IsOpen)
                    {
                        PttButtonCIV.BackColor = default(Color);
                        PttButtonCIV.UseVisualStyleBackColor = true;
                        SerialPort1.Write(PttOff, 0, PttOff.Length);
                        DebugLogTextInsert(sender, BitConverter.ToString(PttOff, 0, PttOff.Length).Replace("-", " "));
                        TimerPTT_CIV.Enabled = false;
                    }
                    else
                    {
                        DebugLogTextInsert(sender, "Error: Radio not active");
                    }
                }

            });

        }

        private void SaveSettings()
        {
            if (checkBoxSaveAppData.Checked == false && SerialPort_value.saveSettings == 1)
            {
                try
                {
                    Properties.Settings.Default.BaudComboBox = BaudComboBox.Text;

                    Properties.Settings.Default.ComPortNumber1 = ComPortNumber1.Text;
                    Properties.Settings.Default.ComPortNumber2 = ComPortNumber2.Text;
                    Properties.Settings.Default.ComPortNumber3 = ComPortNumber3.Text;
                    Properties.Settings.Default.ComPortNumber4 = ComPortNumber4.Text;

                    Properties.Settings.Default.checkBox_PTT_RTS = checkBox_PTT_RTS.Checked;
                    Properties.Settings.Default.checkBox_PTT_CIV = checkBox_PTT_CIV.Checked;
                    Properties.Settings.Default.checkBox_FeedBack2 = checkBox_FeedBack2.Checked;
                    Properties.Settings.Default.checkBox_FeedBack3 = checkBox_FeedBack3.Checked;
                    Properties.Settings.Default.checkBox_FeedBack4 = checkBox_FeedBack4.Checked;

                    Properties.Settings.Default.checkBoxForceCIV = checkBoxForceCIV.Checked;

                    Properties.Settings.Default.port_name2 = SerialPort_value.port_name2;
                    Properties.Settings.Default.port_name3 = SerialPort_value.port_name3;
                    Properties.Settings.Default.port_name4 = SerialPort_value.port_name4;

                    Properties.Settings.Default.Ptt_timeout = TimerPTT_CIV.Interval / 1000;

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
                    BaudComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Icom_Proxy.Properties.Settings.Default, "BaudComboBox", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
                    BaudComboBox.Text = global::Icom_Proxy.Properties.Settings.Default.BaudComboBox;

                    ComPortNumber1.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumber1;
                    ComPortNumber2.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumber2;
                    ComPortNumber3.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumber3;
                    ComPortNumber4.Text = global::Icom_Proxy.Properties.Settings.Default.ComPortNumber4;

                    checkBox_PTT_RTS.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_PTT_RTS;
                    checkBox_PTT_CIV.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_PTT_CIV;
                    checkBox_FeedBack2.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_FeedBack2;
                    checkBox_FeedBack3.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_FeedBack3;
                    checkBox_FeedBack4.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBox_FeedBack4;
                    checkBoxForceCIV.Checked = global::Icom_Proxy.Properties.Settings.Default.checkBoxForceCIV;

                    if (global::Icom_Proxy.Properties.Settings.Default.port_name2 != "") SerialPort_value.port_name2 = global::Icom_Proxy.Properties.Settings.Default.port_name2;
                    if (global::Icom_Proxy.Properties.Settings.Default.port_name3 != "") SerialPort_value.port_name3 = global::Icom_Proxy.Properties.Settings.Default.port_name3;
                    if (global::Icom_Proxy.Properties.Settings.Default.port_name4 != "") SerialPort_value.port_name4 = global::Icom_Proxy.Properties.Settings.Default.port_name4;

                    label_port_name2.Text = SerialPort_value.port_name2;
                    label_port_name3.Text = SerialPort_value.port_name3;
                    label_port_name4.Text = SerialPort_value.port_name4;

                    textBox_port_name2.Text = SerialPort_value.port_name2;
                    textBox_port_name3.Text = SerialPort_value.port_name3;
                    textBox_port_name4.Text = SerialPort_value.port_name4;

                    if (global::Icom_Proxy.Properties.Settings.Default.Ptt_timeout != 0)
                    {
                        textBox_Ptt_timeout.Text = global::Icom_Proxy.Properties.Settings.Default.Ptt_timeout.ToString();
                        TimerPTT_CIV.Interval    = global::Icom_Proxy.Properties.Settings.Default.Ptt_timeout * 1000;
                        TimerPTT_RTS.Interval    = global::Icom_Proxy.Properties.Settings.Default.Ptt_timeout * 1000;
                    }
                    else
                    {
                        textBox_Ptt_timeout.Text = "300";
                        TimerPTT_CIV.Interval = 300 * 1000;
                        TimerPTT_RTS.Interval = 300 * 1000;
                    }



                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetSettings: " + ex.Message);
                    MessageBox.Show(ex.Message, "GetSettings");
                }
            }
            
        }

        private void DevButtonClick(object sender, EventArgs e)
        {
            Process.Start("mmsys.cpl");

        }
        private void SoundButtonClick(object sender, EventArgs e)
        {
            Process.Start("devmgmt.msc");
        }
        private void Com0comButtonClick(object sender, EventArgs e)
        {
            string pathx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") + @"\com0com\setupg.exe";
            string pathx64 = Environment.GetEnvironmentVariable("ProgramFiles") + @"\com0com\setupg.exe";

            if (File.Exists(pathx86)) { Process.Start(pathx86); }
            else if (File.Exists(pathx64)) { Process.Start(pathx64); }
            else MessageBox.Show("Cannot find com0com, maybe it not installed?", "com0com");

        }

        private void TextBox_port_name2_TextChanged(object sender, EventArgs e)
        {
            if (textBox_port_name2.Text != "") 
            {
                SerialPort_value.port_name2 = textBox_port_name2.Text;
            }
            else
            {
                SerialPort_value.port_name2 = "Program 1";
            }

            label_port_name2.Text = SerialPort_value.port_name2;
            SaveSettings();
        }
        private void TextBox_port_name3_TextChanged(object sender, EventArgs e)
        {
            if (textBox_port_name3.Text != "")
            {
                SerialPort_value.port_name3 = textBox_port_name3.Text;
            }
            else
            {
                SerialPort_value.port_name3 = "Program 2";
            }

            label_port_name3.Text = SerialPort_value.port_name3;
            SaveSettings();
        }
        private void TextBox_port_name4_TextChanged(object sender, EventArgs e)
        {
            if (textBox_port_name4.Text != "")
            {
                SerialPort_value.port_name4 = textBox_port_name4.Text;
            }
            else
            {
                SerialPort_value.port_name4 = "Program 3";
            }

            label_port_name4.Text = SerialPort_value.port_name4;
            SaveSettings();
        }

        private void TextBox_Ptt_timeout_TextChanged(object sender, EventArgs e)
        {
            int value;
            
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox_Ptt_timeout.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox_Ptt_timeout.Text = textBox_Ptt_timeout.Text.Remove(textBox_Ptt_timeout.Text.Length - 1);
            }
            
            if (int.TryParse(textBox_Ptt_timeout.Text, out value))
            {
                TimerPTT_CIV.Interval = value*1000;
                TimerPTT_RTS.Interval = value*1000;
                SaveSettings();
            }

            
        }

        private void checkBox_DL_Enabled(bool TrueOrFalse)
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

            PttButtonRTS.Enabled = TrueOrFalse;
            PttButtonCIV.Enabled = TrueOrFalse;
            com0comButton.Enabled = TrueOrFalse;
            SoundButton.Enabled = TrueOrFalse;
            DeviceButton.Enabled = TrueOrFalse;

            textBoxDLTimeOut.Enabled = TrueOrFalse;

            tabPageComPort.Enabled = TrueOrFalse;
            tabPageSettings.Enabled = TrueOrFalse;



        }

        MemoryStream mStrm;
        BinaryWriter writer;

        private void startDummyLoad()
        {
            int sDuration = 10;
            
            if (int.TryParse(textBoxDLTimeOut.Text, out sDuration))
            {
                if ((sDuration < 1) || (sDuration > 3600))
                {
                    sDuration = 60;
                }
            }

            mStrm = new MemoryStream();
            writer = new BinaryWriter(mStrm);

            PlayFile(MakeFileTot(8820*5));
            checkBox_DL_Enabled(false);

            this.FunctionPttRts(true, "DummyLoad");
            this.FunctionPttCIV(true, "DummyLoad");

            TimerDummyLoad.Interval = sDuration * 1000;
            TimerDummyLoad.Enabled = true;
            


        }

        private void stopDummyLoad()
        {
            checkBox_DL_Enabled(true);
            writer.Close();
            mStrm.Close();
            this.FunctionPttRts(false, "DummyLoad");
            this.FunctionPttCIV(false, "DummyLoad");
            sPlayer.Dispose();
            sPlayer.Stop();
            TimerDummyLoad.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            startDummyLoad();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stopDummyLoad();
        }

        private void textBoxDLTimeOut_TextChanged(object sender, EventArgs e)
        {
            int value;

            if (System.Text.RegularExpressions.Regex.IsMatch(textBoxDLTimeOut.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBoxDLTimeOut.Text = textBoxDLTimeOut.Text.Remove(textBoxDLTimeOut.Text.Length - 1);
            }

            if (int.TryParse(textBoxDLTimeOut.Text, out value))
            {
                TimerDummyLoad.Interval = value * 1000 + 1;
                SaveSettings();
            }
        }

        public int[] preWaveFile(int samples)
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

            int[] s1 = preWaveFile(samples);
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
            comportUpdate("Button");
        }

        private void TrashButton_Click(object sender, EventArgs e)
        {
            DebugLogRemoveLines(0);

        }
    }
}

