﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FollowerMazeServer
{
    /// <summary>
    ///  Utility methods
    /// </summary>
    [ExcludeFromCodeCoverage]
    class Utils
    {
#if DEBUG
        static List<string> Buffer = new List<string>();
        const string Path = "Log.txt";
        static bool First = true;
#else
#endif

        /// <summary>
        /// Write log to file, with buffer
        /// </summary>
        /// <param name="Message">string to write</param>
        public static void Log(string Message)
        {
#if DEBUG
            if (First)
            {
                First = false;
                System.IO.File.Delete(Path);
            }
            lock (Buffer)
            {
                Buffer.Add(Message.TrimEnd());
            }
            if (Buffer.Count > 1000)
            {
                FlushLog();
            }

#else
#endif
        }

        /// <summary>
        /// Write log to file
        /// </summary>
        /// <param name="Array">array to write</param>
        public static void Log(byte[] Array)
        {
#if DEBUG
            Buffer.Add(System.Text.Encoding.UTF8.GetString(Array));
#else
#endif
        }


        /// <summary>
        /// Write all log entries in buffer to file
        /// </summary>
        public static void FlushLog()
        {
#if DEBUG
            lock (Buffer)
            {
                System.IO.File.AppendAllLines(Path, Buffer);
                Buffer.Clear();
            }
#else
#endif
        }

        /// <summary>
        /// Write a status message on screen, can be updated inline
        /// </summary>
        /// <param name="Message"></param>
        public static void Status(string Message)
        {
            Console.Write("\r" + DateTime.Now.ToLongTimeString() + " " + Message.PadRight(100));
        }

        public static void StatusLine(string Message)
        {
            Status(Message);
            Console.WriteLine();
        }
    }
}
