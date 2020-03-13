using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ini;

namespace ChatServerCS
{
    public class HTTPClientForDB
    {
        ConsoleIni consoleIni = new ConsoleIni("Setting_Server");

        string DBserver1;
        string DBserver2;
        string temp_server;

        public HTTPClientForDB()
        {
            consoleIni.ReadIni();

            DBserver1 = consoleIni.DBserver1 + "/u2sns.aspx?";
            DBserver2 = consoleIni.DBserver2 + "/u2sns.aspx?";
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

                try
                {
                    Console.WriteLine(DBserver1 + " try");
                    var response = client.UploadValues(DBserver1, values);
                    var responseString = Encoding.Default.GetString(response);
                    //Console.WriteLine(responseString);
                }
                catch (Exception e)
                {
                    Console.WriteLine(DBserver1 + " catch " + e.Message);
                    try
                    {
                        Console.WriteLine(DBserver1 + " catch try");
                        var response = client.UploadValues(DBserver2, values);
                        var responseString = Encoding.Default.GetString(response);
                        //Console.WriteLine(responseString);

                        temp_server = DBserver1;
                        DBserver1 = DBserver2;
                        DBserver2 = temp_server;
                    }
                    catch (Exception ee)
                    {
                        Console.WriteLine(DBserver1 + " catch catch " + ee.Message);
                    }
                }
            }
        }
    }
}
