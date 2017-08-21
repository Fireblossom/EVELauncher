using System;
using System.Text;
using System.Windows;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace EVELauncher
{
    class netConnect
    {
        CookieContainer userCookieContainer = new CookieContainer();
        public string LauncherAccessToken { get; set; }
        //发送两次登陆请求
        public string getLauncherAccessToken(string userName, string userPass)
        {
            try
            {
                //第一个是POST请求，用于提交表单设置Cookie
                string loginPostString = "UserName=" + WebUtility.UrlEncode(userName) + "&Password=" + WebUtility.UrlEncode(userPass) + "&CaptchaToken=&Captcha=";
                byte[] loginPostByte = Encoding.UTF8.GetBytes(loginPostString);
                string firstLoginResponse;
                string accessToken;
                HttpWebRequest loginPostRequest = (HttpWebRequest)WebRequest.Create(new Uri("https://auth.eve-online.com.cn/Account/LogOn?ReturnUrl=%2Foauth%2Fauthorize%3Fclient_id%3DeveLauncherSerenity%26lang%3Dzh%26response_type%3Dtoken%26redirect_uri%3Dhttps%3A%2F%2Fauth.eve-online.com.cn%2Flauncher%3Fclient_id%3DeveLauncherSerenity%26scope%3DeveClientToken%2520user"));
                loginPostRequest.CookieContainer = userCookieContainer;
                loginPostRequest.Method = "POST";
                loginPostRequest.ContentType = "application/x-www-form-urlencoded";
                loginPostRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                loginPostRequest.Headers.Add("Cache-Control", "max-age=0");
                loginPostRequest.Headers.Add("Origin", "https://auth.eve-online.com.cn");
                loginPostRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/535.11 (KHTML, like Gecko) Chrome/17.0.963.15 Safari/535.11";
                loginPostRequest.Headers.Add("Refer", "https://auth.eve-online.com.cn/Account/LogOn?ReturnUrl=%2foauth%2fauthorize%3fclient_id%3deveLauncherSerenity%26lang%3dzh%26response_type%3dtoken%26redirect_uri%3dhttps%3a%2f%2fauth.eve-online.com.cn%2flauncher%3fclient_id%3deveLauncherSerenity%26scope%3deveClientToken%2520user&client_id=eveLauncherSerenity&lang=zh&response_type=token&redirect_uri=https://auth.eve-online.com.cn/launcher?client_id=eveLauncherSerenity&scope=eveClientToken%20user");
                loginPostRequest.ContentLength = loginPostByte.Length;
                Stream loginPostStream = loginPostRequest.GetRequestStream();
                loginPostStream.Write(loginPostByte, 0, loginPostByte.Length);
                loginPostStream.Close();
                HttpWebResponse loginResponse = (HttpWebResponse)loginPostRequest.GetResponse();
                StreamReader loginResponseStreamReader = new StreamReader(loginResponse.GetResponseStream(), Encoding.UTF8);
                firstLoginResponse = loginResponseStreamReader.ReadToEnd();
                loginResponseStreamReader.Close();
                loginResponse.Close();
                //第二个是Get请求，用于获取Access-Token。
                HttpWebRequest loginGetRequest = (HttpWebRequest)WebRequest.Create(new Uri("https://auth.eve-online.com.cn/oauth/authorize?client_id=eveLauncherSerenity&lang=zh&response_type=token&redirect_uri=https://auth.eve-online.com.cn/launcher?client_id=eveLauncherSerenity&scope=eveClientToken%20user"));
                loginGetRequest.CookieContainer = userCookieContainer;
                loginGetRequest.Method = "GET";
                loginGetRequest.Accept = "text/html, application/xhtml+xml, */*";
                loginGetRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko";
                loginGetRequest.AllowAutoRedirect = false;
                loginGetRequest.Referer = "https://auth.eve-online.com.cn/Account/LogOn?ReturnUrl=%2foauth%2fauthorize%3fclient_id%3deveLauncherSerenity%26lang%3dzh%26response_type%3dtoken%26redirect_uri%3dhttps%3a%2f%2fauth.eve-online.com.cn%2flauncher%3fclient_id%3deveLauncherSerenity%26scope%3deveClientToken%2520user&client_id=eveLauncherSerenity&lang=zh&response_type=token&redirect_uri=https://auth.eve-online.com.cn/launcher?client_id=eveLauncherSerenity&scope=eveClientToken%20user";
                HttpWebResponse getResponse = (HttpWebResponse)loginGetRequest.GetResponse();
                StreamReader secondLoginResponseStreamReader = new StreamReader(getResponse.GetResponseStream(), Encoding.UTF8);
                string secondLoginResponse = secondLoginResponseStreamReader.ReadToEnd();
                secondLoginResponseStreamReader.Close();
                getResponse.Close();
                Match accessTokenMatch = Regex.Match(secondLoginResponse, @"access.token.(.*).amp.token.type");
                accessToken = accessTokenMatch.Groups[1].Value;
                return accessToken;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "netErr";
            }
        }

        //获取XML
        public string getApiXML(string URL)
        {
            try
            {
                HttpWebRequest GetRequest = (HttpWebRequest)WebRequest.Create(new Uri(URL));
                GetRequest.Method = "GET";
                GetRequest.Accept = "text/html, application/xhtml+xml, */*";
                GetRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; rv:11.0) like Gecko";
                HttpWebResponse getResponse = (HttpWebResponse)GetRequest.GetResponse();
                StreamReader ResponseStreamReader = new StreamReader(getResponse.GetResponseStream(), Encoding.UTF8);
                string XMLResponse = ResponseStreamReader.ReadToEnd();
                ResponseStreamReader.Close();
                getResponse.Close();
                return XMLResponse;
            }
            catch (Exception)
            {
                return "netErr";
            }
        }

        public string getClientAccessToken(string BearerAccessToken)
        {
            try
            {
                string publicAddress = "https://auth.eve-online.com.cn//launcher/token?accesstoken=";
                HttpWebRequest clientAccessRequest = (HttpWebRequest)WebRequest.Create(new Uri(publicAddress + BearerAccessToken));
                clientAccessRequest.CookieContainer = userCookieContainer;
                clientAccessRequest.Method = "GET";
                clientAccessRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                clientAccessRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/535.11 (KHTML, like Gecko) Chrome/17.0.963.15 Safari/535.11";
                clientAccessRequest.AllowAutoRedirect = false;
                clientAccessRequest.Referer = "https://launcher.eve-online.com.cn/zh/?mac=None";
                clientAccessRequest.ConnectionGroupName = "Keep-Alive";
                clientAccessRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
                clientAccessRequest.Headers.Add("Accept-Language", "en-us,en");
                clientAccessRequest.Headers.Add("Accept-Charset", "iso-8859-1,*,utf-8");
                HttpWebResponse clientAccessResponse = (HttpWebResponse)clientAccessRequest.GetResponse();
                StreamReader responseStreamReader = new StreamReader(clientAccessResponse.GetResponseStream(), Encoding.UTF8);
                string clientAccessResponseHtml = responseStreamReader.ReadToEnd();
                responseStreamReader.Close();
                clientAccessResponse.Close();
                Match clientAccessTokenMatch = Regex.Match(clientAccessResponseHtml, @"access.token.(.*).amp.token.type");
                string clientAccessToken = clientAccessTokenMatch.Groups[1].Value;
                return clientAccessToken;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "netErr";
            }
        }

        public string getClientVersion()
        {
            try
            {
                WebClient WC = new WebClient();
                string versionString = WC.DownloadString("http://client.eve-online.com.cn/patches/premium_patchinfoSERENITY_inc.txt");
                Match clientVersionMatch = Regex.Match(versionString, @"BUILD:(\d*),");
                Match clientUpdateDateMatch = Regex.Match(versionString, @"(\d*.\d*.\d*.\d*)\n");
                return clientVersionMatch.Groups[1].Value + " " + clientUpdateDateMatch.Groups[1].Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "netErr";
            }
        }
    }
}
