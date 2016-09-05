using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DDA_Builder
{
   public class Parser
    {
        string _TableName = "";
        string _textTemplate = "";
        DataTable _Datatable = new DataTable();
       public Parser(string TableName,DataTable dt,string textTemplate)
            {
            _TableName = TableName;
            _Datatable = dt;
            _textTemplate = textTemplate;
        }
        public string Parse()
        {
            MatchCollection mcol = Regex.Matches(_textTemplate, @"@@(.*?)@@");

            foreach (Match m in mcol)
            {
                if(m.Value.Contains("Loop"))
                {
                   string result = Loop(m.Value);
                    _textTemplate = _textTemplate.Replace(m.Value, result);
                }
                else
                switch (m.Value)
                {
                    case "@@Table.Name@@":
                        _textTemplate = _textTemplate.Replace(m.Value, _TableName);
                        break;
                    default:
                        break;
                }
            }
            return _textTemplate;
        }

        string Loop(string syntax)
        {
            string result = "";
            syntax = syntax.Replace("@@", "");
            string[] Components = syntax.Split('%');
            string switcher = Components[1];
            string condition = Components[2];
            
            string conditionColumn = condition.Split('=')[0].Replace("Table.","");
            string conditionValue = condition.Split('=')[1];
            string body = Components[3];
            for (int i = 0; i < _Datatable.Rows.Count; i++)
            {
         
                if (_Datatable.Rows[i][conditionColumn].ToString() == conditionValue)
                {
                    result += body.Replace(@"$$" + switcher + "$$", _Datatable.Rows[i][switcher].ToString())+ System.Environment.NewLine;
                }
            }
            return result;
        }

    }
}
