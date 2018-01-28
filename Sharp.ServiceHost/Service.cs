using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Sharp.Logging;

using static System.IO.NotifyFilters;
using static System.IO.WatcherChangeTypes;

namespace Sharp.ServiceHost
{
    /// <summary>
    ///   Base class for a service program that can run in various contexts,
    ///   including a console application or a Windows service.
    /// </summary>
    [DesignerCategory("")] // disable designer
    public abstract class Service : ServiceBase
    {
        /// <summary>
        ///   Creates a new <c>Service</c> instance with the specified name.
        /// </summary>
        /// <param name="serviceName">The service name.</param>
        protected Service(string serviceName)
        {
            ServiceName = serviceName;
            CanShutdown = true;
        }

        /// <summary>
        ///   Runs the <c>Service</c> as a Windows service.
        /// </summary>
        public void RunService()
        {
            ServiceBase.Run(this);
        }

        /// <summary>
        ///   Runs the <c>Service</c> as an interactive console app.
        ///   The service will stop when the user presses a key.
        /// </summary>
        public void RunInteractive()
        {
            StartInProcess();
            WaitInteractive();
            Stop();
        }

        /// <summary>
        ///   Runs the <c>Service</c> as a non-interactive console app.
        ///   The service will stop when a stop signal is detected.  The kind
        ///   of stop signal expected depends on the environment.
        /// </summary>
        public void RunNonInteractive()
        {
            StartInProcess();
            WaitNonInteractive();
            Stop();
        }

        /// <summary>
        ///   Starts the <c>Service</c> within the current process.  Returns
        ///   when startup has completed and the service is running.  It is the
        ///   caller's responsibility to wait until it is time to stop the
        ///   <c>Service</c>, and to call <see cref="ServiceBase.Stop"/> at
        ///   that time.
        /// </summary>
        public void StartInProcess()
        {
            OnStart(null);
        }

        // NOTE: The existing Stop() method works fine!

        protected sealed override void OnStart(string[] args)
        {
            TraceOperation.Do("Service Start", () =>
            {
                StartCore();
            });
        }

        protected sealed override void OnStop()
        {
            TraceOperation.Do("Service Stop", () =>
            {
                StopCore();
            });
        }

        protected sealed override void OnShutdown()
        {
            TraceOperation.Do("Service Stop (Shutdown)", () =>
            {
                StopCore();
            });
        }

        protected virtual void StartCore() { }
        protected virtual void StopCore () { }

        // Below here, it's all about how to wait for the end.

        private static void WaitInteractive()
        {
            // Give interactive user the power to stop the service.
            Console.WriteLine("Service started.  Press any key to stop...");
            Console.ReadKey(intercept: true);
        }

        private void WaitNonInteractive()
        {
            // Try to wait for some signal to stop gracefully.
            var waited
                = TryWaitForWebJobShutdownFile()
            //  | TryWaitForSomeOtherSignal() ...and so on...
                ;

            // Failing that, just wait to be stopped however it happens.
            if (!waited)
                WaitIndefinitely();
        }

        private static bool TryWaitForWebJobShutdownFile()
        {
            var shutdownPath = Environment.GetEnvironmentVariable("WEBJOBS_SHUTDOWN_FILE");
            if (string.IsNullOrEmpty(shutdownPath))
                // Probably not in a Web Job
                return false;

            Trace.TraceInformation(
                "Running as an Azure Web Job. Will stop when the file {0} is created.",
                shutdownPath
            );

            var shutdownFile      = Path.GetFileName      (shutdownPath);
            var shutdownDirectory = Path.GetDirectoryName (shutdownPath);

            if (string.IsNullOrEmpty(shutdownDirectory))
                shutdownDirectory = ".";

            new FileSystemWatcher
            {
                Path                = shutdownDirectory,
                Filter              = shutdownFile,
                NotifyFilter        = FileName | Attributes | Size | LastWrite | CreationTime,
                EnableRaisingEvents = true
            }
            .WaitForChanged(Created | Renamed | Changed);

            return true;
        }

        public static void WaitIndefinitely()
        {
            Thread.Sleep(int.MaxValue);
        }
    }
}