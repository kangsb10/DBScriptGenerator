using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.IO;

namespace DBScripter
{
    class Test : ezBase
    {
        public void TestScript()
        {
            try
            {
                SqlConnection conn = new SqlConnection(GetSystemConfigValue("master"));
                ServerConnection serverConn = new ServerConnection(conn);
                var server = new Server(serverConn);
                var database = server.Databases["DeleteCompany"];

                var scripter = new Scripter(server);
                scripter.Options.IncludeIfNotExists = true;
                scripter.Options.ScriptSchema = true;
                scripter.Options.ScriptData = true;

                string scrs = "";
                //Script out Tables
                foreach (Table myTable in database.Tables)
                {
                    foreach (string s in scripter.EnumScript(new Urn[] { myTable.Urn }))
                        scrs += s + "\n\n"; ;
                }

                //Script out Views
                foreach (View myView in database.Views)
                {
                    //Skip system views
                    //There is a scripter.Options.AllowSystemObjects = false; setting that does the same but it is glacially slow
                    if (myView.IsSystemObject == true) continue;
                    foreach (string s in scripter.EnumScript(new Urn[] { myView.Urn }))
                        scrs += s + "\n\n";
                }


                if (!Directory.Exists(GetSystemConfigValue("backupPath")))
                {
                    Directory.CreateDirectory(GetSystemConfigValue("backupPath"));
                }

                string scriptPath = string.Format("{0}\\{1}.sql", GetSystemConfigValue("backupPath"), "TEST");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(scriptPath))
                {
                    file.WriteLine(scrs.ToString());
                }
            }
            catch (Exception ex)
            {
                WriteTextLog("", "", ex.ToString());
            }



        }

    }
}
