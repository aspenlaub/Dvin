﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Aspenlaub.Net.GitHub.CSharp.PeghStandard.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components {
    public class ProcessStarter {
        private readonly IDictionary<Process, AutoResetEvent> vOutputWaitHandles = new Dictionary<Process, AutoResetEvent>();
        private readonly IDictionary<Process, AutoResetEvent> vErrorWaitHandles = new Dictionary<Process, AutoResetEvent>();

        public Process StartProcess(string executableFullName, string arguments, string workingFolder, IErrorsAndInfos errorsAndInfos) {
            var process = CreateProcess(executableFullName, arguments, workingFolder);
            var outputWaitHandle = new AutoResetEvent(false);
            var errorWaitHandle = new AutoResetEvent(false);
            process.OutputDataReceived += (sender, e) => {
                OnDataReceived(e, outputWaitHandle, errorsAndInfos.Infos);
            };
            process.ErrorDataReceived += (sender, e) => {
                OnDataReceived(e, errorWaitHandle, errorsAndInfos.Errors);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            vOutputWaitHandles[process] = outputWaitHandle;
            vErrorWaitHandles[process] = errorWaitHandle;
            return process;
        }

        public void WaitForExit(Process process) {
            var outputWaitHandle = vOutputWaitHandles[process];
            var errorWaitHandle = vErrorWaitHandles[process];
            process.WaitForExit();
            outputWaitHandle.WaitOne();
            errorWaitHandle.WaitOne();
        }

        private static void OnDataReceived(DataReceivedEventArgs e, EventWaitHandle waitHandle, ICollection<string> messages) {
            if (e.Data == null) {
                waitHandle.Set();
                return;
            }

            messages.Add(e.Data);
        }

        private static Process CreateProcess(string executableFullName, string arguments, string workingFolder) {
            return new Process {
                StartInfo = {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = executableFullName,
                    Arguments = arguments,
                    WorkingDirectory = workingFolder,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
        }
    }
}