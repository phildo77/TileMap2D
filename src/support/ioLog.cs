using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace ioSoftSmiths.ioLog
{
    public enum MsgPriLvl : byte
    {
        LOW = 1,
        MED = 2,
        HIGH = 4
    }

    public enum LogVerbosity : byte
    {
        LOW = 4,
        MED = 6,
        HIGH = 7
    }

    public enum LogStyle : byte
    {
        DEBUG,
        MESSAGE_ONLY
    }



    public static class Msg
    {
        public const String KEY_DEBUG_LOG = "LOG_DEBUG";
        private const String TAG_DEBUG = "ioLog";



        // Member Fields and Properties -----------------------------------------------------
        private static Dictionary<string, MsgLog> m_Logs = new Dictionary<string, MsgLog>();


        public static LogVerbosity DebugVerbosity = LogVerbosity.HIGH;

        private readonly static Action<string, string> DebugVSConsole =
            new Action<string, string>((_group, _msg) => Debug.WriteLine(_group + " : " + _msg));
        public static Action<string, string> DebugAction = DebugVSConsole;


        // Member Functions -------------------------------------------------------------------------------------
        public static void CreateLog(string _logId, LogVerbosity _logVerbosity, LogStyle _style, Action<string> _outputAction)
        {
            m_Logs.Add(_logId, new MsgLog(_style, _logVerbosity, _outputAction)); ;
        }

        public static void LogDebug(string _group, string _message, MsgPriLvl _verbosityLevel)
        {
            if (((byte)_verbosityLevel & (byte)DebugVerbosity) != 0)
                DebugAction(_group, _message);
        }

        public static void SetDebugAction(Action<string, string> _action)
        {
            DebugAction = _action;
        }

        public static void Log(string _logId, string _group, string _message, MsgPriLvl _verbosityLevel)
        {
            m_Logs[_logId].Log(_group + " : " + _message, _verbosityLevel);
        }

        public static string TimeStamp()
        {
            return DateTime.Now.ToString("u");
        }

        private struct Message
        {
            public MsgPriLvl Verbosity;
            public string Msg;

            public Message(string _message, MsgPriLvl _verbLevel)
            {
                Msg = _message;
                Verbosity = _verbLevel;
            }
        }

        private class MsgLog
        {
            //private Queue<Message> m_Messages = new Queue<Message>();
            public LogStyle Style { get; set; }
            private byte m_VerbosityMask;
            private Action<string> m_OutputAction;

            public MsgLog(LogStyle _style, LogVerbosity _verbosity, Action<string> _outputAction)
            {
                Style = _style;
                m_VerbosityMask = (byte)_verbosity;
                m_OutputAction = _outputAction;
            }



            public LogVerbosity Verbosity
            {
                get { return (LogVerbosity)m_VerbosityMask; }
                set { m_VerbosityMask = (byte)value; }
            }

            internal void Log(string _msg, MsgPriLvl _verbLevel)
            {
                if (((byte)_verbLevel & m_VerbosityMask) != 0)
                    m_OutputAction(_msg);
            }

        }

        public static void SetVerbosity(string _logID, LogVerbosity _verbosity)
        {
            if (_logID.Equals(KEY_DEBUG_LOG))
                DebugVerbosity = _verbosity;
            else
                m_Logs[_logID].Verbosity = _verbosity;
        }

        public static void DeleteLog(string _logID)
        {
            m_Logs.Remove(_logID);
        }
    }



}
