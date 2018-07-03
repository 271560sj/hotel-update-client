using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using HotelUpdateService.update.entity;
using HotelUpdateService.update.utils;
using Newtonsoft.Json.Linq;

namespace HotelUpdateService.update.service
{
    /// <summary>
    /// 检查版本信息，下载新版本软件
    /// </summary>
    class UpdateVersion
    {
        /// <summary>
        /// 操作http请求的工具类
        /// </summary>
        private static HttpUtils http = HttpUtils.getInstance();

        /// <summary>
        /// 私有构造方法
        /// </summary>
        #region private UpdateVersion()
        private UpdateVersion() { }
        #endregion

        /// <summary>
        /// 单实例模式
        /// </summary>
        /// <returns></returns>
        #region public static UpdateVersion getInstance()
        public static UpdateVersion getInstance()
        {
            return new UpdateVersion();
        }
        #endregion

        /// <summary>
        /// 检查版本服务器中是否具有新的版本
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        #region public ResultEntity checkVersionFromServer(String version)
        public ResultEntity checkVersionFromServer(String version)
        {
            ResultEntity result = null;
            String server = getServerUrl();
            if (String.IsNullOrEmpty(server) || String.IsNullOrEmpty(version))
            {
                Logger.info(typeof(UpdateVersion), "local app version or version server url is empty.");
                return result;
            }
            String url = String.Format(@"{0}/check/app/{1}", server, version);
            String request = http.get(url);
            result = JsonUtils.getResultEntity(request);
            return result;
        }

        #endregion

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        #region public bool downloadFile(String path, String name, long localSize)
        public bool downloadFile(String path, String name, long localSize)
        {
            //标志请求结果
            bool isSuccess = false;

            //判断远程文件路径和文件名称是否为空
            if(String.IsNullOrEmpty(path) || String.IsNullOrEmpty(name))
            {
                Logger.info(typeof(UpdateVersion), "request remote file path and name is empty.");
                return isSuccess;
            }

            //构建请求消息体
            PostData post = new PostData()
            {
                remote = path,
                fileName = name,
                localSize = localSize
            };

            //获取服务器的url地址
            String server = getServerUrl();
            if (String.IsNullOrEmpty(server))
            {
                Logger.info(typeof(UpdateVersion), "app version manager server is empty.");
                return isSuccess;
            }
            String url = String.Format(@"{0}/download/ftp", server);

            bool flag = http.download(url, post);
            if(!flag)
            {
                Logger.info(typeof(UpdateVersion), "download file error.");
                return isSuccess;
            }
            isSuccess = true;
            return isSuccess;
        }
        #endregion

        /// <summary>
        /// 获取版本管理服务器的地址
        /// </summary>
        /// <returns></returns>
        #region private String getServerUrl()
        private String getServerUrl()
        {
            String path = String.Format(@"{0}config\version-server.json", CommonUtils.getServiceRunningPath());
            Logger.info(typeof(UpdateVersion), String.Format("version manager server config file path is {0}", path));
            String server = JsonUtils.readJson(path);
            if (String.IsNullOrEmpty(server))
            {
                Logger.info(typeof(UpdateVersion), "version manager server info is empty.");
                return null;
            }

            JObject obj = JObject.Parse(server);

            String host = obj.SelectToken("server").ToString();
            String port = obj.SelectToken("port").ToString();

            if(String.IsNullOrEmpty(host) || String.IsNullOrEmpty(port))
            {
                Logger.info(typeof(UpdateVersion), "can not get version manager server info.");
                return null;
            }

            return String.Format(@"{0}:{1}", host, port);
        }
        #endregion

        /// <summary>
        /// 获取服务器中文件的hash值
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        #region public String getHashFromServer(String fileName)
        public String getHashFromServer(String fileName)
        {
            String hash = String.Empty;
            String url = String.Format(@"{0}/check/hash/{1}", getServerUrl(), fileName);
            String result = http.get(url);
            if (String.IsNullOrEmpty(result))
            {
                Logger.info(typeof(UpdateVersion), "get file hash value from server is failed.");
                return hash;
            }
            ResultEntity entity = JsonUtils.getResultEntity(result);
            if(entity == null)
            {
                Logger.info(typeof(UpdateVersion), "get response value error.");
                return hash;
            }
            hash = entity.hash;
            return hash;
        }
        #endregion

        /// <summary>
        /// 备份本地已经安装的软件的信息
        /// </summary>
        /// <returns></returns>
        #region public bool backLocalAppInfo(out String installPath)
        public bool backLocalAppInfo(out String installPath)
        {
            bool back = false;
            String version;
            String name;
            back = CommonUtils.getVersionFromConfigFile(String.Format(@"{0}config\version.xml", CommonUtils.getServiceRunningPath()), out version, out name);
            if (!back)
            {
                Logger.info(typeof(UpdateVersion), "get local app version error.");
                installPath = String.Empty;
                return back;
            }
            String path;
            Process process = CommonUtils.getProcessInstalled(name, out path);
            if(process == null || String.IsNullOrEmpty(path))
            {
                String runPath = CommonUtils.getServiceRunningPath();
                String defaultPath = runPath.Substring(0, runPath.LastIndexOf(@"\"));

                path = String.Format(@"{0}", defaultPath.Substring(0, defaultPath.LastIndexOf(@"\")));
            }
            else
            {
                process.Kill();
            }
            back = CommonUtils.backAppInfo(path, version, name);
            installPath = path;
            return back;
        }
        #endregion
    }
}
