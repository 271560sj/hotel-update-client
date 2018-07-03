using HotelUpdateService.update.entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HotelUpdateService.update.utils
{
    /// <summary>
    /// 操作json的工具类
    /// </summary>
    class JsonUtils
    {
        /// <summary>
        /// 从json配置文件中读取信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        #region public static String readJson(String path)
        public static String readJson(String path)
        {
            String json = null;

            //文件路径为空，返回空值
            if (String.IsNullOrEmpty(path)){
                Logger.info(typeof(JsonUtils), "json file path is empty.");
                return json;
            }
            //读取文件
            try
            {
                using (StreamReader reader = File.OpenText(path))
                {
                    using(JsonTextReader text = new JsonTextReader(reader))
                    {
                        JObject jObject = JToken.ReadFrom(text) as JObject;
                        json = jObject.ToString();
                        return json;
                    }
                }
            }
            catch(NotSupportedException ex)
            {
                Logger.error(typeof(JsonUtils), ex);
            }
            catch (Exception e)
            {
                Logger.error(typeof(JsonUtils), e);
            }
            return json;
        }
        #endregion

        /// <summary>
        /// 解析string字符串到ResultEntity实体类
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        #region public static ResultEntity getResultEntity(String str)
        public static ResultEntity getResultEntity(String str)
        {
            ResultEntity entity = null;
            if (String.IsNullOrEmpty(str))
            {
                Logger.info(typeof(JsonUtils), "content of string is empty.");
                return entity;
            }
            try
            {
                JObject jo = JObject.Parse(str);
                entity = new ResultEntity();
                if (jo.SelectToken("message") != null)
                {
                    entity.message = jo.SelectToken("message").ToString();
                }
                
                int result = 0;
                int.TryParse(jo.SelectToken("code").ToString(), out result);
                entity.code = result;

                if (jo.SelectToken("object") != null)
                {
                    JObject path = JObject.Parse(jo.SelectToken("object").ToString());
                    if (path.SelectToken("path") != null)
                    {
                        entity.path = path.SelectToken("path").ToString();
                    }
                    if(path.SelectToken("hash") != null)
                    {
                        entity.hash = path.SelectToken("hash").ToString();
                    }
                }
                return entity;
            }
            catch (Exception e)
            {
                Logger.error(typeof(HttpUtils), e);
            }

            return entity;
        } 
        #endregion
    }
}
