using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DDA_Builder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        List<string> Tables = new List<string>();
        string textTemplate = "";
        private void Form1_Load(object sender, EventArgs e)
        {
            //get all templates
            string TemplateFolder = AppDomain.CurrentDomain.BaseDirectory.ToString()+ @"Text template";
            DirectoryInfo d = new DirectoryInfo(TemplateFolder);//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            string str = "";
            foreach (FileInfo file in Files)
            {
                comboBox2.Items.Add(file.Name);
            }



            ConnectionForm f = new ConnectionForm();
            f.ShowDialog();

            SqlConnection con = new SqlConnection(Properties.Settings.Default.Connection);
            SqlCommand com = new SqlCommand("SELECT table_name FROM information_schema.tables",con);
            con.Open();
            SqlDataReader r = com.ExecuteReader(CommandBehavior.CloseConnection);
            while (r.Read())
            {
                Tables.Add(r[0].ToString());

                comboBox1.Items.Add(r[0].ToString());
            }
            con.Close();

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            SqlConnection con = new SqlConnection(Properties.Settings.Default.Connection);
            SqlCommand com = new SqlCommand("SELECT c.name 'Column Name',t.Name 'Data type',c.max_length 'Max Length',c.is_nullable, ISNULL(i.is_primary_key, 0) 'Primary Key' FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id LEFT OUTER JOIN sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id LEFT OUTER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id WHERE c.object_id = OBJECT_ID(@tableName)", con);
            com.Parameters.Add("tableName", SqlDbType.Text).Value = comboBox1.Text;
            con.Open();
            SqlDataReader r = com.ExecuteReader(CommandBehavior.CloseConnection);
            while (r.Read())
            {
                int row = dataGridView1.Rows.Add();
                dataGridView1[0, row].Value = r[0].ToString();
                dataGridView1[1, row].Value = r[1].ToString();
                dataGridView1[2, row].Value = true;
            }
            con.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string Class = "    public class "+comboBox1.Text+"ViewModel {"+ System.Environment.NewLine;
            foreach (DataGridViewRow datarowdata in dataGridView1.Rows)
            {
                if(datarowdata.Cells[0].Value != null)
                Class += "public " + GetDataType(datarowdata.Cells[1].Value.ToString()) + " " + datarowdata.Cells[0].Value.ToString()+"   { get; set; }" + System.Environment.NewLine;
                
            }

            Class += "}";
            ShowTextForm f = new ShowTextForm(Class);
            f.ShowDialog();
        }

        string GetDataType(string SqlType)
        {
            if (SqlType.Contains("varchar"))
                return "string";
            else if (SqlType.Contains("int"))
                return "int";
            else if (SqlType.Contains("uniqueidentifier"))
                return "Guid";
            else if (SqlType.Contains("bit"))
                return "bool";
            
            else
            return SqlType;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Dictionary<string,string> tabs = new Dictionary<string, string>();
            bool hastabs = false;
            foreach (DataGridViewRow datarowdata in dataGridView1.Rows)
            {
                if (datarowdata.Cells[3].Value != null && !string.IsNullOrEmpty(datarowdata.Cells[3].Value.ToString()))
                {
                    hastabs = true;
                }
            }

            string Class = "";
            if (hastabs)
            {
                foreach (DataGridViewRow datarowdata in dataGridView1.Rows)
                {
                    if (string.IsNullOrEmpty(datarowdata.Cells[3].Value?.ToString()))
                    {
                        datarowdata.Cells[3].Value = "Main";
                    }
                }

                bool first = true;
                Class += "<ul class='nav nav-tabs'>" + System.Environment.NewLine; ;
                foreach (DataGridViewRow datarowdata in dataGridView1.Rows)
                {
                    var itemenabled = datarowdata.Cells[2]?.Value;
                    if (itemenabled != null && (datarowdata.Cells[3].Value != null && !string.IsNullOrEmpty(datarowdata.Cells[3].Value.ToString()) && (bool)itemenabled))
                    {
                        if (!tabs.ContainsKey(datarowdata.Cells[3].Value.ToString()))
                        {
                            if (first)
                            {
                                Class += "<li class='active'><a data-toggle='tab' href='#"+ datarowdata.Cells[3].Value.ToString() + "'>"+ datarowdata.Cells[3].Value.ToString() + "</a></li>" + System.Environment.NewLine; ;
                                tabs.Add(datarowdata.Cells[3].Value.ToString(), "<div id='"+ datarowdata.Cells[3].Value.ToString() + "' class='tab-pane fade in active'>");
                                first = false;
                            }
                            else
                            {
                                Class += "<li><a data-toggle='tab' href='#" + datarowdata.Cells[3].Value.ToString() + "'>" + datarowdata.Cells[3].Value.ToString() + "</a></li>" + System.Environment.NewLine; ;
                                tabs.Add(datarowdata.Cells[3].Value.ToString(), "<div id='"+ datarowdata.Cells[3].Value.ToString() + "' class='tab - pane fade'>");
                            }
        
                        }
                    }
                }
                Class += "</ul>" + System.Environment.NewLine ;
                
            }

             Class += @"<form role='form' name='"+comboBox1.Text+ "Form' ng-app='AngularMVCApp' ng-controller='" + comboBox1.Text + "Controller'>" + System.Environment.NewLine;
            
            if(hastabs)
                Class += "<div class='tab-content'>" + System.Environment.NewLine;

            foreach (DataGridViewRow datarowdata in dataGridView1.Rows)
            {
                if (datarowdata.Cells[0].Value != null && (bool) datarowdata.Cells[2].Value)
                {
                    string item = "";
                    item += " <div class='form - group'> " + System.Environment.NewLine;
                    item += " <label>" + datarowdata.Cells[0].Value.ToString() + ":</label> " +
                             System.Environment.NewLine;

                    item += " <input class='form-control' id='" + datarowdata.Cells[0].Value.ToString() +
                             "' ng-model='current" + comboBox1.Text + "." + datarowdata.Cells[0].Value.ToString() +
                             "'>" + System.Environment.NewLine;

                    item += " </div>" + System.Environment.NewLine;
                    if (hastabs)
                    {
                        tabs[datarowdata.Cells[3].Value.ToString()] += item;
                    }
                    else
                    {
                        Class += item;
                    }
                }
            }
            foreach (var tabcode in tabs)
            {
                Class += tabcode.Value+ System.Environment.NewLine + " </div> "+ System.Environment.NewLine;
            }
            Class += "    <button type='button' class='btn btn-primary' ng-click='add"+ comboBox1.Text + "()'>Add </ button > ";
            if (hastabs)
                Class += "</div>";
            Class += "</form>";
            ShowTextForm f = new ShowTextForm(Class);
            f.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
  



        
    }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(comboBox2.Text))
            {
 
               textTemplate =  System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory.ToString() + @"Text template"+@"\\"+ comboBox2.Text);


            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DataTable data = GetDataTableFromDGV(dataGridView1);
            Parser p = new Parser(comboBox1.Text, data,textTemplate);
            ShowTextForm f = new ShowTextForm(p.Parse());
            f.ShowDialog();
        }
        public DataTable GetContentAsDataTable(bool IgnoreHideColumns = false)
        {
            try
            {
                if (dataGridView1.ColumnCount == 0) return null;
                DataTable dtSource = new DataTable();
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (IgnoreHideColumns & !col.Visible) continue;
                    if (col.Name == string.Empty) continue;
                    dtSource.Columns.Add(col.Name, col.ValueType);
                    dtSource.Columns[col.Name].Caption = col.HeaderText;
                }
                if (dtSource.Columns.Count == 0) return null;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    DataRow drNewRow = dtSource.NewRow();
                    foreach (DataColumn col in dtSource.Columns)
                    {
                        drNewRow[col.ColumnName] = row.Cells[col.ColumnName].Value;
                    }
                    dtSource.Rows.Add(drNewRow);
                }
                return dtSource;
            }
            catch { return null; }
        }

        private DataTable GetDataTableFromDGV(DataGridView dgv)
        {
            var dt = new DataTable();
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                if (column.Visible)
                {
                    // You could potentially name the column based on the DGV column name (beware of dupes)
                    // or assign a type based on the data type of the data bound to this DGV column.
                    dt.Columns.Add(column.Name);
                }
            }

            object[] cellValues = new object[dgv.Columns.Count];
            foreach (DataGridViewRow row in dgv.Rows)
            {
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    cellValues[i] = row.Cells[i].Value;
                }
                dt.Rows.Add(cellValues);
            }

            return dt;
        }


    }
    }

