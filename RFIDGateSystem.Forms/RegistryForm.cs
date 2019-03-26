using MySql.Data.MySqlClient;
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
    public partial class RegistryForm : Form
    {
        //TO-DO:
        //Textbox validations before submission


        public RegistryForm()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var sqlConnection = new MySqlConnection();
            sqlConnection.ConnectionString = "server=localhost; database=vehicles; username=root; password=";
            sqlConnection.Open();

            try
            {
                var dataSet = new DataSet();
                var sqlDataAdapter = new MySqlDataAdapter("INSERT INTO car_desc(sid, plate_num, color, make, model, owned_by) VALUES ('"
                    + tbSid.Text + ","
                    + tbPlateNum.Text + ","
                    + tbColor.Text + ","
                    + tbMake.Text + ","
                    + tbModel.Text + ","
                    + tbOwner.Text + "')", sqlConnection);

                if (String.IsNullOrEmpty(tbSid.Text) &&
                    String.IsNullOrEmpty(tbPlateNum.Text) &&
                    String.IsNullOrEmpty(tbColor.Text) &&
                    String.IsNullOrEmpty(tbMake.Text) &&
                    String.IsNullOrEmpty(tbModel.Text) &&
                    String.IsNullOrEmpty(tbOwner.Text))
                {
                    sqlDataAdapter.Fill(dataSet);
                    MessageBox.Show("Saved to database.");
                }
                else
                {
                    MessageBox.Show("Something went wrong.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
