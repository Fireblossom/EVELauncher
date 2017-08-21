using System;
using System.Text;
using System.IO;

namespace EVELauncher
{
    class saveFile
    {
        public string path { get; set; }
        public string launchPara { get; set; }
        public bool isCloseAfterLaunch { get; set; }
        public bool isDX9Choosed { get; set; }
        

        public void Write(string Path,string ContentJson)
        {
            File.WriteAllText(Path, ContentJson);
        }

        public string Read(string Path, Encoding Encoding)
        {
            string Result = File.ReadAllText(Path, Encoding);
            return Result;
        }
    }

    [Serializable]  //表示这个类可以被序列化  
    public class Account
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public Account(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public Account()
        {
            Username = "";
            Password = "";
        }


    }
}
