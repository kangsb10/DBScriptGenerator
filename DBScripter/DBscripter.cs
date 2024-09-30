using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace DBScripter
{
    class DBscripter : ezBase
    {
        public void ScriptingDB()
        {
            try
            {
                if (dbList == null)
                    dbList = GetDBlist();

                totalDBcount = dbList.SelectSingleNode("DATA").ChildNodes.Count;

                WriteTextLog("", "", "Connect status : " + srv.Status.ToString() + "\n");
                WriteTextLog("", "", "Extract DB Count : " + totalDBcount + "\n");

                if (!srv.Status.ToString().ToUpper().Equals("ONLINE"))
                {
                    WriteTextLog("", "", "Connect faild...Status : " + srv.Status);
                    return;
                }

                if (dbList != null && dbList.SelectSingleNode("DATA").ChildNodes.Count > 0)
                {
                    for (int i = 0; i < dbList.GetElementsByTagName("ROW").Count; i++)
                    {
                        string dbName = dbList.GetElementsByTagName("ROW")[i].InnerText;
                        Database db = srv.Databases[dbName];

                        if (db.IsSystemObject)  // master, model, msdb, tempdb 등 시스템DB 제외
                            continue;

                        Console.WriteLine("[{0}] Scripting schema....", dbName);
                        string scriptSchemaResult = ExtractSchema(db);
                        Console.WriteLine("[{0}] Scripting schema Result : {1}\n", dbName, scriptSchemaResult);

                        if (scriptSchemaResult.Equals("OK") && scriptData.Equals(true))
                        {
                            Console.WriteLine("[{0}] Scripting data....", dbName);
                            string scriptDataResult = ExtractData(db);
                            Console.WriteLine("[{0}] Scripting data Result : {1}\n", dbName, scriptDataResult);
                        }

                        if (i != dbList.GetElementsByTagName("ROW").Count - 1)
                            Console.WriteLine("-------------------------------------------------------------------\n");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteTextLog("DBscripter", "ScriptingDB", ex.ToString());
            }
            finally
            {
                WriteTextLog("", "", "================================END=================================");
            }
        }

        /// <summary>
        /// 스키마 스크립트
        /// </summary>
        /// <param name="pDB"></param>
        /// <returns></returns>
        public static string ExtractSchema(Database pDB)
        {
            string rtnVal = "";

            try
            {
                Scripter scrp = new Scripter(srv);

                #region{일반}
                //scrp.Options.AnsiPadding = false;                                             // 1.ANSI 패딩
                //scrp.Options.ScriptDrops = false;                                             // 2. DROP 및 CREATE 스크립팅
                //scrp.Options.ConvertUserDefinedDataTypesToBaseType = false;                   // 3.UDDT를 기본 형식으로 변환
                //scrp.Options.IncludeDatabaseContext = true;                                   // 4. USE DATABASE 스크립팅
                //scrp.Options.IncludeIfNotExists = false;                                      // 개체의 존재 여부 확인??
                //scrp.Options.Default = true;                                                  // 기본값 스크립팅?
                //scrp.Options.TargetDatabaseEngineEdition = DatabaseEngineEdition.Standard;    // 데이터베이스 엔진 버전에 대한 스크립트
                //scrp.Options.TargetDatabaseEngineType = DatabaseEngineType.Standalone;        // 데이터베이스 엔진 유형에 대한 스크립트
                //scrp.Options.LoginSid = false;                                                // 로그인 스크립팅
                //scrp.Options.ExtendedProperties = true;                                       // 확장 속성 스크립팅
                //scrp.Options.Bindings = false;                                                // 스크립트 바인딩
                //scrp.Options.Statistics = false;                                              // 통계 스크립팅
                //scrp.Options.ScriptOwner = false;                                             // 스크립트 소유자
                //scrp.Options.IncludeScriptingParametersHeader = false;                        // 스크립팅 매개 변수 헤더 포함
                //scrp.Options.ExtendedProperties = true;                                       // 확장 속성 스크립팅
                //scrp.Options.ClusteredIndexes = true;                                         // ?
                //scrp.Options.NoCollation = true;                                              // ?
                //scrp.Options.IncludeHeaders = true;                                           
                //scrp.Options.IncludeScriptingParametersHeader = false;                        // 스크립팅 매개 변수 헤더 포함
                //scrp.Options.engine
                #endregion

                #region{테이블/뷰 옵션}
                scrp.Options.DriChecks = true;                                                  // 1. CHECK 제약 조건 스크립팅
                scrp.Options.DriUniqueKeys = true;                                              // 2. 고유 키 스크립팅
                scrp.Options.DriPrimaryKey = true;                                              // 3. 기본 키 스크립팅
                scrp.Options.ScriptDataCompression = false;                                     // 4. 데이터 압축 옵션 스크립팅
                scrp.Options.ChangeTracking = false;                                            // 5. 변경 내용 추적 스크립팅
                scrp.Options.ScriptXmlCompression = false;                                      // 6. 스크립트 Xml 압축 옵션
                scrp.Options.DriForeignKeys = true;                                             // 7. 외래 키 스크립팅
                scrp.Options.DriIndexes = true;                                                 // 8. 인덱스 스크립팅
                scrp.Options.FullTextIndexes = false;                                           // 9. 전체 텍스트 인덱스 스크립팅

                scrp.Options.Triggers = true;                                                   // 10. 트리거 스크립팅
                #endregion

                #region{스크립팅할 데이터 형식 (스키마만/데이터만/스키마 및 데이터)}
                scrp.Options.ScriptSchema = true;                                               // 스키마
                scrp.Options.ScriptData = false;                                                // 데이터
                #endregion

                var urns = new List<Urn>();

                #region{TABLE}
                Console.Write("Scripting tables...");
                int totalTableCnt = pDB.Tables.Count;
                int curTableCnt = 0;
                using (var progress = new ProgressBar())
                {
                    foreach (Table tb in pDB.Tables)
                    {
                        if (tb.IsSystemObject == false)
                        {
                            progress.Report((double)++curTableCnt / totalTableCnt);
                            urns.Add(tb.Urn);
                        }
                    }
                }
                Console.WriteLine("END");
                #endregion

                #region{VIEW}
                Console.Write("Scripting views...");
                int totalViewCnt = pDB.Views.Count;
                int curViewCnt = 0;
                using (var progress = new ProgressBar())
                {
                    foreach (View view in pDB.Views)
                    {
                        if (view.IsSystemObject == false)
                        {
                            progress.Report((double)++curViewCnt / totalViewCnt);
                            urns.Add(view.Urn);
                        }
                    }
                }
                Console.WriteLine("END");
                #endregion

                #region{PROCEDURE}
                Console.Write("Scripting procedures...");
                int totalSPcnt = pDB.StoredProcedures.Count;
                int curSPcnt = 0;
                using (var progress = new ProgressBar())
                {
                    XmlDocument xmldomProc = new XmlDocument();
                    xmldomProc.LoadXml(GetRoutineList("PROCEDURE", pDB.Name));

                    foreach (XmlNode node in xmldomProc.SelectSingleNode("DATA").ChildNodes)
                    {
                        string procedureName = node["ROUTINE_NAME"].InnerText;
                        StoredProcedure sp = pDB.StoredProcedures[procedureName];
                        if (!sp.IsSystemObject)
                        {
                            progress.Report((double)++curSPcnt / totalSPcnt);
                            urns.Add(sp.Urn);
                        }
                    }
                }
                Console.WriteLine("END");
                #endregion

                #region{FUNCTION}
                Console.Write("Scripting functions...");
                int totalFuncCnt = pDB.UserDefinedFunctions.Count;
                int curFuncCnt = 0;
                using (var progress = new ProgressBar())
                {
                    XmlDocument xmldomFnc = new XmlDocument();
                    xmldomFnc.LoadXml(GetRoutineList("FUNCTION", pDB.Name));

                    foreach (XmlNode node in xmldomFnc.SelectSingleNode("DATA").ChildNodes)
                    {
                        string functionName = node["ROUTINE_NAME"].InnerText;
                        UserDefinedFunction fnc = pDB.UserDefinedFunctions[functionName];
                        if (!fnc.IsSystemObject)
                        {
                            progress.Report((double)++curFuncCnt / totalFuncCnt);
                            urns.Add(fnc.Urn);
                        }

                    }
                }
                Console.WriteLine("END");
                #endregion

                if (!Directory.Exists(GetSystemConfigValue("backupPath")))
                {
                    Directory.CreateDirectory(GetSystemConfigValue("backupPath"));
                }

                string scriptPath = string.Format("{0}\\{1}.sql", GetSystemConfigValue("backupPath"), pDB.Name);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(scriptPath))
                {
                    System.Collections.Specialized.StringCollection sc = scrp.Script(urns.ToArray());
                    foreach (string st in sc)
                    {
                        file.WriteLine(st);
                        file.WriteLine("GO");
                    }
                    
                }

                rtnVal = "OK";

            }
            catch (Exception ex)
            {
                WriteTextLog("DBscripter", "ExtractSchema", ex.ToString());
                rtnVal = "ERROR";
            }


            return rtnVal;
        }

        /// <summary>
        /// 데이터 스크립트
        /// </summary>
        /// <param name="pDB"></param>
        /// <returns></returns>
        public static string ExtractData(Database pDB)
        {
            string rtnVal = string.Empty;

            try
            {
                Scripter scrp = new Scripter(srv);

                #region{스크립팅할 데이터 형식 (스키마만/데이터만/스키마 및 데이터)}
                scrp.Options.ScriptSchema = false;           // 스키마
                scrp.Options.ScriptData = true;              // 데이터
                #endregion

                var urns = new List<Urn>();

                long totalRowCnt = 0;
                foreach (Table myTable in pDB.Tables)
                {
                    long rowCnt = myTable.RowCount;
                    totalRowCnt += rowCnt;
                }

                long curCnt = 0;
                using (var progress = new ProgressBar())
                {

                    if (!Directory.Exists(GetSystemConfigValue("backupPath")))
                    {
                        Directory.CreateDirectory(GetSystemConfigValue("backupPath"));
                    }

                    string scriptPath = string.Format("{0}\\{1}_INSERT.sql", GetSystemConfigValue("backupPath"), pDB.Name);
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(scriptPath))
                    {

                        foreach (Table myTable in pDB.Tables)
                        {
                            string tableName = myTable.Name;
                            long rowCnt = myTable.RowCount;

                            foreach (string s in scrp.EnumScript(new Urn[] { myTable.Urn }))
                            {
                                progress.Report((double)++curCnt / totalRowCnt);
                                file.WriteLine(s);
                            }
                        }
                    }
                }

                rtnVal = "OK";
            }
            catch (Exception ex)
            {
                WriteTextLog("DBscripter", "ExtractData", ex.ToString());
                rtnVal = "ERROR";
            }

            return rtnVal;
        }


        public static string GetRoutineList(string pGubun, string pDBname)
        {
            string rtnVal = string.Empty;

            try
            {

                string query = string.Format(@"SELECT 
                                                    ROUTINE_NAME 
                                                 FROM {0}.INFORMATION_SCHEMA.ROUTINES 
                                                WHERE ROUTINE_CATALOG = '{0}' AND ROUTINE_TYPE = '{1}'
                                             ORDER BY ROUTINE_NAME", pDBname, pGubun.ToUpper());

                rtnVal = GetQueryResult("master", query, true);

            }
            catch (Exception ex)
            {
                WriteTextLog("DBscripter", "GetRoutineList", ex.ToString());
            }

            return rtnVal;
        }

        /// <summary>
        /// 추출하고자 하는 DB의 모든 리스트를 가져온다.
        /// 시스템 DB(master, tempdb, model, msdb) 제외
        /// </summary>
        /// <returns></returns>
        public static XmlDocument GetDBlist()
        {
            XmlDocument rtnVal = new XmlDocument();

            try
            {
                StringBuilder query = new StringBuilder();
                query.Append("SELECT NAME FROM master.dbo.sysdatabases WHERE NAME NOT IN ('master', 'tempdb', 'model', 'msdb')");

                // targetDB값이 있으면 해당 DB만 가져온다.
                if (!string.IsNullOrEmpty(GetSystemConfigValue("targetDB")))
                {
                    query.Append(" AND NAME IN (");

                    string targetDB = GetSystemConfigValue("targetDB");
                    if (targetDB.Contains(";"))
                    {
                        string[] targetDBArr = targetDB.Split(';');
                        for (int i = 0; i < targetDBArr.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(targetDBArr[i]))
                            {
                                if (i == targetDBArr.Length - 1)
                                {
                                    query.Append(string.Format("'{0}'", targetDBArr[i]));
                                }
                                else
                                {
                                    query.Append(string.Format("'{0}',", targetDBArr[i]));
                                }
                            }
                        }
                    }
                    else
                    {
                        query.Append(string.Format("'{0}'", targetDB));
                    }

                    query.Append(")");
                }


                string dbListStr = GetQueryResult("master", query.ToString(), true);

                rtnVal.LoadXml(dbListStr);

            }
            catch (Exception ex)
            {
                WriteTextLog("DBscripter", "GetDBlist", ex.ToString());
                rtnVal.LoadXml("<DATA></DATA>");
            }

            return rtnVal;
        }
    }
}
