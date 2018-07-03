using HotelUpdateService.update.entity;
using HotelUpdateService.update.service;
using HotelUpdateService.update.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TaskScheduler;

namespace HotelUpdateService.update.controller
{
    /// <summary>
    /// 用于更新服务
    /// </summary>
    class UpdateController
    {
        /// <summary>
        /// 记录查询到的文件在服务器存储路径
        /// </summary>
        private String serverPath { get; set; }

        /// <summary>
        /// 记录查询到的文件的名称
        /// </summary>
        private String serverName { get; set; }

        private String installPath { get; set; }

        /// <summary>
        /// 获取更新服务类
        /// </summary>
        private static UpdateVersion update = UpdateVersion.getInstance();
        
        /// <summary>
        /// 私有构造方法，用于单实例模式的设计
        /// </summary>
        #region private UpdateController() 
        private UpdateController() { }
        #endregion

        /// <summary>
        /// 单实例模式，用于初始化实例
        /// </summary>
        /// <returns></returns>
        #region public static UpdateController getInstance
        public static UpdateController getInstance()
        {
            return new UpdateController();
        }
        #endregion

        /// <summary>
        /// 启动定时任务计划
        /// </summary>
        #region public void startTask()
        public void startTask()
        {
            //启动定时任务计划
            String name = CommonUtils.getUpdateName();
            String describe = CommonUtils.getUpdateDescribe();
            String frequency = CommonUtils.getFrequency();
            String date = CommonUtils.getDate();
            int day = CommonUtils.getDay();
            String week = CommonUtils.getWeek();
            String path = String.Format(@"{0}HotelUpdateService.exe", CommonUtils.getServiceRunningPath());
            _TASK_STATE state;
            if (!TaskSchedulerUtils.checkTask(name, out state))
            {
                for (; ; )
                {
                    bool flag = TaskSchedulerUtils.createTask(Environment.UserName, describe, name, path, frequency, date, day, week);

                    if (flag)
                    {
                        Process.GetCurrentProcess().Kill();
                        return;
                    }                 
                }
            }

            if (state != _TASK_STATE.TASK_STATE_RUNNING && state != _TASK_STATE.TASK_STATE_READY)
            {
                Logger.info(typeof(UpdateController), String.Format("task {0} 's state is {1}, waiting for start.", name, state.ToString()));
                TaskSchedulerUtils.startTask(name);
                Process.GetCurrentProcess().Kill();
            }else if(state == _TASK_STATE.TASK_STATE_READY)
            {
                Logger.info(typeof(UpdateController), String.Format("task {0} 's state is {1}", name, state));
                Process.GetCurrentProcess().Kill();
            }
        }
        #endregion

        /// <summary>
        /// 开始更新任务
        /// </summary>
        #region public void startUpdate()
        public void startUpdate()
        {
            
            /**
             * 检查版本是否更新
             * */
            
            for(int i = 0; i < 10; i++)//如果不成功，重复查询十次
            {
                //重复十次查询，没有结果，结束本次更新
                if (i >= 10)
                {
                    return;
                }

                String path, name;
                bool update = checkVersion(out path, out name);
                //查询结果
                if (!update)
                {
                    Logger.info(typeof(UpdateController), "the app has not been update in server.");
                    return;
                }
                if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name))
                {
                    Logger.info(typeof(UpdateController), "update client has find updated version in server, but some error occured unknown.");
                    continue;
                }
                else
                {
                    serverName = name;
                    serverPath = path;
                    break;
                }
            }

            /**
             * 查询到版本已经更新下载新的版本
             * **/
             //一直进行文件下载操作，直到更新文件正确下载
            while (true)
            {
                long size = checkFileExist(serverName);
                bool isDownload = downLoad(serverPath, serverName, size);
                //判断是否下载成功
                if (!isDownload)
                {
                    continue;//下载不成功继续下载
                }
                else
                {
                    //下载成功校验sha256值是否正确
                    String localHash = CommonUtils.getFileSHA256(serverName);
                    String serverHash = update.getHashFromServer(serverName);
                    if(String.IsNullOrEmpty(localHash) || String.IsNullOrEmpty(serverHash) || !localHash.Equals(serverHash))
                    {
                        CommonUtils.deleteFile(serverName);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            /**
             * 备份本地数据 十次备份不成功，退出更新
             * **/
            
             for(var i = 0; i < 10;  i++)
            {
                if(i > 9)
                {
                    Logger.info(typeof(UpdateController), "back up local app info error.");
                    return;
                }
                String appPath;
                bool isBack = update.backLocalAppInfo(out appPath);
                if (!isBack)
                {
                    continue;
                }
                else
                {
                    installPath = appPath;
                    break;
                }
            }

            /**
            * 重新安装软件
            * **/
            installApp(installPath, serverName);

        }
        #endregion private void installApp(String installPath, String serverName)

        /// <summary>
        /// 安装应用程序
        /// </summary>
        /// <param name="installPath"></param>
        /// <param name="serverName"></param>
        #region private void installApp(String installPath, String serverName)
        private void installApp(String installPath, String serverName)
        {
            String type = serverName.Substring(serverName.LastIndexOf("."));
            if (type.Equals(".exe"))
            {
                String filePath = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), serverName);
                CommonUtils.installApp(filePath, installPath);
            }
            else
            {
                //解压缩文件
                String path = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), serverName);
                bool result = CommonUtils.unzipFile(path, installPath);
            }
        }
        #endregion

        /// <summary>
        /// 用于检查版本是否需要更新
        /// </summary>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        #region private bool checkVersion(out String path, out String name)
        private bool checkVersion(out String path, out String name)
        {
            String versionFilePath = CommonUtils.getServiceRunningPath();
            String file = String.Format(@"{0}config\version.xml", versionFilePath);
            String oldVersion;
            bool flag = CommonUtils.getVersionFromConfigFile(file, out oldVersion, out String appName);
            if (!flag)
            {
                Logger.info(typeof(UpdateController), "get old app version error.");
                path = String.Empty;
                name = String.Empty;
                return false;
            }

            ResultEntity result = update.checkVersionFromServer(oldVersion);
            if(result == null)
            {
                Logger.info(typeof(UpdateController), "check app version from server failed.");
                path = String.Empty;
                name = String.Empty;
                return false;
            }

            String requestUrl = result.path;
            if (String.IsNullOrEmpty(requestUrl))
            {
                Logger.info(typeof(UpdateController), "check app version from server failed.");
                path = String.Empty;
                name = String.Empty;
                return false;
            }

            path = requestUrl.Substring(0, requestUrl.LastIndexOf("/"));
            name = requestUrl.Substring(requestUrl.LastIndexOf("/") + 1);
            return true;
        }
        #endregion

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="localSize"></param>
        /// <returns></returns>
        #region private bool downLoad(String path, String name, long localSize)
        private bool downLoad(String path, String name, long localSize)
        {
            return update.downloadFile(path, name, localSize);
        }
        #endregion

        /// <summary>
        /// 检查文件是否存在，获取初始文件大小
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        #region private long checkFileExist(String name)
        private long checkFileExist(String name)
        {
            long size = 0;

            String fullName = String.Format(@"{0}update\{1}", CommonUtils.getServiceRunningPath(), name);

            try
            {
                if (File.Exists(fullName))
                {
                    FileStream stream = new FileStream(fullName, FileMode.Open, FileAccess.Read);
                    size = stream.Length;
                    stream.Flush();
                    stream.Close();
                    return size;
                }
            }catch(IOException ex)
            {
                Logger.error(typeof(UpdateController), ex);
            }catch (Exception ex)
            {
                Logger.error(typeof(UpdateController), ex);
            }
            return size;
        }
        #endregion

        /// <summary>
        /// 从服务器获取文件的hash值
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        #region private String getFileHashFromServer(String fileName)
        private String getFileHashFromServer(String fileName)
        {
            String hash = String.Empty;
            if (String.IsNullOrEmpty(fileName))
            {
                Logger.error(typeof(UpdateController), "file name is empty when try to get hash from server.");
                return hash;
            }

            hash = update.getHashFromServer(fileName);
            return hash;
        }
        #endregion

        #region public void initDirectory()
        public void initDirectory()
        {
            String installPath = CommonUtils.getServiceRunningPath();
            String[] dir = new string[] { "update","log","back"};
            try
            {
                foreach (String path in dir)
                {
                    String full = String.Format(@"{0}{1}", installPath, path);
                    if (!Directory.Exists(full))
                    {
                        Directory.CreateDirectory(full);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.error(typeof(UpdateController), ex);
            }
        }
        #endregion
    }
}
