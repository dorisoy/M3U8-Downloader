using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace M3U8_Downloader
{
    public partial class RaceInfoForm : Form
    {
        private SQLiteHelper mySQLiteHelper;
        public RaceInfoForm()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            mySQLiteHelper = new SQLiteHelper();
            SQLiteHelper.SetConnectionString(System.AppDomain.CurrentDomain.BaseDirectory + "Data.db3", "");
        }

        private void RaceInfoForm_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            InitForm();
        }

        private void InitForm()
        {
            try
            {
                string sql = "select * from raceinfo";
                DataTable dt = null;
                dt = mySQLiteHelper.ExecuteQuery(sql);
                if (dt != null)
                {
                    this.dataGridView1.AutoGenerateColumns = true;
                    this.dataGridView1.DataSource = dt;
                }

                string sql2 = "select * from RaceFailed";
                DataTable dt2 = null;
                dt2 = mySQLiteHelper.ExecuteQuery(sql2);
                if (dt != null)
                {
                    this.dataGridView2.AutoGenerateColumns = true;
                    this.dataGridView2.DataSource = dt2;
                }
            }
            catch (Exception ex)
            { 
            
            }
        }
    }
}
