﻿using System;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器基类
    /// </summary>
    public abstract class ReaderWriterBase<TSettings> : NewLife.DisposeBase, IReaderWriter where TSettings : ReaderWriterSetting, new()
    {
        #region 属性
        private String _Name;
        /// <summary>读写器名称</summary>
        public virtual String Name
        {
            get
            {
                if (String.IsNullOrEmpty(_Name))
                {
                    _Name = this.GetType().Name;
                    if (_Name.EndsWith("Reader", StringComparison.OrdinalIgnoreCase))
                        _Name = _Name.Substring(0, _Name.Length - "Reader".Length);
                    if (_Name.EndsWith("Writer", StringComparison.OrdinalIgnoreCase))
                        _Name = _Name.Substring(0, _Name.Length - "Writer".Length);
                }
                return _Name;
            }
        }

        private Stream _Stream;
        /// <summary>数据流。默认实例化一个MemoryStream，设置值时将重置Depth为1</summary>
        public virtual Stream Stream
        {
            get { return _Stream ?? (_Stream = new MemoryStream()); }
            set
            {
                if (_Stream != value)
                {
                    Depth = 1;

                    _Stream = value;
                }
            }
        }

        private TSettings _Settings;
        /// <summary>序列化设置</summary>
        public virtual TSettings Settings
        {
            get { return _Settings ?? (_Settings = new TSettings()); }
            set { _Settings = value; }
        }

        /// <summary>
        /// 序列化设置
        /// </summary>
        ReaderWriterSetting IReaderWriter.Settings { get { return Settings; } set { Settings = (TSettings)value; } }

        private Int32 _Depth;
        /// <summary>层次深度</summary>
        public Int32 Depth
        {
            get
            {
                if (_Depth < 1) _Depth = 1;
                return _Depth;
            }
            set { _Depth = value; }
        }

        //private Boolean _EncodeDateTime;
        ///// <summary>编码时间日期，使用1970-01-01以来的秒数代替</summary>
        //public Boolean EncodeDateTime
        //{
        //    get { return _EncodeDateTime; }
        //    set { _EncodeDateTime = value; }
        //}

        ///// <summary>
        ///// 编码时间日期的其实时间，固定1970-01-01
        ///// </summary>
        //public static readonly DateTime BaseDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //private Boolean _SplitGenericType;
        ///// <summary>是否拆分泛型类。拆分后，泛型类将按照泛型定义和泛型参数依次写入，同时利用对象引用，能在一定程度上减少大小</summary>
        //public Boolean SplitGenericType
        //{
        //    get { return _SplitGenericType; }
        //    set { _SplitGenericType = value; }
        //}
        #endregion

        #region 方法
        /// <summary>
        /// 重置
        /// </summary>
        public virtual void Reset()
        {
            Depth = 1;
        }
        #endregion

        #region 获取成员
        /// <summary>
        /// 获取需要序列化的成员
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        public IObjectMemberInfo[] GetMembers(Type type, Object value)
        {
            if (type == null) throw new ArgumentNullException("type");

            IObjectMemberInfo[] mis = OnGetMembers(type, value);

            if (OnGotMembers != null)
            {
                EventArgs<Type, Object, IObjectMemberInfo[]> e = new EventArgs<Type, Object, IObjectMemberInfo[]>(type, value, mis);
                OnGotMembers(this, e);
                mis = e.Arg3;
            }

            return mis;
        }

        /// <summary>
        /// 获取需要序列化的成员（属性或字段）
        /// </summary>
        /// <param name="type">指定类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        protected virtual IObjectMemberInfo[] OnGetMembers(Type type, Object value)
        {
            if (type == null) throw new ArgumentNullException("type");

            return ObjectInfo.GetMembers(type, value, false, true);
        }

        /// <summary>
        /// 获取指定类型中需要序列化的成员时触发。使用者可以修改、排序要序列化的成员。
        /// </summary>
        public event EventHandler<EventArgs<Type, Object, IObjectMemberInfo[]>> OnGotMembers;
        #endregion

        #region 对象默认值
        /// <summary>
        /// 判断一个对象的某个成员是否默认值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        internal static Boolean IsDefault(Object value, IObjectMemberInfo member)
        {
            if (value == null) return false;

            Object def = ObjectInfo.GetDefaultObject(value.GetType());

            return Object.Equals(member[value], member[def]);
        }
        #endregion

        #region 释放
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);
        }
        #endregion

        #region 写日志
        /// <summary>
        /// 调试输出
        /// </summary>
        /// <param name="action">操作</param>
        /// <param name="args">参数</param>
        [Conditional("DEBUG")]
        public void WriteLog(String action, params Object[] args)
        {
            ConsoleColor color = Console.ForegroundColor;

            // 缩进
            SetDebugIndent();

            // 红色动作
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(action);

            if (args != null && args.Length > 0)
            {
                // 白色参数
                //Console.ForegroundColor = ConsoleColor.White;
                ConsoleColor[] colors = new ConsoleColor[] { ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Yellow };

                for (int i = 0; i < args.Length; i++)
                {
                    Console.ForegroundColor = colors[i % colors.Length];
                    Console.Write(" ");
                    Console.Write(args[i]);
                }
            }

            Console.ForegroundColor = color;
            Console.WriteLine();
        }

        /// <summary>
        /// 设置调试缩进
        /// </summary>
        /// <param name="indent">缩进</param>
        [Conditional("DEBUG")]
        public static void SetDebugIndent(Int32 indent)
        {
            Console.CursorLeft = indent * 4;
        }

        /// <summary>
        /// 设置调试缩进
        /// </summary>
        [Conditional("DEBUG")]
        public void SetDebugIndent()
        {
            SetDebugIndent(Depth - 1);
        }
        #endregion
    }
}