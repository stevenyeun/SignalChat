using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace QlightLibrary
{
    public class LampController
    {
        private static LampIni ini = new LampIni("LampInfo");


        [DllImport("Qtvc_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern bool Tcp_Qu_RW(int iPort, byte* pbIp, byte* pbData);

        private static bool GetIpPortFromIni(out IPAddress ip, out int port, out bool sound)
        {
            ini.ReadIni();
            ip = null;
            port = ini.lampPort;
            sound = ini.sound;

            try
            {
                ip = IPAddress.Parse(ini.lampIP);           
            }
            catch(Exception e)
            {
                return false;
            }
            return true;
        }
        public static unsafe void LampOn()
        {
            //IP 포트를 읽기 실패하면 함수 리턴
            bool ret = GetIpPortFromIni(out IPAddress ip, out int port, out bool sound);
            if (ret == false)
                return;

            const byte D_not = 100;            // Don't care  // Do not change before state
            const byte C_lampoff = 0;
            const byte C_lampon = 1;
            const byte C_lampblink = 2; 


            bool b_chk = false;
            int iPort, i = 0;
            string m_str;
            byte* c_pIdata = stackalloc byte[10];
            byte* c_pIpadd = stackalloc byte[6];

            c_pIdata[0] = 1;		// 1-write  0-read
            c_pIdata[1] = 0;
            //sound 25ea model group select   0-4:
            //c_pIdata[1]  = 3;	


            c_pIdata[2] = C_lampon;         // lamp1 RED
            c_pIdata[3] = C_lampblink;		// lamp2 Yellow
            c_pIdata[4] = D_not;		    // lamp3 Green
            c_pIdata[5] = C_lampon;			// lamp4 Blue
            c_pIdata[6] = C_lampblink;		// lamp4 White
            if(sound)
                c_pIdata[7] = 3;				// so
            else
                c_pIdata[7] = 0;                // so

#if false
            c_pIpadd[0] = 192;
            c_pIpadd[1] = 168;
            c_pIpadd[2] = 0;
            c_pIpadd[3] = 223;

            iPort = 20000;
#else
            c_pIpadd[0] = ip.GetAddressBytes()[0];
            c_pIpadd[1] = ip.GetAddressBytes()[1];
            c_pIpadd[2] = ip.GetAddressBytes()[2];
            c_pIpadd[3] = ip.GetAddressBytes()[3];

            iPort = port;
#endif
            b_chk = Tcp_Qu_RW(iPort, c_pIpadd, c_pIdata);


            m_str = " ";
            if (b_chk)
            {
                m_str = "  [Success send] ";

            }
            else m_str = "  [Send  Error] ";
        }
        public static unsafe void LampOff()
        {
            //IP 포트를 읽기 실패하면 함수 리턴
            bool ret = GetIpPortFromIni(out IPAddress ip, out int port, out bool sound);
            if (ret == false)
                return;

            const byte D_not = 100;            // Don't care  // Do not change before state
            const byte C_lampoff = 0;
            const byte C_lampon = 1;
            const byte C_lampblink = 2;


            bool b_chk = false;
            int iPort, i = 0;
            string m_str;
            byte* c_pIdata = stackalloc byte[10];
            byte* c_pIpadd = stackalloc byte[6];

            c_pIdata[0] = 1;		// 1-write  0-read
            c_pIdata[1] = 0;
            //sound 25ea model group select   0-4:
            //c_pIdata[1]  = 3;	


            c_pIdata[2] = C_lampoff;        // lamp1 RED
            c_pIdata[3] = C_lampoff;		// lamp2 Yellow
            c_pIdata[4] = C_lampoff;		// lamp3 Green
            c_pIdata[5] = C_lampoff;		// lamp4 Blue
            c_pIdata[6] = C_lampoff;		// lamp4 White
            c_pIdata[7] = 0;				// so

#if false
            c_pIpadd[0] = 192;
            c_pIpadd[1] = 168;
            c_pIpadd[2] = 0;
            c_pIpadd[3] = 223;

            iPort = 20000;
#else
            c_pIpadd[0] = ip.GetAddressBytes()[0];
            c_pIpadd[1] = ip.GetAddressBytes()[1];
            c_pIpadd[2] = ip.GetAddressBytes()[2];
            c_pIpadd[3] = ip.GetAddressBytes()[3];

            iPort = port;
#endif

            b_chk = Tcp_Qu_RW(iPort, c_pIpadd, c_pIdata);


            m_str = " ";
            if (b_chk)
            {
                m_str = "  [Success send] ";

            }
            else m_str = "  [Send  Error] ";

         
        }
    }
    public class IniFile
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <param name="INIPath"></param>
        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <param name="Section"></param>
        /// Section name
        /// <param name="Key"></param>
        /// Key Name
        /// <param name="Value"></param>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, this.path);
            return temp.ToString();

        }
    }

    public class LampIni
    {
        private IniFile ini;

        public string lampIP = "";
        public int lampPort = 0;
        public bool sound = false;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="name"> 파일이름 </param>
        /// <param name="parentFolder"> true == 상위 폴더의 ini 파일 접근, false == 현재 경로 ini 파일 접근 </param>
        public LampIni(string name, bool parentFolder = false)//현재경로 + name + .ini
        {
            string filePath = "";
            if (parentFolder)
            {
                int nIndex = AppDomain.CurrentDomain.BaseDirectory.LastIndexOf('\\');
                string newPath = AppDomain.CurrentDomain.BaseDirectory.Substring(0, nIndex);
                nIndex = newPath.LastIndexOf('\\');
                newPath = newPath.Substring(0, nIndex + 1);

                filePath = newPath + name + ".ini";
            }
            else
            {
                filePath = AppDomain.CurrentDomain.BaseDirectory + name + ".ini";
            }

            ini = new IniFile(filePath);
            //파일이 존재하는지 확인
            // See if this file exists in the same directory.
            if (File.Exists(filePath) == true)//파일이 존재
            {
                Console.WriteLine(filePath + " 파일이 존재합니다(LampIni)");
            }
            else
            {
                Console.WriteLine(filePath + " 파일을 찾을수없습니다(LampIni)");

                WriteIni();
            }
        }
        public void WriteIni()
        {
            string section = "common";

            ini.IniWriteValue(section, "lampIP", this.lampIP);
            ini.IniWriteValue(section, "lampPort", this.lampPort.ToString());
            ini.IniWriteValue(section, "sound", this.sound.ToString());
        }
        public void ReadIni()
        {
            string section = "common";

            this.lampIP = ini.IniReadValue(section, "lampIP");
            string lampPort= ini.IniReadValue(section, "lampPort");            
            string sound = ini.IniReadValue(section, "sound");
            try
            {
                this.lampPort = int.Parse(lampPort);
                this.sound = bool.Parse(sound);
            }
            catch(Exception e)
            {

            }

        }

    }
}
