using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.HostConfigurators;

namespace ConsoleApp1
{
    class Program
    {
        //掛到service時的啟動路徑會是C:\Windows\system32，所以要取得exe(dll)的路徑才能正確的讀寫檔案
        public static string assemblyFolder = Path.GetDirectoryName((Assembly.GetEntryAssembly().Location));        
        public static void Main(string[] args)
        {
            try
            {
                var host = HostFactory.New(h =>
                {
                    h.Service<MyService>();
                    ConfigureService(h);
                });
                host.Run();
            }catch(Exception e)
            {
                using (StreamWriter sw = new StreamWriter(assemblyFolder + "\\Log\\Error.txt", true))
                {
                    sw.WriteLine($"[{DateTime.Now}] {e.Message}\r\n{e.StackTrace}");
                }
            }
        }

        private static void ConfigureService(HostConfigurator x)
        {
            x.StartAutomatically();

            x.SetDisplayName(GetSettings().GetValue<string>("DisplayName"));
            x.SetServiceName(GetSettings().GetValue<string>("ServiceName"));

            //x.UseNLog();   //nuget install-package Topshelf.NLog        

            x.EnableServiceRecovery(r =>
            {
                r.RestartService(0);
                r.OnCrashOnly();
                r.SetResetPeriod(1);
            });
        }

        public static IConfigurationRoot GetSettings()
        {
            return new ConfigurationBuilder()
                .SetBasePath(assemblyFolder)
                .AddJsonFile("appsettings.json", false)
                .Build();
        }
    }

    class MyService : ServiceControl
    {
        private void StartMain()
        {
            string msg;
            do
            {
                //這邊因為是透過topshelf的dll在執行，所以目錄位置就不用另外再給就會是正確的
                using (StreamWriter writer = new StreamWriter(Program.GetSettings().GetValue<string>("FileName"), true))
                {
                    msg = $"***{DateTime.Now}";
                    Console.WriteLine(msg);
                    writer.WriteLine(msg);
                }
                Thread.Sleep(Program.GetSettings().GetValue<int>("SleepMillisecond"));
            } while (true);
        }

        public bool Start(HostControl hostControl)
        {
            Console.WriteLine("Start");
            Task.Run(() => { StartMain(); });
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Console.WriteLine("Stop");
            return true;
        }
    }
}