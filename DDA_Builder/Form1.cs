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
using RazorEngine.Text;

namespace DDA_Builder
{
    public partial class Form1 : Form
    {
       public static string DDAProjectLocation = "";
        public static string DDAProjectName = "";
        public static string SaveProjectLocation = "";
        public static  DataSet ProjectSettingsds = new DataSet();
        public Form1()
        {
            InitializeComponent();
        }


        Dictionary<string, string> itwmslocations = new Dictionary<string, string>();

        List<string> Tables = new List<string>();
        
        string textTemplate = "";
        private void Form1_Load(object sender, EventArgs e)
        {
            Project.ProjectAddSelect project = new Project.ProjectAddSelect();
            project.ShowDialog();
            if (string.IsNullOrEmpty(project.Projectname) || string.IsNullOrEmpty(project.Project_Location))
                System.Environment.Exit(0);
            this.Text = project.Projectname;
            DDAProjectLocation = project.Project_Location;
            DDAProjectName = project.Projectname;
            try
            {
                ProjectSettingsds.ReadXml(DDAProjectLocation + "\\" + "settings");
                Form1.SaveProjectLocation = ProjectSettingsds.Tables["Settings"].Rows[0]["ProjectLocation"].ToString();
                Loadtemplates(ProjectSettingsds.Tables["Settings"].Rows[0]["TemplateLocation"].ToString());
                LoadTables(ProjectSettingsds.Tables["Settings"].Rows[0]["ConnectionString"].ToString());

            }
            catch(FileNotFoundException)
            {
                Project.Project_settings settingswindo = new Project.Project_settings();
                    settingswindo.ShowDialog();
                    }
            textBox1.Text = Properties.Settings.Default.ApplicationPath;
         
        }

        void Loadtemplates(string TemplatesLocation)
        {
            string TemplateFolder = AppDomain.CurrentDomain.BaseDirectory.ToString() + @"Text template";
            DirectoryInfo d = new DirectoryInfo(TemplateFolder);//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            string str = "";
            foreach (FileInfo file in Files)
            {
                comboBox2.Items.Add(file.Name);
            }

            foreach (DataRow item in Form1.ProjectSettingsds.Tables["Templates"].Rows)
            {
                itwmslocations.Add(item["TemplateName"].ToString(), SaveProjectLocation+"\\"+ item["TemplatLocation"].ToString());

            }
            itwmslocations.Add("Add template", SaveProjectLocation + "\\" + @"CRMP.Web\Views\@Model.Name\AddForm.cshtml");
            foreach (var item in itwmslocations)
            {
                int i = checkedListBox1.Items.Add(item.Key);
            }
   
        }

        void LoadTables(string connectionString)
        {

            SqlConnection con = new SqlConnection(connectionString);
            SqlCommand com = new SqlCommand("SELECT table_name FROM information_schema.tables", con);
            con.Open();
            SqlDataReader r = com.ExecuteReader(CommandBehavior.CloseConnection);
            while (r.Read())
            {
                checkedListBox2.Items.Add(r[0].ToString());
                Tables.Add(r[0].ToString());

                //checkedListBox2.Items.Add(r[0].ToString());
                RTable.Items.Add(r[0].ToString());
            }
            RTable.Items.Add("");
            con.Close();
        }
        private void checkedListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            SqlConnection con = new SqlConnection(ProjectSettingsds.Tables["Settings"].Rows[0]["ConnectionString"].ToString());
            SqlCommand com = new SqlCommand("SELECT c.name 'Column Name',t.Name 'Data type',c.max_length 'Max Length',c.is_nullable, ISNULL(i.is_primary_key, 0) 'Primary Key' FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id LEFT OUTER JOIN sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id LEFT OUTER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id WHERE c.object_id = OBJECT_ID(@tableName)", con);
            com.Parameters.Add("tableName", SqlDbType.Text).Value = checkedListBox2.Text;
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
            DataSet ds = new DataSet();
            try
            {
                ds.ReadXml(checkedListBox2.Text);
                for (int ro = 0; ro < ds.Tables[0].Rows.Count; ro++)
                {
                    for (int c = 0; c < ds.Tables[0].Columns.Count; c++)
                    {
                        if (ds.Tables[0].Columns[c].ColumnName == "DataType" || ds.Tables[0].Columns[c].ColumnName == "RModel")
                            continue;
                        dataGridView1.Rows[ro].Cells[ds.Tables[0].Columns[c].ColumnName].Value = ds.Tables[0].Rows[ro][c];

                    }
                    //dataGridView1[] 

                }
            }
            catch(Exception ex) {
                MessageBox.Show(ex.Message); }





        }

        private void button2_Click(object sender, EventArgs e)
        {
            string Class = "    public class "+checkedListBox2.Text+"ViewModel {"+ System.Environment.NewLine;
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
            string Modelname = ModelName(checkedListBox2.Text);
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
            Class += @"@{
    Layout = null;
}";
            Class += @"
    <h3>
        {{ 'General." + Modelname + @"' | translate }} {{ 'General.Edit' | translate }}
      </h3> ";
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
                    if (itemenabled != null && (datarowdata.Cells[3].Value != null && !string.IsNullOrEmpty(datarowdata.Cells[3].Value.ToString()) && Convert.ToBoolean(itemenabled)))
                    {
                        if (!tabs.ContainsKey(datarowdata.Cells[3].Value.ToString()))
                        {
                            if (first)
                            {
                                Class += "<li class='active'><a data-toggle='tab' href data-target='#" + datarowdata.Cells[3].Value.ToString().Trim() + "'>" + datarowdata.Cells[3].Value.ToString() + "</a></li>" + System.Environment.NewLine; ;
                                tabs.Add(datarowdata.Cells[3].Value.ToString(), "<div id='" + datarowdata.Cells[3].Value.ToString().Trim() + "' class='tab-pane fade in active'>");
                                first = false;
                            }
                            else
                            {
                                Class += "<li><a data-toggle='tab' href data-target='#" + datarowdata.Cells[3].Value.ToString().Trim() + "'>" + datarowdata.Cells[3].Value.ToString() + "</a></li>" + System.Environment.NewLine; ;
                                tabs.Add(datarowdata.Cells[3].Value.ToString(), "<div id='" + datarowdata.Cells[3].Value.ToString().Trim() + "' class='tab-pane fade'>");
                            }

                        }
                    }
                }
                Class += "</ul>" + System.Environment.NewLine;

            }
            Class += @"
<div ng-controller=' " + Modelname + @"Ctrl'>";
            Class += @"    <div class='row'>
        <div class='panel panel-default'>
            <div class='panel-body'>";
            Class += @"<form role='form' name='" + Modelname + "Form'  data-ng-init='init()'>" + System.Environment.NewLine;

            //add validation errors
            Class += @"<div class='alert alert-error ng-cloak' ng-cloak ng-if='errors.formErrorsSummary.length > 0'><h4 > The following errors were found:</h4 ><ul ng-repeat = 'error in errors.formErrorsSummary' ><li class='ng-cloak'>{{error.Message}}</li></ul></div>" + System.Environment.NewLine;

            if (hastabs)
                Class += "<div class='tab-content row'>" + System.Environment.NewLine;

            foreach (DataGridViewRow datarowdata in dataGridView1.Rows)
            {
                if (datarowdata.Cells[2].Value != null && bool.Parse(datarowdata.Cells[2].Value.ToString()) == false)
                    continue;
                if (datarowdata.Cells[0].Value != null && Convert.ToBoolean(datarowdata.Cells[2].Value))
                {
                    string item = "";
                    if (datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True")
                    {
                        item += " <div class='form-group col-sm-6' "+ (datarowdata.Cells[1].Value.ToString() == "datetime"? " ng-controller='DatepickerDemoCtrl as dpick' ":"") + @" ng-class=""{ 'has-error': " + Modelname + "Form." + datarowdata.Cells[0].Value.ToString() + ".$invalid }\"> " + System.Environment.NewLine;
                    }
                    else
                        item += @" <div class=""form-group col-sm-6"" " + (datarowdata.Cells[1].Value.ToString() == "datetime" ? " ng-controller='DatepickerDemoCtrl as dpick' " : "") + "> " + System.Environment.NewLine;
                      if ((datarowdata.Cells[6].Value != null) && (!string.IsNullOrEmpty(datarowdata.Cells[6].Value.ToString()) || !string.IsNullOrEmpty(datarowdata.Cells[7].Value.ToString())))
                        item += " <label>{{'" + datarowdata.Cells[6].Value + "."+ datarowdata.Cells[7].Value + "' | translate }}:</label> " +
                             System.Environment.NewLine;
                        else
                        item += " <label>{{'" + checkedListBox2.Text + "." + datarowdata.Cells[0].Value.ToString() + "' | translate }}:</label> " +
     System.Environment.NewLine;


                    if (datarowdata.Cells[1].Value.ToString() == "datetime" )
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
                    else if (datarowdata.Cells[1].Value.ToString() == "bit")
                    {
                        item += @"<div class=""checkbox c-checkbox needsclick input-group "">"
                                +"<label class='needsclick'>"
                                 + @"   <input class=""needsclick"" name='" + datarowdata.Cells[0].Value.ToString() + @"' id='" + datarowdata.Cells[0].Value.ToString() + @"'  type=""checkbox"" ng-checked='current" + Modelname + "." + datarowdata.Cells[0].Value.ToString() + "'  ng-model='current" + Modelname + "." + datarowdata.Cells[0].Value.ToString() +"'  > "
                                    +@"<span class=""fa fa-check""></span>"
                                +"</label>"
                            +"</div>" + System.Environment.NewLine;
                    }
                    else if ((datarowdata.Cells[6].Value != null && datarowdata.Cells[7].Value != null) && (!string.IsNullOrEmpty(datarowdata.Cells[6].Value.ToString()) || !string.IsNullOrEmpty(datarowdata.Cells[7].Value.ToString())))
                    {
                        item += " <select class='form-control' name='" + datarowdata.Cells[0].Value.ToString() + "' id='" + datarowdata.Cells[0].Value.ToString() + "' ng-model='current" + Modelname + "." + datarowdata.Cells[0].Value.ToString() +
         "' " + ((datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True") ? "required" : "") + "     " + ((datarowdata.Cells[5].Value != null && datarowdata.Cells[5].Value.ToString().Trim() != "") ? "maxlength=" + datarowdata.Cells[5].Value.ToString().Trim() + "  " : "") + @"  ng-options=""i." + ModelName(datarowdata.Cells[8].Value.ToString().Trim()) + @" as (i." + ModelName(datarowdata.Cells[7].Value.ToString().Trim()) + @") for i in " + datarowdata.Cells[6].Value.ToString().Trim() + @"GroupList""  ><option></option> </select>" + System.Environment.NewLine;
                    }
                    else if (datarowdata.Cells[6].Value == null && datarowdata.Cells[7].Value != null && !string.IsNullOrEmpty(datarowdata.Cells[7].Value.ToString()))
                    {

                        string[] options = datarowdata.Cells[7].Value.ToString().Replace('{', ' ').Replace('}', ' ').Split(',');
                        string[] optionsValue = datarowdata.Cells[8].Value != null && !string.IsNullOrEmpty(datarowdata.Cells[8].Value.ToString()) ? datarowdata.Cells[8].Value.ToString().Replace('{', ' ').Replace('}', ' ').Split(','): new string[0];
                        item += " <select class='form-control' name='" + datarowdata.Cells[0].Value.ToString() + "' id='" + datarowdata.Cells[0].Value.ToString() + "' ng-model='current" + Modelname + "." + datarowdata.Cells[0].Value.ToString() +
         "' " + ((datarowdata.Cells[4].Value != null && datarowdata.Cells[4].Value.ToString() == "True") ? "required" : "") + "     " + ((datarowdata.Cells[5].Value != null && datarowdata.Cells[5].Value.ToString().Trim() != "") ? "maxlength=" + datarowdata.Cells[5].Value.ToString().Trim() + "  " : "") + @"
                >" + System.Environment.NewLine;

                        string v = options[1];
                        if(optionsValue.Count()> 0)
                        for (int i = 0; i < options.Count(); i++)
                        {
                            item += @"<option  value="""+optionsValue[i].Trim()+@""">" + options[i].Trim() + "</option>" + Environment.NewLine;
                        }
                        else
                            for (int i = 0; i < options.Count(); i++)
                            {
                                item += @"<option>" + options[i].Trim() + "</option>" + Environment.NewLine;
                            }

                        item += "</select>";
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
            Class += "    <button type='button' class='btn btn-primary' ng-click='save" + Modelname + "()' ng-disabled='" + Modelname + @"Form.$invalid'> {{ 'General.Add' | translate }}" + "</button>";
            Class += @"   <button type = 'button' class='btn btn-primary' ng-click=""$state.go('app."+ checkedListBox2.Text + @"List','');"" ng-disabled='CarForm.$invalid'><i class=""fa fa-times"" />{{ 'General.Cancel' | translate }}</button>";

        

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
            string _TableName = checkedListBox2.Text;
            var d = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
            string _ModelName = d.Singularize(_TableName);
            //string template = "Hello @Model.Tablename, < div ng - controller = '@Model.Name > ";
            DataTable data = GetDataTableFromDGV(dataGridView1);
           // data.WriteXml(checkedListBox2.Text);
            var config = new TemplateServiceConfiguration();
            // .. configure your instance
            config.ReferenceResolver = new MyIReferenceResolver();
            Engine.Razor = RazorEngineService.Create(config);

            var result =
                Engine.Razor.RunCompile(textTemplate,_TableName+"Quickparse",null, new  { Name = _ModelName,TableName = _TableName,TableDefination = data });
            ShowTextForm f = new ShowTextForm(result);

            f.ShowDialog();
        }

        void parserWithcreateFile(string File,string location)
        {
            DataTable data = GetDataTableFromDGV(dataGridView1);
            data.WriteXml(checkedListBox2.Text);
            if (comboBox2.Items.Contains(File))
            {
                comboBox2.SelectedItem = File ;
                //Parser p = new Parser(checkedListBox2.Text, data, textTemplate);
                //string parsedtext = p.Parse();


                string _TableName = checkedListBox2.Text;
                var d = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
                string  _ModelName = d.Singularize(_TableName);


                var config = new TemplateServiceConfiguration();
                // .. configure your instance
                config.ReferenceResolver = new MyIReferenceResolver();
                config.Debug = true;
                config.EncodedStringFactory = new RawStringFactory();
                
                Engine.Razor = RazorEngineService.Create(config);
               // string templateFile = "C:/mytemplate.cshtml";
                string parsedtext =
                    Engine.Razor.RunCompile(textTemplate,_TableName+ File, null, new { Name = _ModelName, TableName = _TableName, TableDefination = data });

                if (location != SaveProjectLocation+"\\")
                {
                    string SaveLocation = Engine.Razor.RunCompile(location,"location", null, new { Name = _ModelName, TableName = _TableName, TableDefination = data });
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

        string parser(string File,string TableName)
        {
            DataTable data = GetDataTableFromDGV(dataGridView1);
            data.WriteXml(TableName);
            if (comboBox2.Items.Contains(File))
            {
                comboBox2.SelectedItem = File;
                //Parser p = new Parser(checkedListBox2.Text, data, textTemplate);
                //string parsedtext = p.Parse();


                string _TableName = TableName;
                var d = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
                string _ModelName = d.Singularize(_TableName);


                var config = new TemplateServiceConfiguration();
                // .. configure your instance
                config.ReferenceResolver = new MyIReferenceResolver();
                config.Debug = true;
                config.EncodedStringFactory = new RawStringFactory();

                Engine.Razor = RazorEngineService.Create(config);
                // string templateFile = "C:/mytemplate.cshtml";
                string parsedtext =
                    Engine.Razor.RunCompile(textTemplate, _TableName + File, null, new { Name = _ModelName, TableName = _TableName, TableDefination = data });


                return parsedtext;


            }
            else return null;
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
            dt.Columns.Add("RModel");
            object[] cellValues = new object[dgv.Columns.Count];
            var d = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));

            foreach (DataGridViewRow row in dgv.Rows)
            {
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    cellValues[i] = row.Cells[i].Value;

                    if(cellValues[i] != null && cellValues[i].ToString() == "True")
                    {
                        cellValues[i] = "true";
                    }
                }
                dt.Rows.Add(cellValues);
            }

            foreach (DataRow item in dt.Rows)
            {
                if(item[1].ToString() == "nvarchar")
                {
                    item[1] = "string";
                }
                else if (item[1].ToString() == "bit")
                {
                    item[1] = "bool";
                }
                else if (item[1].ToString() == "timestamp")
                {
                    item[1] = "DateTime";
                }


                if(!string.IsNullOrEmpty(item["Rtable"].ToString()) )
                {
                    item["RModel"] = d.Singularize(item["Rtable"].ToString());
                }
                
            }
            dt.TableName = "settings";
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
                    Parser p = new Parser(checkedListBox2.Text, new DataTable(), "");

                    string _TableName = checkedListBox2.Text;
                    var d = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
                    string _ModelName = d.Singularize(_TableName);
                   string loc =  Engine.Razor.RunCompile(location, _TableName + "Edit", null, new { Name = _ModelName, TableName = _TableName });
                    
                    //ShowTextForm f = new ShowTextForm(parsedtext);
                    //f.ShowDialog();
                    System.IO.Directory.CreateDirectory(new FileInfo(loc).Directory.FullName);


                    System.IO.File.WriteAllText(loc, parse, System.Text.Encoding.UTF8);
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

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            DataTable data = GetDataTableFromDGV(dataGridView1);
            data.WriteXml(checkedListBox2.Text);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Project.Project_settings settingswindo = new Project.Project_settings();
            settingswindo.ShowDialog();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var service = Engine.Razor;
            // In this example I'm using the default configuration, but you should choose a different template manager: http://antaris.github.io/RazorEngine/TemplateManager.html
            service.AddTemplate("part", @" my part template");
            service.AddTemplate("layout", @"<h1>@RenderBody()@Include(""part"")</h1>");
            service.AddTemplate("template", @"@{Layout = ""layout"";}my template");
            service.Compile("template");
            var result = service.Run("template");
            Console.WriteLine("Result is: {0}", result);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox2.Items.Count; i++)
            {
                
                checkedListBox2.SetItemChecked(i, checkedListBox2.GetItemCheckState(i) != CheckState.Checked);
            }
            if (button11.Text == "Select All")
                button11.Text = "Deselect All";
            else
                button11.Text = "Select All";
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string Result = "";

                foreach (int i in checkedListBox1.CheckedIndices)
                {
                foreach (int d in checkedListBox2.CheckedIndices)
                {
                    checkedListBox2.SelectedIndex = d;
                    string file = checkedListBox1.Items[i].ToString();
                    //itwmslocations.TryGetValue(file, out location);
                    Result += parser(file, checkedListBox2.Items[d].ToString());
                }
                checkedListBox1.SetItemCheckState(i, CheckState.Unchecked);
            }
        
            ShowTextForm f = new ShowTextForm(Result);
            f.ShowDialog();
        }


        //private void checkedListBox2_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    dataGridView1.Rows.Clear();
        //    SqlConnection con = new SqlConnection(Properties.Settings.Default.Connection);
        //    SqlCommand com = new SqlCommand("SELECT c.name 'Column Name',t.Name 'Data type',c.max_length 'Max Length',c.is_nullable, ISNULL(i.is_primary_key, 0) 'Primary Key' FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id LEFT OUTER JOIN sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id LEFT OUTER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id WHERE c.object_id = OBJECT_ID(@tableName)", con);
        //    com.Parameters.Add("tableName", SqlDbType.Text).Value = checkedListBox2.Text;
        //    con.Open();
        //    SqlDataReader r = com.ExecuteReader(CommandBehavior.CloseConnection);
        //    while (r.Read())
        //    {
        //        int row = dataGridView1.Rows.Add();
        //        dataGridView1[0, row].Value = r[0].ToString();
        //        dataGridView1[1, row].Value = r[1].ToString();
        //        dataGridView1[4, row].Value = !Convert.ToBoolean(r[3].ToString());
        //        dataGridView1[2, row].Value = true;

        //    }
        //    con.Close();
        //    DataSet ds = new DataSet();
        //    try
        //    {
        //        ds.ReadXml(checkedListBox2.Text);
        //        for (int ro = 0; ro < ds.Tables[0].Rows.Count; ro++)
        //        {
        //            for (int c = 0; c < ds.Tables[0].Columns.Count; c++)
        //            {
        //                if (ds.Tables[0].Columns[c].ColumnName == "DataType" || ds.Tables[0].Columns[c].ColumnName == "RModel")
        //                    continue;
        //                dataGridView1.Rows[ro].Cells[ds.Tables[0].Columns[c].ColumnName].Value = ds.Tables[0].Rows[ro][c];

        //            }
        //            //dataGridView1[] 

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }


        //}
    }
    }

