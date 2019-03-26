using MySql.Data.MySqlClient;
using RFIDGateSystem.Forms.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RFIDGateSystem.Forms
{
    public partial class LoginForm : Form
    {
        #region Variables
        private MySqlConnection sqlConnection = new MySqlConnection();
        private string username;
        private string password;
        private string sqlPassword;
        #endregion

        public LoginForm()
        {
            InitializeComponent();

            sqlPassword = new CredentialsMgr().SqlDbPass;

#if DEBUG
            tbUserName.Text = "root";
            tbPassword.Text = "123456";
#endif
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                //Make connection parameters
                sqlConnection.ConnectionString = "server=localhost; database=login; username=root; password=";

                string query = "SELECT * from login"; //Select all columns from the LOGIN table
                var sqlCommand = new MySqlCommand(query, sqlConnection); //Sets the query to the sql command

                sqlConnection.Open();

                var reader = sqlCommand.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Read();

                    username = reader["uname"].ToString();
                    password = reader["pword"].ToString();

                    if (tbUserName.Text == username)
                    {
                        if (tbPassword.Text == password)
                        {
                            //Login successful, Show MainForm
                            new MainForm().Show();
                            this.Hide();
                        }
                        else
                        {
                            //Wrong password
                            MessageBox.Show("Wrong password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            sqlConnection.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show("You are not registered", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        sqlConnection.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Wrong username or password\nPlease try again in a few seconds", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    sqlConnection.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message);
                Debug.WriteLine(ex.Message);
            }
        }

        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Application.MessageLoop)
            {
                // WinForms app
                Application.Exit();
            }
            else
            {
                // Console app
                Environment.Exit(1);
            }
        }
    }
}
