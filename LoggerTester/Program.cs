﻿using Logging;
using System;
using System.Linq;

namespace LoggerTester
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = Logger.GetLogger().EnableParallelism()
                .AddTarget(new TargetConsole("Console"))
                .AddTarget(new TargetTrace("Trace"))
                .AddTarget(new TargetFile("File") { MaxFileSizeBytes = 100, MaxFilesLines = 20 })
                .AddTarget(new TargetEventViewer("EventViewer"));

            //logger.SetTargetLogLevel("EventViewer", LogLevel.Info);
            logger.EnableLogHistory();
            var history = logger.GetLogHistory();
            foreach (var item in history)
                Console.WriteLine(item);

            logger.Info(new Exception("msg1", new Exception("inner1", new Exception("inner2"))));
            logger.Error("Error message");

            logger.Disable();
            logger.Debug("Debug when disabled");
            logger.Enable();

            logger.Debug("Debug");
            logger.Critical("Critical");

            logger.DisableTarget("File");
            logger.Info("File target disabled");
            logger.EnableTarget("File");

            logger.ClearLogHistory();

            logger.SetTargetLogLevel("File", LogLevel.Critical);
            logger.Info("File target level wae changed to Critical");
            logger.Critical("Critical message");
            logger.SetTargetLogLevel("File", LogLevel.Info);

            var format = new TimeFormatBuilder().Day("/").Month("/").Year(" ")
                .Hour(":").Minute(":").Second(".").Millisecond(7);
            logger.SetTimeFormat(format.ToString());
            logger.Info("Time format changed");
            format = format.Clear().Day("/").Month("/").Year()
                .Add(" -- ")
                .Hour(":").Minute(":").Second(".").Millisecond(5);
            logger.SetTimeFormat(format);
            logger.Info("Time format changed again");

            history = logger.GetLogHistory();
            foreach (var item in history)
                Console.WriteLine(item);
        }

        private static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
    }
}
