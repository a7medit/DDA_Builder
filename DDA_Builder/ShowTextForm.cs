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
    public partial class ShowTextForm : Form
    {
        public ShowTextForm(string mainText)
        {
            InitializeComponent();
            richTextBox1.Text = mainText;
        }
        

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
