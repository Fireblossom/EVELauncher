using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace EVELauncher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        string saveFileJson;
        saveFile userSaveFile = new saveFile();
        string temp = System.IO.Directory.GetCurrentDirectory();
        bool isLoggedIn = false;
        netConnect eveConnection = new netConnect();
        List<Account> accounts;
        public MainWindow()
        {
            InitializeComponent();
            updateServerStatus();
            updateSharedCacheLocation();
            if (!File.Exists(temp + @"\Settings.json"))
            {
                userSaveFile.path = "";
                userSaveFile.launchPara = "";
                userSaveFile.isCloseAfterLaunch = false;
                userSaveFile.isDX9Choosed = false;
                userSaveFile.Write(temp + @"\Settings.json", JsonConvert.SerializeObject(userSaveFile));
            }
            else
            {
                saveFileJson = userSaveFile.Read(temp + @"\Settings.json", Encoding.UTF8);
                userSaveFile = JsonConvert.DeserializeObject<saveFile>(saveFileJson);
                try
                {
                    gameExePath.Text = userSaveFile.path;
                    launchParameter.Text = userSaveFile.launchPara;
                    useDX9RenderMode(userSaveFile.isDX9Choosed);
                    exitAfterLaunch.IsChecked = userSaveFile.isCloseAfterLaunch;


                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + " 启动器更新导致存档文件需要更新，更新的设置项已应用默认设置。");
                }
            }

            if (File.Exists("userInfo.xml"))
            {
                /*创建文件流对象 参数1:文件的(相对)路径也可以再另一个文件夹下如:User(文件夹)/userInfo 
                                 参数2:指定操作系统打开文件的方式 
                                 参数3:指定文件的访问类型(这里为只读)  */

                //为了安全在这里创建了一个userInfo文件(用户信息),也可以命名为其他的文件格式的(可以任意)                       
                FileStream fs = new FileStream("userInfo.xml", FileMode.Open, FileAccess.Read); //使用第6个构造函数  
                XmlSerializer xs = new XmlSerializer(typeof(List<Account>));//创建一个序列化和反序列化类的对象 
                accounts = (List<Account>)xs.Deserialize(fs);//调用反序列化方法，从文件userInfo.xml中读取对象信息  


                for (int i = 0; i < accounts.Count; i++)//将集合中的用户登录ID读取到下拉框中  
                {
                    if (i == 0 && accounts[i].Password != "")  //如果第一个用户已经记住密码了。  
                    {
                        savePassword.IsChecked = true;
                        userPass.Password = accounts[i].Password;  //给密码框赋值  
                    }
                    userName.Items.Add(accounts[i].Username.ToString());
                }
                fs.Close();   //关闭文件流  
                userName.SelectedIndex = 0;   //默认下拉框选中为第一项  


            }
            else
            {
                accounts = new List<Account>();
                saveAllAccount();
            }
            userName.DropDownClosed += SelectedIndexChanged;
        }

        private void SelectedIndexChanged(object sender, EventArgs e)
        {
            if (accounts[userName.SelectedIndex].Password != "") //如果用户的密码不是为空时  
            {
                //把用户ID所对应的密码赋给密码框(这时的数据还在用户集合中)  
                userPass.Password = accounts[userName.SelectedIndex].Password.ToString();
                savePassword.IsChecked = true;
            }
            else
            {
                userPass.Password = "";  //如果用户的密码本身就是空，那只能给空值给密码框了。  
                savePassword.IsChecked = false;
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
            string launchPara = launchParameter.Text;
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
                            string FinalLaunchP =  "/noconsole " + launchPara + " /ssoToken=" + clientAccessToken + " /triPlatform=" + loginRenderMode + " " + loginGameExePath.Replace(@"\bin\exefile.exe","") + @"\launcher\appdata\EVE_Online_Launcher-2.2.896256.win32\launcher.exe";
                            Process.Start(loginGameExePath,FinalLaunchP);
                            if (loginExitAfterLaunch == true)
                            {
                                userSaveFile.isCloseAfterLaunch = true;
                                File.WriteAllText(temp + @"\Settings.json", JsonConvert.SerializeObject(userSaveFile));
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
            eveConnection.LauncherAccessToken = "";
            enableLoginControls(true);
            launcherLoginButton.Content = "登录";
            loginButton.IsEnabled = false;
            isLoggedIn = false;
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


            string loginName = userName.Text.Trim();  //将下拉框的登录名先保存在变量中  
            for (int i = 0; i < userName.Items.Count; i++)  //遍历下拉框中的所有元素  
            {
                if (userName.Items[i].ToString() == loginName)
                {
                    userName.Items.RemoveAt(i);  //如果当前登录用户在下拉列表中已经存在，则将其移除  
                    break;
                }
            }

            for (int i = 0; i < accounts.Count; i++)    //遍历用户集合中的所有元素  
            {
                if (accounts[i].Username == loginName)  //如果当前登录用户在用户集合中已经存在，则将其移除  
                {
                    accounts.RemoveAt(i);
                    break;
                }
            }

            userName.Items.Insert(0, loginName);  //每次都将最后一个登录的用户放插入到第一位  
            Account user;
            if (savePassword.IsChecked == true)    //如果用户要求要记住密码  
            {
                user = new Account(loginName, userPass.Password);  //将登录ID和密码一起插入到用户集合中  
            }
            else
                user = new Account(loginName, "");  //否则只插入一个用户名到用户集合中，密码设为空  
            accounts.Insert(0, user);   //在用户集合中插入一个用户  
            userName.SelectedIndex = 0;   //让下拉框选中集合中的第一个 
            saveAllAccount();
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
                savePassword.IsEnabled = true;
            }
            else
            {
                userName.IsEnabled = false;
                userPass.IsEnabled = false;
                launcherLoginButton.IsEnabled = false;
                savePassword.IsEnabled = false;
            }
        }

        private void launcherLogOutClick(object sender, RoutedEventArgs e)
        {
            eveConnection.LauncherAccessToken = "";
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
            userSaveFile.launchPara = launchParameter.Text;
            userSaveFile.isCloseAfterLaunch = (bool)exitAfterLaunch.IsChecked;
            userSaveFile.isDX9Choosed = (bool)radioButtonDX9.IsChecked;
            userSaveFile.path = gameExePath.Text;
            userSaveFile.Write(temp + @"\Settings.json", JsonConvert.SerializeObject(userSaveFile));
        }
        public void saveAllAccount()
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<Account>));
            FileStream fs = new FileStream("userInfo.xml", FileMode.Create, FileAccess.Write);  //创建一个文件流对象  
            //BinaryFormatter bf = new BinaryFormatter();  //创建一个序列化和反序列化对象  
            xs.Serialize(fs, accounts);   //要先将Account类先设为可以序列化(即在类的前面加[Serializable])。将用户集合信息写入到硬盘中  
            fs.Close();   //关闭文件流  
        }
    }
}