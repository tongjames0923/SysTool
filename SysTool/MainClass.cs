using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
namespace SysTool
{
    namespace NET
    {
        /// <summary>
        /// 下载或上传完成时的结果
        /// </summary>
        public struct NetResult
        {
            public string message;
            public bool IsDone;
        }
        /// <summary>
        /// 进行下载或上传时待处理的数据类
        /// </summary>
        public class NetArg : EventArgs
        {
            public long HasDone, Total;
            public string host, local;
        }
        /// <summary>
        /// 网络处理
        /// </summary>
        public class NetDeal : WebClient
        {
            /// <summary>
            /// 下载时的回调
            /// </summary>
            public event EventHandler<NetArg> OnDownLoad;
            /// <summary>
            /// 上传时的回调
            /// </summary>
            public event EventHandler<NetArg> OnUpLoad;
            /// <summary>
            /// 初始化获取信息
            /// </summary>
            /// <param name="Ip">FTP服务器ip，无需Ftp://</param>
            public NetDeal(string Ip)
            {
                BaseAddress = Ip;
            }
            /// <summary>
            /// 初始化
            /// </summary>
            public NetDeal()
            {
            }
            private new string BaseAddress
            {
                get => base.BaseAddress;
                set => base.BaseAddress = value;
            }
            private string name, code;
            /// <summary>
            /// Ftp的用户名
            /// </summary>
            public string FTPName
            {
                get => name;
                set => name = value;
            }
            /// <summary>
            /// Ftp的密码
            /// </summary>
            public string FTPCode
            {
                get => code;
                set => code = value;
            }
            private long tol = -1;
            /// <summary>
            /// 含参初始化后的获取文件大小的方法
            /// </summary>
            /// <param name="fileName">需要获取大小的Ftp服务器上的文件名</param>
            /// <returns></returns>
            public long GetFileSize(string fileName)
            {
                try
                {
                    FtpWebRequest obj = WebRequest.Create("ftp://" + BaseAddress + "/" + fileName) as FtpWebRequest;
                    obj.Credentials = new NetworkCredential(FTPName, FTPCode);
                    obj.KeepAlive = true;
                    obj.UseBinary = true;
                    obj.Method = "SIZE";
                    return (obj.GetResponse() as FtpWebResponse).ContentLength;
                }
                catch
                {
                    return 0;
                }
            }
            /// <summary>
            /// 上传至服务器（需要含参初始化）
            /// </summary>
            /// <param name="ftpfilename">上传后在服务器端的文件名</param>
            /// <param name="localfile">需要上传的本地文件</param>
            /// <param name="persize">每一小块文件数据的上传字节大小（小于文件总大小）</param>
            /// <returns></returns>
            public NetResult FTPUploadFile(string ftpfilename, string localfile, int persize = 1024)
            {
                NetResult result = new NetResult();
                result.IsDone = false;
                try
                {
                    FtpWebRequest obj = (FtpWebRequest)WebRequest.Create("ftp://" + BaseAddress + "/" + ftpfilename);
                    obj.Credentials = new NetworkCredential(FTPName, FTPCode);
                    obj.KeepAlive = true;
                    obj.UseBinary = true;
                    obj.Method = WebRequestMethods.Ftp.UploadFile;
                    FileStream fileStream = System.IO.File.OpenRead(localfile);
                    Stream requestStream = obj.GetRequestStream();
                    byte[] array = new byte[fileStream.Length];
                    int time = 0;
                    int hasread = fileStream.Read(array, time * persize, persize);
                    int tol = hasread;
                    NetArg net = new NetArg();
                    net.Total = fileStream.Length;
                    net.local = localfile;
                    net.host = "ftp://" + BaseAddress + "/" + ftpfilename;
                    while (hasread > 0)
                    {
                        requestStream.Write(array, time * persize, hasread);
                        try
                        {
                            time++;
                            hasread = fileStream.Read(array, time * persize, persize);
                            tol += hasread;
                            if (OnUpLoad != null)
                            {
                                net.HasDone = tol;
                                OnUpLoad(this, net);
                            }
                        }
                        catch
                        {
                            hasread = fileStream.Read(array, time * persize, array.Length - tol);
                            tol += hasread;
                            requestStream.Write(array, time * persize, hasread);
                            if (OnUpLoad != null)
                            {
                                net.HasDone = tol;
                                OnUpLoad(this, net);
                            }
                            break;
                        }
                    }
                    fileStream.Dispose();
                    requestStream.Close();
                    obj.Abort();
                    result.IsDone = true;
                    result.message = "Done";
                }
                catch (Exception e)
                {
                    result.message = e.Message;
                }
                return result;
            }
            /// <summary>
            /// 下载至本地（需要含参初始化）
            /// </summary>
            /// <param name="ftpfilename">需要从服务器上下载的文件名</param>
            /// <param name="path">下载至本地的路径</param>
            /// <param name="fileName">下载至本地的文件名</param>
            /// <param name="mode">文件读写模式</param>
            /// <returns></returns>
            public NetResult FTPDownLoadFile(string ftpfilename, string path, string fileName, FileMode mode = FileMode.OpenOrCreate)
            {
                NetResult result = new NetResult();
                result.IsDone = false;
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }
                if (System.IO.File.Exists(path + fileName).Equals(obj: true))
                {
                    System.IO.File.Delete(path + fileName);
                }
                try
                {
                    NetArg net = new NetArg();
                    FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create("ftp://" + BaseAddress + "/" + ftpfilename);
                    ftpWebRequest.Credentials = new NetworkCredential(FTPName, FTPCode);
                    ftpWebRequest.KeepAlive = true;
                    ftpWebRequest.UseBinary = true;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                    FtpWebResponse ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
                    Stream stream = ftpWebResponse.GetResponseStream();
                    FileStream file = new FileStream(path + fileName, mode, FileAccess.ReadWrite);
                    long size = GetFileSize(ftpfilename);
                    byte[] byteBuffer = new byte[size];
                    int bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
                    tol = bytesRead;
                    while (bytesRead > 0)
                    {
                        file.Write(byteBuffer, 0, bytesRead);
                        bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
                        tol += bytesRead;
                        if (OnDownLoad != null)
                        {
                            net.host = "ftp://" + BaseAddress + "/" + ftpfilename;
                            net.local = path + fileName;
                            net.Total = size;
                            net.HasDone = tol;
                            OnDownLoad(this, net);
                        }
                        if (bytesRead <= 0)
                        {
                            file.Dispose();
                            stream.Dispose();
                            result.IsDone = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    result.message = e.Message;
                }
                return result;

            }
        }
    }
    namespace FileSystem
    {
        /// <summary>
        /// IOdeler的删除复制操作的回调参数
        /// </summary>
        public class FileArg : EventArgs
        {
            public int total = 0, hasDone = 0;
            public bool isok = false;
            public string nowPath;
            public string[] allfileList;
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="all">所有文件的路径</param>
            public FileArg(string[] all)
            {
                allfileList = all;
                total = allfileList.Length;
            }
        }
        /// <summary>
        /// 文件计数的数据储存类
        /// </summary>
        public class FileCounter
        {
            public int total;
            public string[] FilePaths;
            /// <summary>
            /// 文件计数的数据类
            /// </summary>
            /// <param name="pathes"></param>
            public FileCounter(string[] pathes)
            {
                FilePaths = pathes;
                total = FilePaths.Length;
            }
        }
        /// <summary>
        /// 返回路径，提供用户路径选择
        /// </summary>
        public class PathReturner
        {
            private static Assembly LocalApplication;
            /// <summary>
            /// 用户选择单个文件路径
            /// </summary>
            /// <param name="extension">特定的后缀  ex:".exe"</param>
            /// <returns></returns>
            public string UserSelectFile(string[] extension)
            {
                string res = "!";
                System.Windows.Forms.FileDialog file = new System.Windows.Forms.OpenFileDialog();
                file.ShowDialog();
                if (!file.FileName.Equals(""))
                {
                    FileInfo fileInfo = new FileInfo(file.FileName);
                    file.Dispose();
                    for (int i = 0; i < extension.Length; i++)
                    {
                        if (fileInfo.Extension.Equals(extension[i]))
                        {
                            res = fileInfo.FullName;
                        }
                    }
                }
                return res;
            }
            /// <summary>
            /// 返回用户选择的文件路径
            /// </summary>
            /// <returns></returns>
            public string UserSelectFile()
            {
                string res = "";
                System.Windows.Forms.FileDialog file = new System.Windows.Forms.OpenFileDialog();
                file.ShowDialog();
                if (!file.FileName.Equals(""))
                {
                    FileInfo fileInfo = new FileInfo(file.FileName);
                    file.Dispose();
                    res = fileInfo.FullName;
                }
                return res;
            }
            /// <summary>
            /// 用户选择多个文件路径
            /// </summary>
            /// <param name="extension">特定的后缀  ex:".exe"</param>
            /// <param name="filesInfo">得到的结果将传递至此</param>
            public void UserSelectFile(string[] extension, ref List<string> filesInfo)
            {
                System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog
                {
                    Multiselect = true
                };
                file.ShowDialog();
                string[] filepath = file.FileNames;
                file.Dispose();
                if (filepath.Length != 0)
                {
                    foreach (string x in filepath)
                    {
                        FileInfo fileInfo = new FileInfo(x);
                        for (int i = 0; i < extension.Length; i++)
                        {
                            if (fileInfo.Extension.Equals(extension[i]))
                            {
                                filesInfo.Add(fileInfo.FullName);
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// 用户选择多个文件路径
            /// </summary>
            /// <param name="filesInfo">得到的结果将传递至此</param>
            public void UserSelectFile(ref List<string> filesInfo)
            {
                System.Windows.Forms.OpenFileDialog file = new System.Windows.Forms.OpenFileDialog
                {
                    Multiselect = true
                };
                file.ShowDialog();
                string[] filepath = file.FileNames;
                file.Dispose();
                if (filepath.Length != 0)
                {
                    foreach (string x in filepath)
                    {
                        FileInfo fileInfo = new FileInfo(x);
                        filesInfo.Add(fileInfo.FullName);
                    }
                }
            }

            /// <summary>
            /// 用户选择路径
            /// </summary>
            /// <param name="description">选择文件夹提示</param>
            /// <returns></returns>
            public string UserSelectPath(string description)
            {
                string res = "";
                System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog()
                {
                    ShowNewFolderButton = true,
                    Description = description
                };
                folder.ShowDialog();
                if (folder.SelectedPath.Equals(null) || folder.SelectedPath.Equals("") || folder.SelectedPath.Length == 0)
                {
                }
                else
                {
                    res = folder.SelectedPath;
                }
                folder.Dispose();
                return res;
            }
            /// <summary>
            /// 注入Assembly
            /// </summary>
            /// <param name="assembly">当前程序的assembly</param>
            /// <returns></returns>
            public PathReturner(Assembly assembly)
            {
                LocalApplication = assembly;
            }
            /// <summary>
            /// 返回当前程序的所在文件夹路径
            /// </summary>
            /// <returns></returns>
            public string GetPath()
            {
                return Path.GetDirectoryName(LocalApplication.Location) + "\\";
            }
            /// <summary>
            /// 返回当前程序的文件名
            /// </summary>
            /// <returns></returns>
            public string GetFileName()
            {
                return Path.GetFileName(LocalApplication.Location);
            }
        }
        /// <summary>
        /// 处理文件
        /// </summary>
        public class IoDealer
        {
            /// <summary>
            /// 读取指定文件夹的指定后缀的文件名
            /// </summary>
            /// <param name="directory">指定文件夹路径</param>
            /// <param name="after">后缀名</param>
            /// <returns></returns>
            public static string[] HasFile(DirectoryInfo directory, string after)
            {
                List<string> res = new List<string>();
                FileInfo[] fileInfos = directory.GetFiles();
                foreach (FileInfo info in fileInfos)
                {
                    if (info.Extension.Equals(after))
                    {
                        res.Add(info.Name.TrimEnd(after.ToCharArray()));
                    }
                }

                return res.ToArray();
            }
            /// <summary>
            /// 删除文件时的事件
            /// </summary>
            public event EventHandler<FileArg> OnDelete;
            /// <summary>
            /// 复制文件时的事件
            /// </summary>
            public event EventHandler<FileArg> OnCopy;
            private readonly int[] DelFailItem = { 0, 0 };
            private readonly List<string> fail = new List<string>();
            /// <summary>
            /// 想要返回的结果数据
            /// </summary>
            public enum ReturnItem
            {
                /// <summary>
                /// 删除失败的文件
                /// </summary>
                FailToDeleteFile,
                /// <summary>
                /// 删除失败的文件夹
                /// </summary>
                FailToDeleteFolder,
                /// <summary>
                /// 删除失败的文件和文件夹
                /// </summary>
                All
            }

            private List<string> allfile = new List<string>();
            private List<string> HowManyFiles(string SourcePath)
            {

                SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
                if (Directory.Exists(SourcePath))
                {
                    string[] temp = Directory.GetFiles(SourcePath);
                    allfile.AddRange(temp);
                    foreach (string drs in Directory.GetDirectories(SourcePath))
                    {
                        DirectoryInfo drinfo = new DirectoryInfo(drs);
                        HowManyFiles(drs);
                    }
                }
                return allfile;
            }
            private void deleteEmptyFolder(string source)
            {
                if (Directory.Exists(source))
                {
                    foreach (string temp in Directory.GetDirectories(source))
                    {
                        deleteEmptyFolder(temp);
                        if (Directory.GetDirectories(temp).Length == 0 && Directory.GetFiles(temp).Length == 0)
                        {
                            try
                            {
                                Directory.Delete(temp);
                            }
                            catch { };
                        }
                    }
                }
            }
            /// <summary>
            /// 删除空文件夹
            /// </summary>
            /// <param name="source">目标文件夹</param>
            /// <param name="allclean">是否将目标文件夹一块删除</param>
            public void DeleteEmptyFolder(string source, bool allclean)
            {
                deleteEmptyFolder(source);
                if (allclean == true)
                {
                    try
                    {
                        Directory.Delete(source);
                    }
                    catch { };
                }
            }
            /// <summary>
            /// 返回路径下的全部文件数
            /// </summary>
            /// <param name="path">需要返回文件数的路径</param>
            /// <returns></returns>
            public FileCounter CountFiles(string path)
            {
                return new FileCounter(HowManyFiles(path).ToArray());
            }
            /// <summary>
            /// 删除指定文件夹中的所有内容
            /// </summary>
            /// <param name="SourcePath">需要删除的文件夹路径</param>
            /// <param name="returnItem">所需返回的值 all:删除失败的文件夹和文件数  FailToDeleteFile:删除失败的文件 FailToDeleteFolder：删除失败的文件夹</param>
            /// <returns></returns>
            public int[] DeleteFolder(string SourcePath, ReturnItem returnItem = ReturnItem.All)
            {
                DelFailItem[0] = 0;
                DelFailItem[1] = 0;
                allfile.Clear();
                int[] res = new int[1];
                List<string> path = HowManyFiles(SourcePath);
                FileArg deletearg = new FileArg(path.ToArray());
                for (int i = 0; i < path.Count; i++)
                {

                    try
                    {
                        System.IO.File.Delete(path[i]);
                        if (OnDelete != null)
                        {
                            deletearg.hasDone++;
                            deletearg.nowPath = path[i];
                            OnDelete(this, deletearg);
                        }
                    }
                    catch
                    {
                        DelFailItem[0]++;
                    }
                }
                if (OnDelete != null)
                {
                    deletearg.isok = true;
                    OnDelete(this, deletearg);
                }
                DeleteEmptyFolder(SourcePath, true);
                switch (returnItem)
                {
                    case ReturnItem.All:
                        return DelFailItem;
                    case ReturnItem.FailToDeleteFile:
                        res[0] = DelFailItem[0];
                        return res;
                    case ReturnItem.FailToDeleteFolder:
                        res[0] = DelFailItem[1];
                        return res;

                    default:
                        return DelFailItem;
                }

            }
            private bool Copy(string SourcePath, string DestinationPath, bool overwriteexisting = true, FileArg arg = null)
            {
                bool ret = false;
                try
                {
                    SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
                    DestinationPath = DestinationPath.EndsWith(@"\") ? DestinationPath : DestinationPath + @"\";
                    if (Directory.Exists(SourcePath))
                    {
                        if (Directory.Exists(DestinationPath) == false)
                        {
                            Directory.CreateDirectory(DestinationPath);
                        }
                        foreach (string fls in Directory.GetFiles(SourcePath))
                        {
                            FileInfo flinfo = new FileInfo(fls);
                            flinfo.CopyTo(DestinationPath + flinfo.Name, overwriteexisting);
                            if (OnCopy != null)
                            {
                                arg.hasDone++;
                                arg.nowPath = fls + "->" + DestinationPath + flinfo.Name;
                                OnCopy(this, arg);
                            }
                        }
                        foreach (string drs in Directory.GetDirectories(SourcePath))
                        {
                            DirectoryInfo drinfo = new DirectoryInfo(drs);
                            if (Copy(drs, DestinationPath + drinfo.Name, overwriteexisting, arg) == false)
                            {
                                fail.Add(drs);
                                ret = false;
                            }
                        }
                    }
                    ret = true;

                }
                catch
                {
                    ret = false;
                }
                return ret;
            }
            /// <summary>
            /// 复制文件夹
            /// </summary>
            /// <param name="SourcePath">需要被复制的路径</param>
            /// <param name="DestinationPath">复制到的文件夹</param>
            /// <param name="overwriteexisting">true:覆盖现有文件，false：不覆盖现有文件</param>
            /// <returns></returns>
            public List<string> CopyDirectory(string SourcePath, string DestinationPath, bool overwriteexisting = true)
            {
                fail.Clear();
                allfile.Clear();
                FileArg arg = new FileArg(HowManyFiles(SourcePath).ToArray());
                Copy(SourcePath, DestinationPath, overwriteexisting, arg);
                if (OnCopy != null)
                {
                    arg.isok = true;
                    OnCopy(this, arg);
                }
                return fail;
            }
        }
    }
    namespace WindowsSystem
    {
        /// <summary>
        /// 系统服务处理
        /// </summary>
        public class ServiceCase
        {
            private List<ServiceController> services;
            public ServiceCase()
            {
                services = new List<ServiceController>(ServiceController.GetServices());
            }
            /// <summary>
            /// 获取服务的安装路径
            /// </summary>
            /// <param name="ServiceName">服务名</param>
            /// <returns></returns>
            public string GetWindowsServiceInstallPath(string ServiceName)
            {
                try
                {
                    string key = @"SYSTEM\CurrentControlSet\Services\" + ServiceName;
                    string path = Registry.LocalMachine.OpenSubKey(key).GetValue("ImagePath").ToString();
                    //替换掉双引号
                    path = path.Replace("\"", string.Empty);
                    FileInfo fi = new FileInfo(path);
                    return fi.Directory.ToString();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            /// <summary>
            /// 检查服务是否存在
            /// </summary>
            /// <param name="serviceName">服务名</param>
            /// <returns></returns>
            public bool IsServiceExisted(string serviceName)
            {
                foreach (ServiceController sc in services)
                {
                    if (sc.ServiceName.ToLower() == serviceName.ToLower())
                    {
                        return true;
                    }
                }
                return false;
            }
            /// <summary>
            /// 获取服务列表
            /// </summary>
            /// <returns></returns>
            public ServiceController[] GetServices()
            {
                return services.ToArray();
            }
            /// <summary>
            /// 卸载服务
            /// </summary>
            /// <param name="serviceFilePath">需要卸载的服务路径</param>
            public void UninstallService(string serviceFilePath)
            {
                try
                {
                    using (AssemblyInstaller installer = new AssemblyInstaller())
                    {
                        installer.UseNewContext = true;
                        installer.Path = serviceFilePath;
                        installer.Uninstall(null);
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            /// <summary>
            /// 安装服务
            /// </summary>
            /// <param name="serviceFilePath">安装服务名</param>
            public void InstallService(string serviceFilePath)
            {
                try
                {
                    using (AssemblyInstaller installer = new AssemblyInstaller())
                    {
                        installer.UseNewContext = true;
                        installer.Path = serviceFilePath;
                        IDictionary savedState = new Hashtable();
                        installer.Install(savedState);
                        installer.Commit(savedState);
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            /// <summary>
            /// 启动服务
            /// </summary>
            /// <param name="serviceName">服务名</param>
            public void ServiceStart(string serviceName)
            {
                try
                {
                    using (ServiceController control = new ServiceController(serviceName))
                    {
                        if (control.Status == ServiceControllerStatus.Stopped)
                        {
                            control.Start();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            /// <summary>
            /// 停止服务
            /// </summary>
            /// <param name="serviceName">服务名</param>
            public void ServiceStop(string serviceName)
            {
                using (ServiceController control = new ServiceController(serviceName))
                {
                    if (control.Status == ServiceControllerStatus.Running)
                    {
                        control.Stop();
                    }
                }
            }
        }
        /// <summary>
        /// 检查管理员身份工具
        /// </summary>
        public class RootUser
        {
            /// <summary>
            /// 检查当前是否以管理员权限启动
            /// </summary>
            /// <returns></returns>
            public static bool CheckForAdm()
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /// <summary>
            /// 请求管理员权限，返回false已是管理员，返回true则已以管理员重启
            /// </summary>
            /// <param name="path">需要以管理员方式启动的程序路径</param>
            /// <returns></returns>
            public bool RestartForAdm(string path)
            {
                if (CheckForAdm() == true)
                {
                    return false;
                }
                else
                {
                    //创建启动对象
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        //设置运行文件
                        FileName = path,
                        //设置启动参数
                        Arguments = string.Join(" ", "Args"),
                        //设置启动动作,确保以管理员身份运行
                        Verb = "runas"
                    };
                    //如果不是管理员，则启动UAC
                    System.Diagnostics.Process.Start(startInfo);
                    //退出
                    return true;
                }
            }
        }
        /// <summary>
        /// 快捷方式工具
        /// </summary>
        public class ShortCutTool
        {
            /// <summary>
            /// 创建位置
            /// </summary>
            public enum To
            {
                /// <summary>
                /// 自定义
                /// </summary>
                Custom,
                /// <summary>
                /// 桌面
                /// </summary>
                DeskTop,
                /// <summary>
                /// 自启动文件夹（用于设置开机自启动）
                /// </summary>
                StartUp,
                /// <summary>
                /// 程序所在位置
                /// </summary>
                Local
            }

            /// <summary>
            /// 创建快捷方式
            /// </summary>
            /// <param name="to">选择创建的位置</param>
            /// <param name="exeFullpath">需要被创建快捷方式的完整路径（包括文件名）</param>
            /// <param name="exeDir">需要被创建快捷方式的文件的所在文件夹的路径</param>
            /// <param name="lnkName">创建快捷方式的名称</param>
            /// <param name="Description">快捷方式的描述</param>
            /// <param name="where">尽在to为Customs时生效，可创建在自定义的路径下</param>
            /// <returns></returns>
            public bool CreateShortCut(To to, string exeFullpath, string exeDir, string lnkName, string Description, string where = "C://")
            {
                bool result = false;
                string str = "";
                switch (to)
                {
                    case To.DeskTop:
                        str = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        break;
                    case To.Local:
                        str = exeDir;
                        break;
                    case To.StartUp:
                        str = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                        break;
                    case To.Custom:
                        str = where;
                        break;
                }
                try
                {
                    WshShell shell = new WshShell();
                    IWshShortcut obj = shell.CreateShortcut(str + "/" + lnkName + ".lnk") as IWshShortcut;
                    obj.TargetPath = "\"" + exeFullpath + "\"";
                    obj.Description = Description;
                    obj.WorkingDirectory = "\"" + exeDir + "\"";
                    obj.Save();
                    result = true;
                    return result;
                }
                catch
                {
                    return result;
                }
            }
            /// <summary>
            /// 创建快捷方式
            /// </summary>
            /// <param name="to">选择创建的位置</param>
            /// <param name="file">所需创建快捷方式的文件\n ex:  FileInfo files = new FileInfo({{文件路径字符串}}); </param>
            /// <param name="lnkName"></param>
            /// <param name="Description"></param>
            /// <param name="where"></param>
            /// <returns></returns>
            public bool CreateShortCut(To to, FileInfo file, string lnkName, string Description, string where = "C://")
            {

                string exeDir = file.DirectoryName;
                string exeFullpath = file.FullName;
                bool result = false;
                string str = "";
                switch (to)
                {
                    case To.DeskTop:
                        str = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        break;
                    case To.Local:
                        str = exeDir;
                        break;
                    case To.StartUp:
                        str = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                        break;
                    case To.Custom:
                        str = where;
                        break;
                }
                try
                {
                    WshShell shell = new WshShell();
                    IWshShortcut obj = shell.CreateShortcut(str + "/" + lnkName + ".lnk") as IWshShortcut;
                    obj.TargetPath = "\"" + exeFullpath + "\"";
                    obj.Description = Description;
                    obj.WorkingDirectory = "\"" + exeDir + "\"";
                    obj.Save();
                    result = true;
                    return result;
                }
                catch
                {
                    return result;
                }
            }
            /// <summary>
            /// 删除启动路径下的文件
            /// </summary>
            /// <param name="ShortCutName">文件名</param>
            /// <returns></returns>
            public bool RemoveStartUp(string ShortCutName)
            {
                try
                {
                    System.IO.File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) + "\\" + ShortCutName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}