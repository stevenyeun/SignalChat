using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ini
{
	/// <summary>
	/// Create a New INI file to store or load data
	/// </summary>
	public class IniFile
	{
		public string path;

		[DllImport("kernel32")]
		private static extern long WritePrivateProfileString(string section,string key,string val,string filePath);
		[DllImport("kernel32")]
		private static extern int GetPrivateProfileString(string section,string key,string def,StringBuilder retVal,int size,string filePath);

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
		public void IniWriteValue(string Section,string Key,string Value)
		{
			WritePrivateProfileString(Section,Key,Value,this.path);
		}
		
		/// <summary>
		/// Read Data Value From the Ini File
		/// </summary>
		/// <param name="Section"></param>
		/// <param name="Key"></param>
		/// <param name="Path"></param>
		/// <returns></returns>
		public string IniReadValue(string Section,string Key)
		{
			StringBuilder temp = new StringBuilder(255);
			int i = GetPrivateProfileString(Section,Key,"",temp,255,this.path);
			return temp.ToString();

		}
	}

    public class ConsoleIni
    {
        private IniFile ini;

        public string server1 = "";
        public string server2 = "";
        public string id = "";

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="name"> 파일이름 </param>
        /// <param name="parentFolder"> true == 상위 폴더의 ini 파일 접근, false == 현재 경로 ini 파일 접근 </param>
        public ConsoleIni(string name, bool parentFolder = false)//현재경로 + name + .ini
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
               // Console.WriteLine(filePath + " 파일이 존재합니다(MainProgramIni)");
            }
            else
            {
               // Console.WriteLine(filePath + " 파일을 찾을수없습니다(MainProgramIni)");
            }
        }
        public void WriteIni()
        {
            string section = "common";

            ini.IniWriteValue(section, "server1", this.server1);
            ini.IniWriteValue(section, "server2", this.server2);
            ini.IniWriteValue(section, "id", this.id);


        }
        public void ReadIni()
        {
            string section = "common";

            this.server1 = ini.IniReadValue(section, "server1");
            this.server2 = ini.IniReadValue(section, "server2");
            this.id = ini.IniReadValue(section, "id");
        }

    }
}
