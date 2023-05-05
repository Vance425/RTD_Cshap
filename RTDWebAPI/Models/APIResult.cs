using Newtonsoft.Json.Linq;
using System;

namespace RTDWebAPI.Models
{
    public class APIResult
    {
        public bool Success { get; internal set; }
        public string State { get; internal set; }
        public int ErrorCode { get; internal set; }
        public string Message { get; internal set; }
        public object objContent { get; internal set; }
        public JObject Data { get; set; }

        /// <summary>
        /// 释放标记
        /// </summary>
        private bool disposed;
        /// <summary>
        /// 为了防止忘记显式的调用Dispose方法
        /// </summary>

        ~APIResult()
        {
            //必须为false
            Dispose(false);
        }
        /// <summary>执行与释放或重置非托管资源关联的应用程序定义的任务。</summary>
        public void Dispose()
        {
            //必须为true
            Dispose(true);
            //通知垃圾回收器不再调用终结器
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 非必需的，只是为了更符合其他语言的规范，如C++、java
        /// </summary>
        public void Close()
        {
            Dispose();
        }
        /// <summary>
        /// 非密封类可重写的Dispose方法，方便子类继承时可重写
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            //清理托管资源
            if (disposing)
            {

                if (State != null)
                {
                    State = null;
                }
                if (ErrorCode != 0)
                {
                    ErrorCode = 0;
                }
                if (Message != null)
                {
                    Message = null;
                }
                if (objContent != null)
                {
                    objContent = null;
                }
                if (Data != null)
                {
                    Data = null;
                }
            }
            //清理非托管资源


            //告诉自己已经被释放
            disposed = true;

        }
    }
}