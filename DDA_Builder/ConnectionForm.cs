using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DDA_Builder
{
    public partial class ConnectionForm : Form
    {
        public ConnectionForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Connection = textBox1.Text;
            Properties.Settings.Default.Save();
            Close();
        }

        private void ConnectionForm_Load(object sender, EventArgs e)
        {
            textBox1.Text = Properties.Settings.Default.Connection;
        }
    }
}
