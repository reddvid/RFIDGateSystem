using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using MySql.Data.MySqlClient;
using RFIDGateSystem.Forms.Classes;

namespace RFIDGateSystem.Forms
{
    public partial class MainForm : Form
    {
        #region Variables
        DateTime dateTime;
        Array ports;
        Array ports2;
        private string readBuffer;
        const string rfidKey = "1Fog66";
        string directory;
        string vId, vPlate, vColor, vMake, vModel, vOwner;

        int[] baudRates = new int[] { 9600, 19200, 38400, 57600, 115200 };
        private string serial;
        bool isConnected;
        delegate void SetTextCallback(string text);
        #endregion

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            isConnected = false;

            LoadComPorts();

            LoadBaudRates();

            DisableButtons();

            CheckLogsDirectory();

            SetTimeAndDate();
        }

        private void SetTimeAndDate()
        {
            timer1.Interval = 1000;
            timer1.Tick += Timer1_Tick;

            timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {

            lblTime.Text = DateTime.Now.ToLongTimeString();
            lblDate.Text = DateTime.Now.ToLongDateString();
        }

        private void CheckLogsDirectory()
        {
            var drives = DriveInfo.GetDrives();
            Debug.WriteLine(drives[0]);

            var folderPath = drives[0] + @"RFIDLogs\";

            if (!Directory.Exists(folderPath))
            {
                //Create directory
                Directory.CreateDirectory(folderPath);
            }

            directory = folderPath;
        }

        private void DisableButtons()
        {
            btnGenerate.Enabled = btnClear.Enabled = false;
        }

        private void ChangeButtonState(bool isConnected)
        {
            Debug.WriteLine(isConnected);

            if (isConnected)
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        }

        private void Connect()
        {
            if (cbPort1.SelectedItem != null || cbPort2.SelectedItem != null)
            {
                if (!readerPort.IsOpen || !controllerPort.IsOpen)
                {
                    try
                    {
                        readerPort.PortName = cbPort1.Text;
                        readerPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                        readerPort.Parity = Parity.None;
                        readerPort.StopBits = StopBits.One;
                        readerPort.DataBits = 8;
                        readerPort.Open();

                        controllerPort.PortName = cbPort2.Text;
                        controllerPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                        controllerPort.Parity = Parity.None;
                        controllerPort.StopBits = StopBits.One;
                        controllerPort.DataBits = 8;
                        controllerPort.Open();

                        btnConnect.Text = "Disconnect";

                        isConnected = true;

                        cbPort1.Enabled = cbPort2.Enabled = cbBaudRate.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Error opening COM ports", "", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show(this, "Select a valid COM port!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);

                cbPort1.Enabled = cbPort2.Enabled = cbBaudRate.Enabled = true;
            }
        }

        private void Disconnect()
        {
            try
            {
                readerPort.Close();
                controllerPort.Close();

                btnConnect.Text = "Connect";

                isConnected = false;

                cbPort1.Enabled = cbPort2.Enabled = cbBaudRate.Enabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void LoadBaudRates()
        {
            foreach (var rate in baudRates)
            {
                cbBaudRate.Items.Add(rate.ToString());
            }

            // Select default
            cbBaudRate.SelectedIndex = 0;
        }

        private void LoadComPorts()
        {
            ports = SerialPort.GetPortNames();
            ports2 = SerialPort.GetPortNames();

            if (ports.Length != 0 || ports2.Length != 0)
            {
                foreach (var p in ports)
                {
                    cbPort1.Items.Add(p);
                }

                foreach (var p in ports2)
                {
                    cbPort2.Items.Add(p);
                }

                // Select defaults
                try
                {
                    cbPort1.SelectedIndex = 0;
                    cbPort2.SelectedIndex = 1;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                // SHow error in finding COM ports
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            ChangeButtonState(isConnected);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show(this, "Are you sure you want to close the application?", "Quit", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                new LoginForm().Show();

                timer1.Stop();
            }
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            ReceivedTextHandler(readerPort.ReadExisting());

            if (readerPort.IsOpen)
            {
                try
                {
                    readBuffer = readerPort.ReadExisting();
                    Invoke(new EventHandler(DoUpdate));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void DoUpdate(object sender, EventArgs e)
        {

        }

        private void ReceivedTextHandler(string received)
        {
            dateTime = DateTime.Now;

            if (rtbData.InvokeRequired)
            {
                var stcb = new SetTextCallback(ReceivedTextHandler);
                this.Invoke(stcb, new object[] { (received) });
            }
            else
            {
                serial = received.Substring(0, 4);

                try
                {
                    var sqlConnection = new MySqlConnection();
                    sqlConnection.ConnectionString = "server=localhost; database=vehicles; username=root; password=";
                    var query = "SELECT * from car_desc where sid=@serial";
                    var command = new MySqlCommand(query, sqlConnection);
                    command.Parameters.Add(new SqlParameter("@serial", serial));
                    sqlConnection.Open();

                    var reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();
                        vId = reader["sid"] as string;
                        vPlate= reader["plate_num"] as string;
                        vColor = reader["color"] as string;
                        vMake = reader["make"] as string;
                        vModel = reader["model"] as string;
                        vOwner = reader["owned_by"] as string;
                    }
                    else
                    {
                        return;
                    }

                    if (serial == vId)
                    {
                        rtbNum.Text = vPlate;
                        rtbNum.SelectionStart = rtbData.TextLength;
                        rtbNum.ScrollToCaret();
                        rtbNum.Focus();

                        rtbDetails.Text += String.Format("----> {0} --- {1} {2} {3} : {4}", vPlate, vColor, vMake, vModel, vOwner);
                        rtbDetails.SelectionStart = rtbData.TextLength;
                        rtbDetails.ScrollToCaret();
                        rtbDetails.Focus();

                        rtbData.Text += String.Format("----> {0} --- {1} \n", received, dateTime.ToString("MMMdyyyy"));
                        rtbData.SelectionStart = rtbData.TextLength;
                        rtbData.ScrollToCaret();
                        rtbData.Focus();

                        Beep();

                        if (string.IsNullOrEmpty(readBuffer))
                        {
                            controllerPort.Open();
                            controllerPort.Write("0");
                            controllerPort.Close();
                        }
                        else if (controllerPort.IsOpen)
                        {
                            controllerPort.Write("1"); //Send Logic 1 signal voltage
                            controllerPort.Close();
                            serial = "";
                            received = "";
                        }
                        else
                        {
                            controllerPort.Open();
                            controllerPort.Write("1");
                            controllerPort.Close();
                            serial = "";
                            received = "";
                        }
                        //I do not know what these textlenght and scroll focus meant.. needs to check
                    }
                    else
                    {
                        //Vehicle not found
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
              
            }
        }

        private void Beep()
        {
            //throw new NotImplementedException();
        }

        private void cbPort1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!readerPort.IsOpen)
            {
                readerPort.PortName = cbPort1.Text;
            }
            else
            {
                MessageBox.Show("Make sure the port is not in use.");
            }
        }

        private void cbBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!readerPort.IsOpen)
            {
                readerPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
            }
            else
            {
                MessageBox.Show("Make sure the port is not in use.");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(rtbData.Text))
            {
                var result = MessageBox.Show(this, "Are you sure you want to clear data?", "Clearing data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    rtbData.Text = rtbNum.Text = string.Empty;
                }
            }
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            Debug.WriteLine(directory);
            var file = directory + "Logs" + DateTime.Now.ToString("MMMdyyyy") + ".txt";

            if (File.Exists(file))
            {
                var objWriter = new StreamWriter(file, true);
                var current = this.Text + dateTime.ToString("MMMdyyyy") + Environment.NewLine;
                objWriter.WriteLine(current);
                objWriter?.Dispose();
                objWriter?.Close();
            }
            else
            {
                var objWriter = new StreamWriter(file);
                objWriter.WriteLine(rtbData.Text);
                objWriter?.Dispose();
                objWriter?.Close();
            }
        }

        private void TsmReg_Click(object sender, EventArgs e)
        {
            new RegistryForm().Show();
        }

        private void RtbData_TextChanged(object sender, EventArgs e)
        {
            btnClear.Enabled = btnGenerate.Enabled = !String.IsNullOrEmpty(rtbData.Text);
            var file = directory + "AutoLog.txt";

            if (!String.IsNullOrEmpty(rtbData.Text) && File.Exists(file))
            {
                var streamWriter = new StreamWriter(file, true);
                var current = rtbData.Lines.GetLength(rtbData.Lines.Length - 2);
                var currentPlateNumber = rtbNum.Text;
                var currentDetails = String.Format("{0} {1} {2} registered to {3}", vColor, vMake, vModel, vOwner);
                streamWriter.WriteLine(String.Format("{0} {1} {2} {3}", vId, currentPlateNumber, currentDetails, dateTime.ToString("MMMdyyyy HHmm")));
                streamWriter?.Dispose();
                streamWriter?.Close();
            }
            else
            {
                var streamWriter = new StreamWriter(file, true);
                streamWriter.WriteLine(rtbData.Text);
                streamWriter?.Dispose();
                streamWriter?.Close();
            }
        }
    }
}
