using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerCS
{
    public class HTTPClientForDB
    {
        string DBserver1;
        string DBserver2;
        string temp_server;
        int num_sv;
        string num;
        string strTemp;

        public HTTPClientForDB(string a, string b, int c)
        {
            DBserver1 = a + "/u2sns.aspx?";
            DBserver2 = b + "/u2sns.aspx?";

            num_sv = c;
            num = num_sv.ToString();
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
                    var response = client.UploadValues(DBserver1, values);
                    var responseString = Encoding.Default.GetString(response);
                    //Console.WriteLine(responseString);
                }
                catch (Exception e)
                {
                    strTemp = DBserver1.Replace("/u2sns.aspx?", "");
                    Console.WriteLine(num + "번 DB서버(" + strTemp + ")가 비가용 상태입니다. " + e.Message);
                    strTemp = DBserver2.Replace("/u2sns.aspx?", "");
                    Console.WriteLine(num + "번 DB서버(" + strTemp + ")로 전환합니다.");
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
                        strTemp = DBserver2.Replace("/u2sns.aspx?", "");
                        Console.WriteLine(num + "번 DB서버(" + strTemp + ")가 비가용 상태입니다. " + e.Message);
                        Console.WriteLine("모든"+num+"번 서버가 비가용 상태입니다. 메시지를 저장할 수 없습니다.");
                    }
                }
            }
        }
    }
}
