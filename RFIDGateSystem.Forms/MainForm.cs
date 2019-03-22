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
        string data;
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
                if (!readerPort.IsOpen)
                {
                    try
                    {
                        readerPort.PortName = cbPort1.Text;
                        readerPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                        readerPort.Parity = System.IO.Ports.Parity.None;
                        readerPort.StopBits = System.IO.Ports.StopBits.One;
                        readerPort.DataBits = 8;
                        readerPort.Open();


                        btnConnect.Text = "Disconnect";

                        isConnected = true;

                        cbPort1.Enabled = cbPort2.Enabled = cbBaudRate.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show(this, "Select a valid COM port!", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

                cbPort1.Enabled = cbPort2.Enabled = cbBaudRate.Enabled = true;
            }
        }

        private void Disconnect()
        {
            try
            {
                readerPort.Close();

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
            ports = System.IO.Ports.SerialPort.GetPortNames();
            ports2 = System.IO.Ports.SerialPort.GetPortNames();

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

                var sqlConnection = new MySqlConnection();
                sqlConnection.ConnectionString = "server=localhost; database=vehicles; username=root; password=";
                var query = "SELECT * from car_desc where sid=@serial";
                var command = new MySqlCommand(query, sqlConnection);
                command.Parameters.Add(new SqlParameter("@serial", serial));
                sqlConnection.Open();

                var reader = command.ExecuteReader();
                var vehicle = new Vehicle();

                if (reader.HasRows)
                {
                    reader.Read();
                    vehicle.Id = reader["sid"] as string;
                    vehicle.PlateNum = reader["plate_num"] as string;
                    vehicle.Color = reader["color"] as string;
                    vehicle.Make = reader["make"] as string;
                    vehicle.Model = reader["model"] as string;
                    vehicle.Owner = reader["owned_by"] as string;
                }

                if (serial == vehicle.Id)
                {
                    rtbData.Text = received;
                    rtbDetails.Text += String.Format("----> {0} --- {1} {2} {3} : {4}", vehicle.PlateNum, vehicle.Color, vehicle.Make, vehicle.Model, vehicle.Owner);
                    rtbDetails.SelectionStart = rtbData.TextLength;
                    rtbDetails.ScrollToCaret();
                    rtbDetails.Focus();

                    rtbData.Text += String.Format("----> {0} --- {1} \n", received, dateTime);
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
        }

        private void Beep()
        {
            throw new NotImplementedException();
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

                if (result == DialogResult.OK)
                {
                    rtbData.Text = string.Empty;
                }
            }
        }

        private async void btnGenerate_Click(object sender, EventArgs e)
        {
            SaveFileDialog s = new SaveFileDialog();
            StreamWriter sw;
            s.Filter = ".txt";
            s.CheckPathExists = true;
            s.Title = "Save Report File";
            s.ShowDialog(this);

            try
            {
                sw = System.IO.File.AppendText(s.FileName);
                sw.Write(rtbData.Text);
                await sw.FlushAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // Show register form
        }
    }
}
