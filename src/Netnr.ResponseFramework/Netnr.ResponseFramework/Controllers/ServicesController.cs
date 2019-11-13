using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentScheduler;
using Microsoft.AspNetCore.Mvc;
using Netnr.Func.ViewModel;

namespace Netnr.ResponseFramework.Controllers
{
    /// <summary>
    /// 服务
    /// </summary>
    [Route("[controller]/[action]")]
    public class ServicesController : Controller
    {
        #region 定时任务

        //帮助文档：https://github.com/fluentscheduler/FluentScheduler

        /// <summary>
        /// 任务项
        /// </summary>
        public enum TaskItem
        {
            /// <summary>
            /// 重置数据库
            /// </summary>
            ResetDataBase,

            /// <summary>
            /// 清理临时目录
            /// </summary>
            ClearTemp
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="ti">任务项枚举</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResultVM ExecTask(TaskItem? ti)
        {
            var vm = new ActionResultVM();

            try
            {
                if (!ti.HasValue)
                {
                    ti = (TaskItem)Enum.Parse(typeof(TaskItem), RouteData.Values["id"]?.ToString(), true);
                }

                switch (ti)
                {
                    default:
                        vm.Set(ARTag.invalid);
                        break;

                    case TaskItem.ResetDataBase:
                        using (var tc = new ToolController())
                        {
                            vm = tc.ResetDataBaseForJson();
                        }
                        break;

                    case TaskItem.ClearTemp:
                        {
                            string directoryPath = (GlobalTo.WebRootPath + "/upload/temp/").Replace("\\", "/");

                            var listLog = new List<string>();

                            int delFileCount = 0;
                            int delFolderCount = 0;

                            //删除文件
                            var listFile = Directory.GetFiles(directoryPath).ToList();
                            foreach (var path in listFile)
                            {
                                if (!path.Contains("README"))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(path);
                                        delFileCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        listLog.Add("删除文件异常：" + ex.Message);
                                    }
                                }
                            }

                            //删除文件夹
                            var listFolder = Directory.GetDirectories(directoryPath).ToList();
                            foreach (var path in listFolder)
                            {
                                try
                                {
                                    Directory.Delete(path, true);
                                    delFolderCount++;
                                }
                                catch (Exception ex)
                                {
                                    listLog.Add("删除文件夹异常：" + ex.Message);
                                }
                            }

                            listLog.Insert(0, $"删除文件{delFileCount}个，删除{delFolderCount}个文件夹");

                            vm.data = listLog;
                            vm.Set(ARTag.success);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                vm.Set(ex);
            }

            return vm;
        }

        /// <summary>
        /// 任务组件
        /// </summary>
        public class TaskComponent
        {
            /// <summary>
            /// 任务注册
            /// </summary>
            public class Reg : Registry
            {
                /// <summary>
                /// 构造
                /// </summary>
                public Reg()
                {
                    //每间隔一天在4:4 重置数据库
                    Schedule<ResetDataBaseJob>().ToRunEvery(1).Days().At(4, 4);
                    //每间隔两天在3:3 清理临时目录
                    Schedule<ClearTempJob>().ToRunEvery(2).Days().At(3, 3);
                }
            }

            /// <summary>
            /// 重置数据库
            /// </summary>
            public class ResetDataBaseJob : IJob
            {
                void IJob.Execute()
                {
                    using var sc = new ServicesController();
                    Core.ConsoleTo.Log(sc.ExecTask(TaskItem.ResetDataBase).ToJson());
                }
            }

            /// <summary>
            /// 清理临时目录
            /// </summary>
            public class ClearTempJob : IJob
            {
                void IJob.Execute()
                {
                    using var sc = new ServicesController();
                    Core.ConsoleTo.Log(sc.ExecTask(TaskItem.ClearTemp).ToJson());
                }
            }

        }

        #endregion
    }
}