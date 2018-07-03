using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace HotelUpdateService.update.utils
{
    //用于记录操作日志
    class Logger
    {
        #region public static void info(Type t, String message)

        public static void info(Type t, String message)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.InfoFormat("Info: {0}", message);
        }

        #endregion

        #region public static void info(Type t, Exception ex)
        public static void info(Type t, Exception ex)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.InfoFormat("Info: {0}", ex);
        }
        #endregion

        #region public static void error(Type t, String message)
        public static void error(Type t, String message)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.ErrorFormat("Error: {0}", message);
        }
        #endregion

        #region public static void error(Type t, Exception e)
        public static void error(Type t, Exception e)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.ErrorFormat("Error: {0}", e);
        }
        #endregion

        #region public static void warn(Type t, String message)
        public static void warn(Type t, String message)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.WarnFormat("Warn: {0}", message);
        }
        #endregion

        #region public static void warn(Type t, Exception e)
        public static void warn(Type t, Exception e)
        {
            log4net.ILog logger = log4net.LogManager.GetLogger(t);
            logger.WarnFormat("Warn: {0}", e);
        }
        #endregion
    }
}
