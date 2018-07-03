using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace HotelUpdateService.update.utils
{
    class CommonUtils
    {
        /// <summary>
        /// 用于获取服务的安装路径
        /// </summary>
        /// <returns></returns>
        #region public static String getServiceRunningPath()
        public static String getServiceRunningPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        #endregion

        /// <summary>
        /// 获取当前版本的version
        /// </summary>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        #region public static bool getVersionFromConfigFile(String path, out String version, out String name)
        public static bool getVersionFromConfigFile(String path, out String version, out String name)
        {
            bool isSuccess = false;
            if (String.IsNullOrEmpty(path))
            {
                Logger.info(typeof(CommonUtils), "version config xml file path is empty.");
                version = String.Empty;
                name = String.Empty;
                return isSuccess;
            }
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(path);
                XmlElement xml = document.DocumentElement;
                version = xml.SelectSingleNode("version").InnerText.ToString();
                name = xml.SelectSingleNode("name").InnerText.ToString();
                isSuccess = true;
                return isSuccess;
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            version = String.Empty;
            name = String.Empty;
            return isSuccess;
        }
        #endregion

        /// <summary>
        /// 检查文件是否存在，并返回本地文件的长度
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        #region public static long checkExistFileInLocal(String name)
        public static long checkExistFileInLocal(String name)
        {
            long size = 0L;
            String filePath = String.Format(@"{0}{1}\{2}", CommonUtils.getServiceRunningPath(), "update", name);
            if (File.Exists(filePath))
            {
                FileStream stream = new FileStream(filePath, FileMode.Open);
                size = stream.Length;
            }
            return size;
        }
        #endregion

        /// <summary>
        /// 将获取的文件流保存到本地文件中
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        #region public bool saveFile(Stream stream, String name)
        public static bool saveFile(Stream stream, String name)
        {
            //标记返回结果
            bool isSuccess = false;
            String fullName = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), name);
            FileStream fs = null;

            //创建文件，并保存文件
            try
            {
                //创建文件
                if (File.Exists(fullName))
                {
                    fs = new FileStream(fullName, FileMode.Append, FileAccess.Write, FileShare.Write);
                }
                else
                {
                    fs = new FileStream(fullName, FileMode.Create, FileAccess.Write);
                }

                byte[] buffer = new byte[5 * 1024 * 1024];
                int len = -1;
                while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, len);
                }
                stream.Flush();
                stream.Close();
                fs.Close();
                isSuccess = true;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            finally
            {
                stream.Flush();
                stream.Close();
                fs.Close();
            }
            
            return isSuccess;
        }
        #endregion

        /// <summary>
        /// 查询指定名称的进程，获取进程实例并返回进程的安装目录
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static Process getProcessInstalled(String name, out String path)
        public static Process getProcessInstalled(String name, out String path)
        {
            Process process = null;

            if (String.IsNullOrEmpty(name))
            {
                Logger.info(typeof(CommonUtils), "process name is empty.");
                path = null;
                return process;
            }
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process ps in processes)
                {
                    if (ps.ProcessName.ToLower().Contains(name.ToLower()))
                    {
                        path = Path.GetDirectoryName(ps.MainModule.FileName);
                        process = ps;
                        return ps;
                    }
                }

            }
            catch (InvalidOperationException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            path = null;
            return process;

        }
        #endregion

        /// <summary>
        /// 备份需要重新安装的文件的数据
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static bool backAppInfo(String path, String version, String name)
        public static bool backAppInfo(String path, String version, String name)
        {
            bool result = false;

            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(version) || String.IsNullOrEmpty(name))
            {
                Logger.info(typeof(CommonUtils), "app install path or name or version is empty.");
                return result;
            }

            String directory = String.Format(@"{0}back\{1}", CommonUtils.getServiceRunningPath(), String.Format(@"{0}-{1}.zip", name, version));
            try
            {
                if (File.Exists(directory))
                {
                    File.Delete(directory);
                }
                using (ZipOutputStream stream = new ZipOutputStream(File.Create(directory)))
                {
                    stream.SetLevel(6);
                    result = ZipHelper.zipDirectory(path, stream, path);
                    return result;
                }
            }catch(IOException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 解压缩文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        #region public static bool unzipFile(String filePath, String directory)
        public static bool unzipFile(String filePath, String directory)
        {
            if(String.IsNullOrEmpty(filePath) || String.IsNullOrEmpty(directory))
            {
                Logger.info(typeof(CommonUtils), "file path is empty.");
                return false;
            }
            String type = filePath.Substring(filePath.LastIndexOf("."));
            if (type.Equals(".tar"))
            {
                return ZipHelper.unTarFile(filePath, directory);
            }
            else
            {
                return ZipHelper.unZipFile(filePath);
            }
        }
        #endregion

        /// <summary>
        /// 安装软件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="directory"></param>
        #region public static void installApp(String path, String directory)
        public static void installApp(String path, String directory)
        {
            String cmd = String.Format(@"/s /S /silent /D={0} /dir={0}", directory);
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(path, cmd)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = false,
                };
                Process process = Process.Start(info);
                process.WaitForExit();
                process.Close();
                process.Dispose();

            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
        }
        #endregion

        /// <summary>
        /// 读取跟新程序的配置文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        #region public static String readConfig(String path, String key)
        public static String readConfig(String path, String key)
        {
            String value = "";

            if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(key))
            {
                Logger.info(typeof(CommonUtils), "parameters is not vaildated");
                return value;
            }

            try
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = path;
                Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                value = configuration.AppSettings.Settings[key].Value.ToString();
            }catch(ConfigurationErrorsException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            catch (Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return value;
        }
        #endregion

        /// <summary>
        /// 获取定时任务的名称
        /// </summary>
        /// <returns></returns>
        #region public static String getUpdateName()
        public static String getUpdateName()
        {
            String name = "HotelUpdateTask";
            name = CommonUtils.readConfig(String.Format(@"{0}config\update.info.config", CommonUtils.getServiceRunningPath()), "taskName");
            if (String.IsNullOrEmpty(name))
            {
                name = "HotelUpdateTask";
            }
            return name;
        }
        #endregion

        /// <summary>
        /// 描述信息
        /// </summary>
        /// <returns></returns>
        #region public static String getUpdateDescribe()
        public static String getUpdateDescribe()
        {
            String describe = CommonUtils.readConfig(String.Format(@"{0}config\update.info.config", CommonUtils.getServiceRunningPath()), "describe");
            if (String.IsNullOrEmpty(describe))
            {
                describe = "update hotel help client";
            }
            return describe;
        }
        #endregion

        /// <summary>
        /// 获取更新频率
        /// </summary>
        /// <returns></returns>
        #region public static String getFrequency()
        public static String getFrequency()
        {
            String frequency = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "frequency");
            if (String.IsNullOrEmpty(frequency))
            {
                frequency = "daily";
            }
            return frequency;
        }
        #endregion

        /// <summary>
        /// 获取任务开始时间
        /// </summary>
        /// <returns></returns>
        #region public static String getDate()
        public static String getDate()
        {
            String date = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "start");
            if (String.IsNullOrEmpty(date))
            {
                date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            return date;
        }
        #endregion

        /// <summary>
        /// 获取天数
        /// </summary>
        /// <returns></returns>
        #region public static int getDay()
        public static int getDay()
        {
            String day = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "day");
            if (String.IsNullOrEmpty(day))
            {
                day = "1";
            }
            int days = 1;
            int.TryParse(day, out days);
            return days;
        }
        #endregion

        /// <summary>
        /// 获取星期
        /// </summary>
        /// <returns></returns>
        #region public static String getWeek()
        public static String getWeek()
        {
            String week = CommonUtils.readConfig(String.Format(@"{0}config\timer.config", CommonUtils.getServiceRunningPath()), "week");
            if (String.IsNullOrEmpty(week))
            {
                week = "mon";
            }
            return week;
        }
        #endregion

        /// <summary>
        /// 计算文件的sha256的hash值
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        #region public static String getFileSHA256(String fileName)
        public static String getFileSHA256(String fileName)
        {
            String hash = String.Empty;
            try
            {
                String fullName = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), fileName);
                using (FileStream fs = new FileStream(fullName, FileMode.Open, FileAccess.Read))
                {
                    HashAlgorithm algorithm = SHA256.Create();
                    byte[] values = algorithm.ComputeHash(fs);
                    hash = byteToHeyString(values);
                    return hash;
                }
            }catch(IOException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
            return hash;
        }
        #endregion

        /// <summary>
        /// 字节数组转换为字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        #region public static String byteToHeyString(byte[] bytes)
        public static String byteToHeyString(byte[] bytes)
        {
            String value = String.Empty;
            if(bytes == null)
            {
                return value;
            }
            foreach(byte bs in bytes)
            {
                value += bs.ToString("x2");
            }
            return value;
        }
        #endregion

        /// <summary>
        /// 删除本地文件
        /// </summary>
        /// <param name="fileName"></param>
        #region public static void deleteFile(String fileName)
        public static void deleteFile(String fileName)
        {
            String full = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), fileName);
            try
            {
                if (File.Exists(full))
                {
                    File.Delete(full);
                }
            }catch(IOException ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }catch(Exception ex)
            {
                Logger.error(typeof(CommonUtils), ex);
            }
        }
        #endregion
    }
}
