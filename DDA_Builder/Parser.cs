﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DDA_Builder
{
   public class Parser
    {
         string _TableName = "";
        string _ModelName = "";
         string _textTemplate = "";
        DataTable _Datatable = new DataTable();
       public Parser(string TableName,DataTable dt,string textTemplate)
            {
            _TableName = TableName;
            _Datatable = dt;
            _textTemplate = textTemplate;
            var d = System.Data.Entity.Design.PluralizationServices.PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
            _ModelName = d.Singularize(TableName);
            if(_ModelName != "")
            _ModelName = _ModelName.Substring(0,1).ToUpper() + _ModelName.Substring(1, _ModelName.Length-1);
        }
        public Parser()
        {

        }
        public string Parse()
        {
     
            for (int i = 0; i < 2; i++)
            {
                MatchCollection mcol = Regex.Matches(_textTemplate, @"@@(.*?)@@");
                foreach (Match m in mcol)
            {

                switch (m.Value)
                {
                    case "@@Table.Name@@":
                        _textTemplate = _textTemplate.Replace(m.Value, _TableName);
                        break;
                        case "@@Model.Name@@":
                            _textTemplate = _textTemplate.Replace(m.Value, _ModelName);
                            break;

                        default:
                        break;
                }
            }

            }
            MatchCollection mcol2 = Regex.Matches(_textTemplate, @"@@((\n|\r|\r\n|.)*?)@@");
            foreach (Match m in mcol2)
            {
                if (m.Value.Contains("Loop"))
                {
                    string result = Loop(m.Value);
                    _textTemplate = _textTemplate.Replace(m.Value, result);
                }
                if (m.Value.Contains("IFF"))
                {
                    string result = IFF(m.Value);
                    _textTemplate = _textTemplate.Replace(m.Value, result);
                }
            }



                return _textTemplate;
        }

        public string Parse(string syntax)
        {

            for (int i = 0; i < 2; i++)
            {
                MatchCollection mcol = Regex.Matches(syntax, @"@@(.*?)@@");
                foreach (Match m in mcol)
                {

                    switch (m.Value)
                    {
                        case "@@Table.Name@@":
                            syntax = syntax.Replace(m.Value, _TableName);
                            break;
                        case "@@Model.Name@@":
                            syntax = syntax.Replace(m.Value, _ModelName);
                            break;

                        default:
                            break;
                    }
                }

            }
            return syntax;
        }


        string Loop(string syntax)
        {
            string result = "";
            syntax = syntax.Replace("@@", "");
            string[] Components = syntax.Split('%');
            string switcher = Components[1];
            string condition = Components[2];
            string _operator = "";
            if (condition.Contains("!="))
                _operator = "!=";
            else
                _operator = "=";
            string conditionColumn = condition.Split(_operator.ToCharArray())[0].Replace("Table.", "");
            string conditionValue = condition.Split(_operator.ToCharArray())[1] != null?condition.Split(_operator.ToCharArray())[1]: condition.Split(_operator.ToCharArray())[2];
            string body = Components[3];
            for (int i = 0; i < _Datatable.Rows.Count; i++)
            {
                bool valid = false; 
                if(_operator == "=")
                {
                    if(conditionValue.ToLower() == "null")
                    {
                        if (_Datatable.Rows[i][conditionColumn] == null)
                            valid = true;
                    }
                    else if (_Datatable.Rows[i][conditionColumn].ToString() == conditionValue)
                        valid = true;
                }
                else if (_operator == "!=")
                {
                    if (conditionValue.ToLower() == "null")
                    {
                        if (!string.IsNullOrEmpty(_Datatable.Rows[i][conditionColumn].ToString()))
                            valid = true;
                    }
                    else if (_Datatable.Rows[i][conditionColumn].ToString() != conditionValue)
                        valid = true;
                }
         
                if (valid)
                {
                    result += body.Replace(@"$$" + switcher + "$$", _Datatable.Rows[i][switcher].ToString()).Replace(@"$$" + switcher + ".Type$$", GetDataType(_Datatable.Rows[i][1].ToString())) + System.Environment.NewLine;
                }
            }
            return result;
        }

        string IFF(string syntax)
        {
            string switcher = "";
            string result = "";
            syntax = syntax.Replace("@@", "");
            string[] Components = syntax.Split('%');
            string condition = Components[1];
            string _operator = "";
            if (condition.Contains("!="))
                _operator = "!=";
            else
                _operator = "=";
            string conditionLeftSide = condition.Split(_operator.ToCharArray())[0];
            string conditionValue = condition.Split(_operator.ToCharArray())[1] != null ? condition.Split(_operator.ToCharArray())[1] : condition.Split(_operator.ToCharArray())[2];
            for (int i = 0; i < _Datatable.Rows.Count; i++)
            {
                bool valid = false;
                if (_operator == "=")
                {
                     if (conditionLeftSide == conditionValue)
                        valid = true;
                }
                else if (_operator == "!=")
                {
                     if (conditionLeftSide != conditionValue)
                        valid = true;
                }

                if (valid)
                {
                    result = Components[2];
                }
                else
                {
                    result = Components[3];
                }
            }
            return result;
        }

        string GetDataType(string SqlType)
        {
            if (SqlType.Contains("varchar"))
                return "string";
            else if (SqlType.Contains("int"))
                return "int?";
            else if (SqlType.Contains("uniqueidentifier"))
                return "Guid";
            else if (SqlType.Contains("bit"))
                return "bool?";
            else if (SqlType.Contains("datetime"))
                return "DateTime?";
            else
                return SqlType;
        }
    }
}
