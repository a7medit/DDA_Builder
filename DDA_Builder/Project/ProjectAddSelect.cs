using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DDA_Builder.Project
{
    public partial class ProjectAddSelect : Form
    {
        public string Projectname = "";
        public string Project_Location = "";
        public ProjectAddSelect()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StringCollection Projects = Properties.Settings.Default.ProjectList == null ?new StringCollection(): Properties.Settings.Default.ProjectList;
            Projects.Add(textBox1.Text +"&"+ textBox2.Text);
            Properties.Settings.Default.ProjectList = Projects;
            Properties.Settings.Default.Save();
            this.Close();
            Projectname = textBox1.Text;
            Project_Location = textBox2.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ProjectAddSelect_Load(object sender, EventArgs e)
        {
            StringCollection Projects = Properties.Settings.Default.ProjectList;
            if(Projects != null)
            foreach (var item in Projects)
            {
                int x =dataGridView1.Rows.Add();
                dataGridView1[0, x].Value = item.Split('&')[0];
                dataGridView1[1, x].Value = item.Split('&')[1];
                }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog open = new FolderBrowserDialog();
            
            if( open.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = open.SelectedPath;

            } 
        }

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            Projectname = dataGridView1[0, e.RowIndex].Value.ToString();
            Project_Location = dataGridView1[1, e.RowIndex].Value.ToString();
            this.Close();
        }
    }
}
