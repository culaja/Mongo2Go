﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Mongo2Go.Helper
{
    public static class ProcessControl
    {
        public static Process ProcessFactory(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process process = new Process { StartInfo = startInfo };
            return process;
        }

        public static ProcessOutput StartAndWaitForExit(Process process)
        {
            List<string> errorOutput = new List<string>();
            List<string> standardOutput = new List<string>();
            
            process.ErrorDataReceived  += (sender, args) => errorOutput.Add(args.Data);
            process.OutputDataReceived += (sender, args) => standardOutput.Add(args.Data);

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();

            process.CancelErrorRead();
            process.CancelOutputRead();

            return new ProcessOutput(errorOutput, standardOutput);
        }


        /// <summary>
        /// Reads from Output stream to determine if prozess is ready
        /// </summary>
        public static ProcessOutput StartAndWaitForReady(Process process, int timeoutInSeconds, string processReadyIdentifier)
        {
            if (timeoutInSeconds < 1 ||
                timeoutInSeconds > 10)
            {
                 throw new ArgumentOutOfRangeException("timeoutInSeconds", "The amount in seconds should have a value between 1 and 10.");
            }
            
            List<string> errorOutput = new List<string>();
            List<string> standardOutput = new List<string>();
            bool processReady = false;

            
            process.ErrorDataReceived  += (sender, args) => errorOutput.Add(args.Data);
            process.OutputDataReceived += (sender, args) =>
                {
                    standardOutput.Add(args.Data);

                    if (!string.IsNullOrEmpty(args.Data) &&
                        args.Data.Contains(processReadyIdentifier))
                    {
                        processReady = true;
                    }
                };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            int lastResortCounter = 0;
            int timeOut = timeoutInSeconds * 10;
            while (!processReady)
            {
                Thread.Sleep(100);
                if (++lastResortCounter > timeOut)
                {
                    // we waited X seconds.
                    // for any reason the detection did not worked, eg. the identifier changed
                    // lets assume everything is still ok
                    break;
                }
            }

            process.CancelErrorRead();
            process.CancelOutputRead();

            return new ProcessOutput(errorOutput, standardOutput);
        }
    }
}
