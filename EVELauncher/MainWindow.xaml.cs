using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace EVELauncher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string saveFileJson;
        saveFile userSaveFile = new saveFile();
        string temp = System.IO.Path.GetTempPath();
        bool isLoggedIn = false;
        netConnect eveConnection = new netConnect();
        public MainWindow()
        {
            InitializeComponent();
            updateServerStatus();
            updateSharedCacheLocation();
            if (!File.Exists(temp + @"\fakeEveLauncher.json"))
            {
                userSaveFile.path = "";
                userSaveFile.userName = "";
                userSaveFile.userPass = "";
                userSaveFile.isCloseAfterLaunch = false;
                userSaveFile.isDX9Choosed = false;
                userSaveFile.Write(temp + @"\fakeEveLauncher.json", JsonConvert.SerializeObject(userSaveFile));
            }
            else
            {
                saveFileJson = userSaveFile.Read(temp + @"\fakeEveLauncher.json",Encoding.UTF8);
                userSaveFile = JsonConvert.DeserializeObject<saveFile>(saveFileJson);
                try
                {
                    gameExePath.Text = userSaveFile.path;
                    userName.Text = userSaveFile.userName;
                    userPass.Password = userSaveFile.userPass;
                    useDX9RenderMode(userSaveFile.isDX9Choosed);
                    exitAfterLaunch.IsChecked = userSaveFile.isCloseAfterLaunch;

                    if (!String.IsNullOrEmpty(userSaveFile.userName))
                    {
                        saveUserName.IsChecked = true;
                    }
                    if (!String.IsNullOrEmpty(userSaveFile.userPass))
                    {
                        savePassword.IsChecked = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + " 启动器更新导致存档文件需要更新，更新的设置项已应用默认设置。");
                }
            }
        }

        private void loginClearClick(object sender, RoutedEventArgs e)
        {
            userName.Text = "";
            userPass.Password = "";
        }

        private async void loginButtonClick(object sender, RoutedEventArgs e)
        {
            string loginGameExePath = gameExePath.Text;
            bool loginExitAfterLaunch = (bool)exitAfterLaunch.IsChecked;
            string loginRenderMode;
            if (radioButtonDX9.IsChecked == false)
            {
                loginRenderMode = "dx11";
            }
            else
            {
                loginRenderMode = "dx9";
            }
            saveAllData();
            await Task.Run(() =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => loginButton.IsEnabled = false));
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => loginButton.Content = "正在启动…"));
                    string clientAccessToken = eveConnection.getClientAccessToken(eveConnection.LauncherAccessToken);
                    if (clientAccessToken == "netErr")
                    {
                        MessageBox.Show("网络错误");
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => loginButton.IsEnabled = true));
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => loginButton.Content = "启动游戏"));
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(clientAccessToken) == false)
                        {
                            string FinalLaunchP =  "/noconsole /ssoToken=" + clientAccessToken + " /triPlatform=" + loginRenderMode + " " + loginGameExePath.Replace(@"\bin\exefile.exe","") + @"\launcher\appdata\EVE_Online_Launcher-2.2.896256.win32\launcher.exe";
                            Process.Start(loginGameExePath,FinalLaunchP);
                            if (loginExitAfterLaunch == true)
                            {
                                userSaveFile.isCloseAfterLaunch = true;
                                File.WriteAllText(temp + @"\fakeEveLauncher.json", JsonConvert.SerializeObject(userSaveFile));
                                Application.Current.Dispatcher.BeginInvoke(new Action(() => this.Close()));
                            }
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => loginButton.IsEnabled = true));
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => loginButton.Content = "启动游戏"));
                        }
                        else
                        {
                            MessageBox.Show("登录失败，网络错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                });
        }

        private void choosePathClick(object sender, RoutedEventArgs e)
        {
            string exePath;
            OpenFileDialog chooseExeFile = new OpenFileDialog();
            chooseExeFile.InitialDirectory = @"C:\";
            chooseExeFile.Filter = "CCP-EVE执行主程序|exefile.exe";
            if (chooseExeFile.ShowDialog() == true)
            {
                exePath = chooseExeFile.FileName;
                gameExePath.Text = exePath;
            }
        }

        private void serverStateRefresh(object sender, RoutedEventArgs e)
        {
            updateServerStatus();
        }

        private void aboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("EVE山寨启动器，无广告~ \n协议：MIT License \n作者：@imi415_ \n更新：https://blog.imi.moe/?p=288 \nGitHub主页：https://github.com/imi415/EVELauncher ","关于",MessageBoxButton.OK,MessageBoxImage.Asterisk);
        }

        private void checkSavePass(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("警告：密码明文存储，请勿在非本人电脑上勾选此项！用户信息存储在临时文件夹的fakeEveLauncher.json里，请注意删除！！", "安全警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
            {
                savePassword.IsChecked = false;
            }
            saveUserName.IsChecked = true;
        }

        private void updateSharedCacheLocation()
        {
            sharedCacheLocationLabel.Content = "共享缓存位置：";
            string sharedLocation = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\CCP\\EVEONLINE", "CACHEFOLDER", "Err");
            if (sharedLocation == "Err") sharedLocation = "不存在";
            sharedCacheLocationLabel.Content += sharedLocation;
        }

        /// <summary>
        /// 异步发送更新请求，委托更新状态控件
        /// </summary>
        public async void updateServerStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => refreshStatus.Content = "正在刷新，请稍等..."));
                    string clientVersion = eveConnection.getClientVersion();
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => clientVersionLabel.Content = "最新客户端版本："));
                    if (clientVersion == "netErr") clientVersion = "网络错误...";
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => clientVersionLabel.Content += clientVersion.Split(' ')[0] + ", 更新日：" + clientVersion.Split(' ')[1]));
                    string XMLString;
                    XMLString = eveConnection.getApiXML("https://api.eve-online.com.cn/server/ServerStatus.xml.aspx");
                    XmlDocument XML = new XmlDocument();
                    XML.LoadXml(XMLString);
                    string JSON = JsonConvert.SerializeXmlNode(XML);
                    JSON = JSON.Replace("@", "");
                    eveServerStatus status = JsonConvert.DeserializeObject<eveServerStatus>(JSON);
                if (status.eveApi.result.serverOpen == "True")
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => serverStatusLabel.Content = "开启"));
                    if (isLoggedIn == false)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => launcherLoginButton.IsEnabled = true));
                    }
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => serverStatusLabel.Content = "关闭"));
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => launcherLoginButton.IsEnabled = false));
                }
                Application.Current.Dispatcher.BeginInvoke(new Action(() => playerNumberLabel.Content = status.eveApi.result.onlinePlayers));
                Application.Current.Dispatcher.BeginInvoke(new Action(() => lastUpdateLabel.Content = status.eveApi.cachedUntil + " UTC+08:00"));
                Application.Current.Dispatcher.BeginInvoke(new Action(() => refreshStatus.Content = "刷新完成"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("网络连接失败，" + ex.Message);
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => refreshStatus.Content = "刷新失败"));
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => loginButton.IsEnabled = false));
                }
            });
        }

        /// <summary>
        /// 是否使用DX9兼容模式渲染
        /// </summary>
        /// <param name="RenderMode"></param>
        public void useDX9RenderMode(bool RenderMode)
        {
            if (RenderMode == true)
            {
                radioButtonDX9.IsChecked = true;
                radioButtonDX11.IsChecked = false;
            }
            else
            {
                radioButtonDX9.IsChecked = false;
                radioButtonDX11.IsChecked = true;
            }
        }

        private void radioButtonDX9Clicked(object sender, RoutedEventArgs e)
        {
            userSaveFile.isDX9Choosed = (bool)radioButtonDX9.IsChecked;
        }

        private void radioButtonDX11Clicked(object sender, RoutedEventArgs e)
        {
            userSaveFile.isDX9Choosed = (bool)radioButtonDX9.IsChecked;
        }

        private async void launcherLoginClick(object sender, RoutedEventArgs e)
        {
            launcherLoginButton.IsEnabled = false;
            launcherLoginButton.Content = "正在登录…";
            string accessToken;
            string loginUserName = userName.Text;
            string loginUserPassword = userPass.Password;
            if (String.IsNullOrEmpty(userName.Text) == false || String.IsNullOrEmpty(userPass.Password) == false)
            {
                if (saveUserName.IsChecked == true)
                {
                    userSaveFile.userName = userName.Text;
                    if (savePassword.IsChecked == true)
                    {
                        userSaveFile.userPass = userPass.Password;
                    }
					else
					{
						userSaveFile.userPass = "";
					}
                }
                if (String.IsNullOrEmpty(gameExePath.Text) == false)
                {
                    userSaveFile.path = gameExePath.Text;
                    saveAllData();
                    
                    await Task.Run(() =>
                    {
                        accessToken = eveConnection.getLauncherAccessToken(loginUserName, loginUserPassword);
                        if (accessToken == "netErr")
                        {
                            MessageBox.Show("登录失败，网络错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(accessToken))
                            {
                                MessageBox.Show("登陆失败，用户名或密码错误。", "错误");

                                Application.Current.Dispatcher.BeginInvoke(new Action(() => enableLoginControls(true)));
                                Application.Current.Dispatcher.BeginInvoke(new Action(() => launcherLoginButton.Content = "登录"));
                            }
                            else
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() => launcherLoginButton.Content = "已登录"));
                                Application.Current.Dispatcher.BeginInvoke(new Action(() => enableLoginControls(false)));
                                eveConnection.LauncherAccessToken = accessToken;
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>loginButton.IsEnabled = true));
                                isLoggedIn = true;
                            }
                        }
                    });
                }
                else
                {
                    MessageBox.Show("请指定主执行程序路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    enableLoginControls(true);
                    launcherLoginButton.Content = "登录";
                }
            }
            else
            {
                MessageBox.Show("请填写用户名和密码", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                enableLoginControls(true);
                launcherLoginButton.Content = "登录";
            }
        }

        /// <summary>
        /// 更改控件可用状态
        /// </summary>
        /// <param name="isEnabled"></param>
        public void enableLoginControls(bool isEnabled)
        {
            if (isEnabled == true)
            {
                userName.IsEnabled = true;
                userPass.IsEnabled = true;
                launcherLoginButton.IsEnabled = true;
                saveUserName.IsEnabled = true;
                savePassword.IsEnabled = true;
            }
            else
            {
                userName.IsEnabled = false;
                userPass.IsEnabled = false;
                launcherLoginButton.IsEnabled = false;
                saveUserName.IsEnabled = false;
                savePassword.IsEnabled = false;
            }
        }

        private void launcherLogOutClick(object sender, RoutedEventArgs e)
        {
            eveConnection.LauncherAccessToken = "";
			userName.Text="";
			userPass.Password="";
            enableLoginControls(true);
            launcherLoginButton.Content = "登录";
            loginButton.IsEnabled = false;
            isLoggedIn = false;
        }

        /// <summary>
        /// 保存全部数据并写入到文件
        /// </summary>
        public void saveAllData()
        {
            userSaveFile.userName = userName.Text;
            userSaveFile.userPass = userPass.Password;
            userSaveFile.isCloseAfterLaunch = (bool)exitAfterLaunch.IsChecked;
            userSaveFile.isDX9Choosed = (bool)radioButtonDX9.IsChecked;
            userSaveFile.path = gameExePath.Text;
            userSaveFile.Write(temp + @"\fakeEveLauncher.json", JsonConvert.SerializeObject(userSaveFile));
        }
    }
}
