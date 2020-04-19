using Ini;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ChatClientCS.Log;

namespace ChatClientCS
{
    public class HTTPClientForDB
    {
        string DBserver1;
        string DBserver2;
        string temp_server;
        string strTemp;
        log log = new log();

        public HTTPClientForDB()
        {
            ConsoleIni consoleIni = new ConsoleIni("Setting_Client");

            consoleIni.ReadIni();

            DBserver1 = consoleIni.DBserver1 + "/u2sns.aspx?";
            DBserver2 = consoleIni.DBserver2 + "/u2sns.aspx?";
        }

        public void DB_MainSub(string sender, string receiver, string msg, string filename = "")
        {
            using (var client = new MyWebClient(3))
            {
                var values = new NameValueCollection();
                values["HBTo"] = sender;
                values["HBFrom"] = receiver;
                values["HBMemo"] = msg;
                values["HBFile"] = filename;

                try
                {
                    var response = client.UploadValues(DBserver1, values);
                    var responseString = Encoding.Default.GetString(response);
                    //Console.WriteLine(responseString);
                }
                catch (Exception e)
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + "_DB Error.txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    string strData = "";
                    
                    strTemp = DBserver1.Replace("/u2sns.aspx?", "");
                    strData = "DB서버(" + strTemp + ")가 비가용 상태입니다. " + e.Message;
                    //Console.WriteLine(strData);
                    log.WriteDBFile(strFileName, strDate, strData);

                    strTemp = DBserver2.Replace("/u2sns.aspx?", "");
                    strData = "DB서버(" + strTemp + ")로 전환합니다.";
                    //Console.WriteLine(strData);
                    log.WriteDBFile(strFileName, strDate, strData);


                    try
                    {
                        var response = client.UploadValues(DBserver2, values);
                        var responseString = Encoding.Default.GetString(response);
                        //Console.WriteLine(responseString);

                        temp_server = DBserver1;
                        DBserver1 = DBserver2;
                        DBserver2 = temp_server;
                    }
                    catch (Exception ee)
                    {
                        strDate = DateTime.Now.ToString("[HH:mm:ss]");
                        
                        strTemp = DBserver2.Replace("/u2sns.aspx?", "");
                        strData = "DB서버(" + strTemp + ")가 비가용 상태입니다. " + ee.Message;
                        //Console.WriteLine(strData);
                        log.WriteDBFile(strFileName, strDate, strData);

                        strData = "소초내 모든DB 서버가 비가용 상태입니다. 메시지를 저장할 수 없습니다.";
                        //Console.WriteLine(strData);
                        log.WriteDBFile(strFileName, strDate, strData);

                    }
                }
            }
        }

        public void DB(string sender, string receiver, string msg, string filename = "")
        {
            using (var client = new MyWebClient(3))
            {
                var values = new NameValueCollection();
                values["HBTo"] = sender;
                values["HBFrom"] = receiver;
                values["HBMemo"] = msg;
                values["HBFile"] = filename;

                int e_flag = 0;

                try
                {
                    var response = client.UploadValues(DBserver1, values);
                    var responseString = Encoding.Default.GetString(response);
                    //Console.WriteLine(responseString);
                }
                catch (Exception e)
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + "_DB Error.txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    string strData = "";

                    strTemp = DBserver1.Replace("/u2sns.aspx?", "");
                    strData = "DB서버(" + strTemp + ")가 비가용 상태입니다. " + e.Message;
                    //Console.WriteLine(strData);
                    log.WriteDBFile(strFileName, strDate, strData);

                    e_flag++;
                }

                try
                {
                    var response = client.UploadValues(DBserver2, values);
                    var responseString = Encoding.Default.GetString(response);
                    //Console.WriteLine(responseString);
                }
                catch (Exception e)
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + "_DB Error.txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    string strData = "";

                    strTemp = DBserver2.Replace("/u2sns.aspx?", "");
                    strData = "DB서버(" + strTemp + ")가 비가용 상태입니다. " + e.Message;
                    //Console.WriteLine(strData);
                    log.WriteDBFile(strFileName, strDate, strData);

                    e_flag++;
                }

                if(e_flag == 2)
                {
                    string strFileName = DateTime.Today.ToString("yyyy-MM-dd") + "_DB Error.txt";
                    string strDate = DateTime.Now.ToString("[HH:mm:ss]");
                    string strData = "";

                    strData = "소초내 모든DB 서버가 비가용 상태입니다. 메시지를 저장할 수 없습니다.";
                    //Console.WriteLine(strData);
                    log.WriteDBFile(strFileName, strDate, strData);
                }
            }
        }


    }
}
