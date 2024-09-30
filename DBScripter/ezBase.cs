using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;

namespace DBScripter
{
    class ezBase
    {
        private static string server = null;
        private static string uid = null;
        private static string pwd = null;
        private static ServerConnection conn = null;
        public static Server srv = null;
        public static bool scriptData = false;
        public static XmlDocument dbList = null;
        public static int totalDBcount = 0;

        public ezBase()
        {
            server = GetSystemConfigValue("server");
            uid = GetSystemConfigValue("uid");
            pwd = GetSystemConfigValue("pwd");
            conn = new ServerConnection(server, uid, pwd);
            srv = new Server(conn);
            scriptData = (GetSystemConfigValue("scriptData").Equals("Y") ? true : false);

        }

        protected static void WriteTextLog(string pPage, string pFunction, string pMessage)
        {
            Console.WriteLine(pMessage);

            WriteTextLog_File(pPage, pFunction, pMessage);

        }

        protected static void WriteTextLog_File(string pPage, string pFunction, string pMessage)
        {
            try
            {
                string folderpath = GetSystemConfigValue("ezCommon_RootLogPath");
                if (folderpath == "") return;
                //string filepath = folderpath + "\\" + "Home";
                string filepath = folderpath;
                if (!System.IO.Directory.Exists(filepath))
                    System.IO.Directory.CreateDirectory(filepath);

                filepath += "\\LOG[" + DateTime.Now.ToLongDateString() + "].txt";
                StreamWriter output = new StreamWriter(filepath, true, System.Text.Encoding.Unicode);
                output.WriteLine(System.Environment.MachineName.ToString() + "||");
                output.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "||" + Guid.NewGuid().ToString() + "||");
                output.WriteLine("WEB||" + pPage + "||" + pFunction + "||");
                output.WriteLine(pMessage);
                output.WriteLine("|END|");
                output.Close();
            }
            catch
            { }
        }

        protected static string GetSystemConfigValue(string pKeyValue)
        {
            string pResult = string.Empty;
            try
            {
                string pKeyResult = "";
                pKeyResult = ConfigurationManager.AppSettings[pKeyValue];
                if (pKeyResult == null)
                {
                    pKeyResult = ConfigurationManager.ConnectionStrings[pKeyValue].ConnectionString;
                }

                pResult = pKeyResult;
            }
            catch (Exception Ex)
            {
                WriteTextLog("ezBase", "GetSystemConfigValue", "inputData=" + pKeyValue + Ex.ToString());
                pResult = "Exception";
            }
            return pResult;
        }

        public static string GetQueryResult(string pKeyName, string pSQL, bool pIsMultiLine)
        {
            SqlConnection conn = new SqlConnection(GetSystemConfigValue(pKeyName));

            try
            {
                conn.Open();
                SqlCommand comd = new SqlCommand(pSQL, conn);
                SqlDataReader dr = comd.ExecuteReader();
                comd.Dispose();

                StringBuilder result = new StringBuilder("<DATA>");

                while (dr.Read())
                {
                    if (pIsMultiLine)
                        result.Append("<ROW>");

                    for (int i = 0; i < dr.FieldCount; i++)
                        result.Append("<" + dr.GetName(i).ToUpper() + ">" + MakeXMLString(dr.GetSqlValue(i).ToString()) +
                            "</" + dr.GetName(i).ToUpper() + ">");

                    if (pIsMultiLine)
                        result.Append("</ROW>");
                    else
                        break;
                }

                dr.Close();
                result.Append("</DATA>");

                return result.ToString();
            }
            catch (Exception Ex)
            {
                WriteTextLog("ezBase", "GetQueryResult", Ex.ToString());
                return "<DATA />";
            }
            finally
            {
                conn.Close();
                conn.Dispose();
            }

        }

        public static string MakeXMLString(string pOrgString)
        {
            return pOrgString.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

    }
}
