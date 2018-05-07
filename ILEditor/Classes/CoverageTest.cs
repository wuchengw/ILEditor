﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILEditor.Classes
{
    public class CoverageTest
    {
        public static Dictionary<string, CoverageTest> Tests = new Dictionary<string, CoverageTest>();

        public static CoverageTest GetTest(string Name)
        {
            if (Tests.ContainsKey(Name))
                return Tests[Name];
            else
                return null;
        }

        public static void LoadTests()
        {
            string[] data;
            string json;

            data = IBMi.CurrentSystem.GetValue("TESTS").Split('|');

            foreach (string Test in data)
            {
                if (Test.Trim() == "") continue;
                json = IBMi.CurrentSystem.GetValue("TEST_" + Test);
                Tests.Add(Test, JsonConvert.DeserializeObject<CoverageTest>(json));
            }
        }

        public static void SaveTests()
        {
            IBMi.CurrentSystem.SetValue("TESTS", String.Join("|", Tests.Keys));
            string json = "";

            foreach(string Test in Tests.Keys)
            {
                json = JsonConvert.SerializeObject(
                    Tests[Test],
                    Newtonsoft.Json.Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                );

                IBMi.CurrentSystem.SetValue("TEST_" + Test, json);
            }
        }

        #region Class
        public string Name;
        public string Command;
        public List<ILEObject> Modules;

        public CoverageTest(string Name, string Command)
        {
            this.Name = Name;
            this.Command = Command;
            this.Modules = new List<ILEObject>();
        }

        public string Run()
        {
            string FileResult = "", Command = "", RemoteFile = "";
            List<string> ModuleParam = new List<string>();
            
            foreach(ILEObject Module in this.Modules)
            {
                ModuleParam.Add("(" + Module.Library + "/" + Module.Name + " " + Module.Type + ")");
            }

            FileResult = IBMiUtils.GetLocalFile("CODECOV", "TESTS", this.Name, "zip");
            RemoteFile = "/tmp/" + IBMi.CurrentSystem.GetValue("username") + '_' + this.Name + ".cczip";
            Command = "CODECOV CMD(" + this.Command + ") "
                    + "MODULE(" + String.Join(" ", ModuleParam) + ") "
                    + "OUTSTMF('" + RemoteFile + "') "
                    + "TESTID(" + Name + ")";
            
            IBMi.RemoteCommand($"CHGLIBL LIBL({ IBMi.CurrentSystem.GetValue("datalibl").Replace(',', ' ')}) CURLIB({ IBMi.CurrentSystem.GetValue("curlib") })");
            if (IBMi.RemoteCommand(Command))
            {
                if (IBMi.DownloadFile(FileResult, RemoteFile) == false) //false = it worked!!
                    return FileResult;
                else
                    return "";
            }
            else
            {
                return "";
            }
        }
        #endregion
    }
}