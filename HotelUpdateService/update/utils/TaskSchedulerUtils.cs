using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskScheduler;

namespace HotelUpdateService.update.utils
{
    class TaskSchedulerUtils
    {
        /// <summary>
        /// 删除任务计划
        /// </summary>
        /// <param name="taskName">任务计划的名称</param>
        #region public static void deleteTask(String taskName)
        public static void deleteTask(String taskName)
        {
            TaskSchedulerClass task = new TaskSchedulerClass();
            task.Connect(null, null, null, null);
            ITaskFolder folder = task.GetFolder("\\");
            folder.DeleteTask(taskName, 0);
        }
        #endregion

        /// <summary>
        /// 获取所有的定时任务
        /// </summary>
        /// <returns></returns>
        #region public static IRegisteredTaskCollection GetAllTasks()
        public static IRegisteredTaskCollection GetAllTasks()
        {
            TaskSchedulerClass task = new TaskSchedulerClass();
            task.Connect(null, null, null, null);
            ITaskFolder folder = task.GetFolder("\\");
            IRegisteredTaskCollection taskList = folder.GetTasks(1);
            return taskList;
        }
        #endregion

        /// <summary>
        /// 检查定时任务是否存在
        /// </summary>
        /// <param name="taskName"></param>
        /// <returns></returns>
        #region public static bool checkTask(String taskName)
        public static bool checkTask(String taskName, out _TASK_STATE state)
        {
            var isExists = false;
            IRegisteredTaskCollection taskList = GetAllTasks();
            foreach(IRegisteredTask task in taskList)
            {
                if (task.Name.Equals(taskName))
                {
                    isExists = true;
                    state = task.State;
                    
                    return isExists;
                }
            }
            state = _TASK_STATE.TASK_STATE_UNKNOWN;
            return isExists;

        }
        #endregion

        /// <summary>
        /// 创建定时器
        /// </summary>
        /// <param name="creator">标识创建任务的用户</param>
        /// <param name="description">任务的描述信息</param>
        /// <param name="name">任务的名称</param>
        /// <param name="path">任务需要执行的exe文件</param>
        /// <param name="frequency">任务启动频率</param>
        /// <param name="date">任务开始时间</param>
        /// <param name="day">任务在那一天执行</param>
        /// <param name="week">任务在星期几执行</param>
        /// <returns></returns>
        #region public static bool createTask(String creator, String description, String name, String path, String frequency, String date, int day, String week)
        public static bool createTask(String creator, String description, String name, String path, String frequency, String date, int day, String week)
        {
            try
            {
                if (checkTask(name, out _TASK_STATE state))
                {
                    deleteTask(name);
                }

                TaskSchedulerClass task = new TaskSchedulerClass();
                task.Connect(null, null, null, null);
                ITaskFolder folder = task.GetFolder("\\");

                ITaskDefinition definition = task.NewTask(0);
                definition.RegistrationInfo.Author = creator;
                definition.RegistrationInfo.Description = description;
                definition.RegistrationInfo.Date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                ITrigger trigger = getTriigger(frequency, definition, date, day, week);
                if(trigger == null)
                {
                    Logger.info(typeof(TaskSchedulerUtils), "create trigger type error.");
                    return false;
                }

                IExecAction action = definition.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC) as IExecAction;
                action.Path = path;
                //definition.Settings.ExecutionTimeLimit = "RestartOnFailure";
                definition.Settings.DisallowStartIfOnBatteries = false;
                definition.Settings.RunOnlyIfIdle = false;
                
                IRegisteredTask iregTask = folder.RegisterTaskDefinition(name, definition, (int)_TASK_CREATION.TASK_CREATE, "", "", _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN, "");
                //IRunningTask running = iregTask.Run(null);
                return true;
            }
            catch(Exception ex)
            {
                Logger.error(typeof(TaskSchedulerUtils), ex);
            }
            return false;
        }
        #endregion

        /// <summary>
        /// 启动任务
        /// </summary>
        /// <param name="name"></param>
        #region public static void startTask(String name)
        public static void startTask(String name)
        {
            IRegisteredTaskCollection tasks = GetAllTasks();
            foreach(IRegisteredTask task in tasks)
            {
                if (task.Name.Equals(name))
                {
                    task.Run(null);
                }
            }
        }
        #endregion

        /// <summary>
        /// 获取定时器
        /// </summary>
        /// <param name="frequency">任务执行频率</param>
        /// <param name="task">任务实例</param>
        /// <param name="date">任务开始时间</param>
        /// <param name="day">任务在那一天执行</param>
        /// <param name="week">任务在星期几执行</param>
        /// <returns></returns>
        #region private static ITrigger getTriigger(String frequency,ITaskDefinition task, String date, String date, int day, String week)
        private static ITrigger getTriigger(String frequency,ITaskDefinition task, String date, int day, String week)
        {
            ITrigger trigger = null;
           
            if(String.IsNullOrEmpty(frequency))
            {
                Logger.info(typeof(TaskSchedulerUtils), "task scheduler is empty");
                return null;
            }

            try
            {
                if (frequency.Equals("weekly"))
                {
                    IWeeklyTrigger weekly = task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_WEEKLY) as IWeeklyTrigger;
                    weekly.StartBoundary = date;
                    //week 取值为mon ,tues,wed,thur,fru,sat,sun
                    if (String.IsNullOrEmpty(week) || week.ToLower().Equals("mon"))
                    {
                        weekly.DaysOfWeek = 2;
                    }else if (week.ToLower().Equals("tues"))
                    {
                        weekly.DaysOfWeek = 4;
                    }else if (week.ToLower().Equals("wed"))
                    {
                        weekly.DaysOfWeek = 8;
                    }else if (week.ToLower().Equals("thur"))
                    {
                        weekly.DaysOfWeek = 16;
                    }else if (week.ToLower().Equals("fri"))
                    {
                        weekly.DaysOfWeek = 32;
                    }else if (week.ToLower().Equals("sat"))
                    {
                        weekly.DaysOfWeek = 64;
                    }else
                    {
                        weekly.DaysOfWeek = 1;
                    }
                    trigger = weekly;
                }
                else if (frequency.Equals("monthly"))
                {
                    IMonthlyTrigger monthly = task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_MONTHLY) as IMonthlyTrigger;
                    monthly.StartBoundary = date;
                    if (day <= 0 || day > 31)
                    {
                        monthly.DaysOfMonth = 1;
                    }else
                    {  
                        monthly.DaysOfMonth = (int)Math.Pow(2.0, (day - 1) * 1.0);
                    }
                    trigger = monthly;
                }
                else
                {
                    IDailyTrigger daily = task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY) as IDailyTrigger;
                    daily.StartBoundary = date;
                    trigger = daily;
                }
            }
            catch(Exception ex)
            {
                Logger.error(typeof(TaskSchedulerUtils), ex);
            }
            return trigger;
        }
        #endregion
    }
}
