using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using RazorEngine;
using RazorEngine.Templating;
using RazorEngine.Compilation.ReferenceResolver;
using RazorEngine.Configuration;

namespace DDA_Builder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            itwmslocations.Add("Angular ViewModel", @"CRMP.Models\@@Model.Name@@ViewModel.cs");
            itwmslocations.Add("Administrator functions", "");
            itwmslocations.Add("MVC controller", @"CRMP.Web\Controllers\@@Model.Name@@Controller.cs");
            itwmslocations.Add("MVC view", @"CRMP.Web\Views\@@Model.Name@@\Index.cshtml");
            itwmslocations.Add("List template", @"CRMP.Web\Views\@@Model.Name@@\List.cshtml");
            itwmslocations.Add("Add template", @"CRMP.Web\Views\@@Model.Name@@\AddForm.cshtml");
            itwmslocations.Add("Angular controller", @"CRMP.Web\App\\@@Model.Name@@\@@Model.Name@@Ctrl.js");

            foreach (var item in itwmslocations)
            {
                int i = checkedListBox1.Items.Add(item.Key);
                //checkedListBox1.SetItemCheckState(i, CheckState.Checked);
            }


        }

        Dictionary<string, string> itwmslocations = new Dictionary<string, string>();

        List<string> Tables = new List<string>();
        string textTemplate = "";
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = Properties.Settings.Default.ApplicationPath;
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
                RTable.Items.Add(r[0].ToString());
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
                dataGridView1[4, row].Value = !Convert.ToBoolean(r[3].ToString());
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
            CreateAddView("");
        }


        string CreateAddView(string SavePath)
        {
            string Modelname = ModelName(comboBox1.Text);
            Dictionary<string, string> tabs = new Dictionary<string, string>();
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
                                Class += "<li class='active'><a data-toggle='tab' href data-target='#" + datarowdata.Cells[3].Value.ToString() + "'>" + datarowdata.Cells[3].Value.ToString() + "</a></li>" + System.Environment.NewLine; ;
                                tabs.Add(datarowdata.Cells[3].Value.ToString(), "<div id='" + datarowdata.Cells[3].Value.ToString() + "' class='tab-pane fade in active'>");
                                first = false;
                            }
                            else
                            {
                                Class += "<li><a data-toggle='tab' href data-target='#" + datarowdata.Cells[3].Value.ToString() + "'>" + datarowdata.Cells[3].Value.ToString() + "</a></li>" + System.Environment.NewLine; ;
                                tabs.Add(datarowdata.Cells[3].Value.ToString(), "<div id='" + datarowdata.Cells[3].Value.ToString() + "' class='tab-pane fade'>");
                            }

                        }
                    }
                }
                Class += "</ul>" + System.Environment.NewLine;

            }
            Class += @"@{
    Layout = null;
}";
            Class += @"
    <h3>
        " + Modelname + @" Edit
      </h3> 
<div ng-controller=' " + Modelname + @"Ctrl'>";
            Class += @"    <div class='row'>
        <div class='panel panel-default'>
            <div class='panel-body'>";
            Class += @"<form role='form' name='" + Modelname + "Form'  data-ng-init='init()'>" + System.Environment.NewLine;

            //add validation errors
            Class += @"<div class='alert alert-error ng-cloak' ng-cloak ng-if='errors.formErrorsSummary.length > 0'><h4 > The following errors were found:</h4 ><ul ng-repeat = 'error in errors.formErrorsSummary' ><li class='ng-cloak'>{{error.Message}}</li></ul></div>" + System.Environment.NewLine;

            if (hastabs)
                Class += "<div class='tab-content'>" + System.Environment.NewLine;

            foreach (DataGridViewRow datarowdata in dataGridView1.Rows)
            {
                if (datarowdata.Cells[2].Value != null && bool.Parse(datarowdata.Cells[2].Value.ToString()) == false)
                    continue;
                if (datarowdata.Cells[0].Value != null && (bool)datarowdata.Cells[2].Value)
                {
                    string item = "";
                    if (datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True")
                    {
                        item += " <div class='form-group col-sm-6' "+ (datarowdata.Cells[1].Value.ToString() == "datetime"? " ng-controller='DatepickerDemoCtrl as dpick' ":"") + @" ng-class=""{ 'has-error': " + Modelname + "Form." + datarowdata.Cells[0].Value.ToString() + ".$invalid }\"> " + System.Environment.NewLine;
                    }
                    else
                        item += @" <div class=""form-group col-sm-6"" " + (datarowdata.Cells[1].Value.ToString() == "datetime" ? " ng-controller='DatepickerDemoCtrl as dpick' " : "") + "> " + System.Environment.NewLine;
                    item += " <label>" + datarowdata.Cells[0].Value.ToString() + ":</label> " +
                             System.Environment.NewLine;

                    if(datarowdata.Cells[1].Value.ToString() == "datetime" )
                    {
                        item += @"<p class='input-group'>
                                              <input   name='" + datarowdata.Cells[0].Value.ToString() + "' id='" + datarowdata.Cells[0].Value.ToString() + "' ng-model='current" + Modelname + "." + datarowdata.Cells[0].Value.ToString() +
                             @"' " + ((datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True") ? "required" : "") + "     " + ((datarowdata.Cells[5].Value != null && datarowdata.Cells[5].Value.ToString().Trim() != "") ? "maxlength=" + datarowdata.Cells[5].Value.ToString().Trim() + "  " : "") + @"    class='form-control' type='text' uib-datepicker-popup='{{dpick.format}}'  is-open='dpick.opened' min-date='dpick.minDate' max-date=""'2050-12-22'"" uib-datepicker-options=""dpick.dateOptions""
                                                   date-disabled=""dpick.disabled(date, mode)"" close-text=""Close"" />
                                            <span class=""input-group-btn"">
                                                <button class=""btn btn-default"" type=""button"" ng-click=""dpick.open($event)"">
                                                    <em class=""fa fa-calendar""></em>
                                                </button>
                                            </span>
                                        </p>"+ System.Environment.NewLine;
                    }
                    else if (datarowdata.Cells[6].Value != null && datarowdata.Cells[7].Value != null)
                    {
                        item += " <select class='form-control' name='" + datarowdata.Cells[0].Value.ToString() + "' id='" + datarowdata.Cells[0].Value.ToString() + "' ng-model='current" + Modelname + "." + datarowdata.Cells[0].Value.ToString() +
         "' " + ((datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True") ? "required" : "") + "     " + ((datarowdata.Cells[5].Value != null && datarowdata.Cells[5].Value.ToString().Trim() != "") ? "maxlength=" + datarowdata.Cells[5].Value.ToString().Trim() + "  " : "") + @"  ng-options=""i." + ModelName(datarowdata.Cells[8].Value.ToString().Trim()) + @" as (i." + ModelName(datarowdata.Cells[7].Value.ToString().Trim()) + @") for i in " + ModelName(datarowdata.Cells[6].Value.ToString().Trim()) + @"List""  ><option></option> </select>" + System.Environment.NewLine;
                    }
                        else
                    item += " <input class='form-control' name='"+ datarowdata.Cells[0].Value.ToString() + "' id='" + datarowdata.Cells[0].Value.ToString() +"' ng-model='current" + Modelname + "." + datarowdata.Cells[0].Value.ToString() +
                             "' " + ((datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True") ? "required" : "") + "     " + ((datarowdata.Cells[5].Value != null && datarowdata.Cells[5].Value.ToString().Trim() != "") ? "maxlength=" + datarowdata.Cells[5].Value.ToString().Trim() + "  " : "") + "    >" + System.Environment.NewLine;

                    //validation tags
                    if ((datarowdata.Cells[5].Value != null && datarowdata.Cells[4].Value.ToString().Trim() != "") || (datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True"))
                    {
                        item += " <div ng-messages='" + Modelname + "Form." + datarowdata.Cells[0].Value.ToString() + ".$touched && " + Modelname + "Form." + datarowdata.Cells[0].Value.ToString() + ".$error' style='color: maroon' role='alert'>" + System.Environment.NewLine;
                    }
                    if (datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True")
                    {
                        item += " <div ng-message='required'>" + datarowdata.Cells[0].Value.ToString() + " is required</div>" + System.Environment.NewLine;
                    }
                    if (datarowdata.Cells[5].Value != null && datarowdata.Cells[4].Value.ToString().Trim() != "")
                    {
                        item += " <div ng-message='maxlength'>" + datarowdata.Cells[0].Value.ToString() + " is too long</div>";
                    }
                    if ((datarowdata.Cells[5].Value != null && datarowdata.Cells[4].Value.ToString().Trim() != "") || (datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True"))
                    {
                        item += " </div>" + System.Environment.NewLine;
                    }

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
                Class += tabcode.Value + System.Environment.NewLine + " </div> " + System.Environment.NewLine;
            }
            if (hastabs)
                Class += "</div>";
            Class += "</form>";
            Class += "</div>";
            Class += "    <button type='button' class='btn btn-primary' ng-click='save" + Modelname + "()' ng-disabled='CarForm.$invalid'>Add " + Modelname + "</button>";
            Class += @"</div></div></div>";
            if (SavePath == "")
            {
                ShowTextForm f = new ShowTextForm(Class);
                f.ShowDialog();
            }
            return Class;
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

            //string template = "Hello @Model.Tablename, < div ng - controller = '@Model.Name > ";
            DataTable data = GetDataTableFromDGV(dataGridView1);

            var config = new TemplateServiceConfiguration();
            // .. configure your instance
            config.ReferenceResolver = new MyIReferenceResolver();
            Engine.Razor = RazorEngineService.Create(config);

            var result =
                Engine.Razor.RunCompile(textTemplate, "templateKey",null, new  { Name = "Customer",TableName = "Customers",TableDefination = data });
            ShowTextForm f = new ShowTextForm(result);

            f.ShowDialog();
        }

        void parserWithcreateFile(string File,string location)
        {
            DataTable data = GetDataTableFromDGV(dataGridView1);
            if (comboBox2.Items.Contains(File+ ".txt"))
            {
                comboBox2.SelectedItem = File + ".txt";
                Parser p = new Parser(comboBox1.Text, data, textTemplate);
                string parsedtext = p.Parse();
                if (location != "")
                {
                    string SaveLocation = textBox1.Text + @"\" + p.Parse(location);
                    //ShowTextForm f = new ShowTextForm(parsedtext);
                    //f.ShowDialog();
                    System.IO.Directory.CreateDirectory(new FileInfo(SaveLocation).Directory.FullName);
                    

                    System.IO.File.WriteAllText(SaveLocation, parsedtext, System.Text.Encoding.UTF8);
                }
                else
                {
                    ShowTextForm f = new ShowTextForm(parsedtext);
                    f.ShowDialog();

                }
            }
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

        private void button3_Click(object sender, EventArgs e)
        {

        }

        string ModelName(string TableName)
        {
            string _ModelName = "";
            var d = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
            _ModelName = d.Singularize(TableName);
            if (_ModelName != "")
                _ModelName = _ModelName.Substring(0, 1).ToUpper() + _ModelName.Substring(1, _ModelName.Length - 1);
            return _ModelName;
        }
        private void button6_Click(object sender, EventArgs e)
        {

            foreach (int i in checkedListBox1.CheckedIndices)
            {

                string file = checkedListBox1.Items[i].ToString();
                string location = "";
                itwmslocations.TryGetValue(file,out location);

                if(file == "Add template")
                {
                   string parse = CreateAddView(location);
                    Parser p = new Parser(comboBox1.Text, new DataTable(), "");
                    string SaveLocation = textBox1.Text + @"\" + p.Parse(location);
                    //ShowTextForm f = new ShowTextForm(parsedtext);
                    //f.ShowDialog();
                    System.IO.Directory.CreateDirectory(new FileInfo(SaveLocation).Directory.FullName);


                    System.IO.File.WriteAllText(SaveLocation, parse, System.Text.Encoding.UTF8);
                }
                else
                parserWithcreateFile(file, location);
        





             checkedListBox1.SetItemCheckState(i, CheckState.Unchecked);
            }

        }

        private void button7_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog di = new FolderBrowserDialog();
            di.ShowDialog();
            textBox1.Text = di.RootFolder.ToString();
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                Properties.Settings.Default.ApplicationPath = textBox1.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if(!string.IsNullOrEmpty(textBox1.Text ))
                {
                    Properties.Settings.Default.ApplicationPath = textBox1.Text;
                    Properties.Settings.Default.Save();
                }
            }
        }
    }
    }

