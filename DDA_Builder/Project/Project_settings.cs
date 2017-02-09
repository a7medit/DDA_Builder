using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DDA_Builder.Project
{
    public partial class Project_settings : Form
    {
        public Project_settings()
        {
            InitializeComponent();
            TBProjectName.Text = Form1.DDAProjectName;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DataSet project = CreateSettingsDataset();
            //save general settings
            DataRow FirstRow = project.Tables["Settings"].NewRow();
            FirstRow["ConnectionString"] = TBConnectionstring.Text;
            FirstRow["ProjectLocation"] = TBProjectLocation.Text;
            FirstRow["TemplateLocation"] = TBTemplateFolder.Text;
            project.Tables["Settings"].Rows.Add(FirstRow);
            Form1.ProjectSettingsds = project;

            //save Templates settings


            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                DataRow Trow = project.Tables["Templates"].NewRow();
                Trow["TemplateName"] = item.Cells[0].Value;
                Trow["TemplatLocation"] = item.Cells[1].Value;
                project.Tables["Templates"].Rows.Add(Trow);
                Form1.ProjectSettingsds = project;
            }
            project.WriteXml(Form1.DDAProjectLocation + "\\" + "settings");
            this.Close();
        }

        DataSet CreateSettingsDataset()
        {
            DataSet ds = new DataSet();
            DataTable settingsdt = new DataTable("Settings");
            settingsdt.Columns.Add("ConnectionString");
            settingsdt.Columns.Add("ProjectLocation");
            settingsdt.Columns.Add("TemplateLocation");
            DataTable Templatesdt = new DataTable("Templates");
            Templatesdt.Columns.Add("TemplateName");
            Templatesdt.Columns.Add("TemplatLocation");
            ds.Tables.Add(settingsdt);
            ds.Tables.Add(Templatesdt);
            return ds;
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Project_settings_Load(object sender, EventArgs e)
        {
            try
            {
                TBConnectionstring.Text = Form1.ProjectSettingsds.Tables["Settings"].Rows[0]["ConnectionString"].ToString();
                TBProjectLocation.Text = Form1.ProjectSettingsds.Tables["Settings"].Rows[0]["ProjectLocation"].ToString();
                TBTemplateFolder.Text = Form1.ProjectSettingsds.Tables["Settings"].Rows[0]["TemplateLocation"].ToString();

                foreach (DataRow item in Form1.ProjectSettingsds.Tables["Templates"].Rows)
                {
                    int rowindex = dataGridView1.Rows.Add();
                    dataGridView1[0, rowindex].Value = item["TemplateName"];
                    dataGridView1[1, rowindex].Value = item["TemplatLocation"];
                }


            }
            catch { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Loadtemplates(TBTemplateFolder.Text);
        }
        void Loadtemplates(string TemplatesLocation)
        {
            string TemplateFolder = TBTemplateFolder.Text;
            DirectoryInfo d = new DirectoryInfo(TemplateFolder);//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            string str = "";
            foreach (FileInfo file in Files)
            {
                bool scape = false;
                if(Form1.ProjectSettingsds.Tables["Templates"] != null )
                foreach (DataRow item in Form1.ProjectSettingsds.Tables["Templates"].Rows)
                {
                    if (file.Name.Trim() == item["TemplateName"].ToString().Trim())
                        scape = true;
                }
                if (scape)
                    continue;
                int rowindex = dataGridView1.Rows.Add();
                dataGridView1[0, rowindex].Value = file.Name;
            }
        }
    }
}
