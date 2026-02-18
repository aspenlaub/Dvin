using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Dvin.Components;

public class ProcessStarter {
    private readonly IDictionary<Process, AutoResetEvent> _OutputWaitHandles = new Dictionary<Process, AutoResetEvent>();
    private readonly IDictionary<Process, AutoResetEvent> _ErrorWaitHandles = new Dictionary<Process, AutoResetEvent>();

    public Process StartProcess(string executableFullName, string arguments, string workingFolder, IErrorsAndInfos errorsAndInfos) {
        if (executableFullName.Contains(@"\") && !File.Exists(executableFullName)) {
            errorsAndInfos.Errors.Add($"Executable \"{executableFullName}\" not found");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(workingFolder) && !Directory.Exists(workingFolder)) {
            errorsAndInfos.Errors.Add($"Folder \"{workingFolder}\" not found");
            return null;
        }

        Process process = CreateProcess(executableFullName, arguments, workingFolder);
        var outputWaitHandle = new AutoResetEvent(false);
        var errorWaitHandle = new AutoResetEvent(false);
        process.OutputDataReceived += (_, e) => {
            OnDataReceived(e, outputWaitHandle, errorsAndInfos.Infos);
        };
        process.ErrorDataReceived += (_, e) => {
            OnDataReceived(e, errorWaitHandle, errorsAndInfos.Errors);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _OutputWaitHandles[process] = outputWaitHandle;
        _ErrorWaitHandles[process] = errorWaitHandle;
        return process;
    }

    public void WaitForExit(Process process) {
        AutoResetEvent outputWaitHandle = _OutputWaitHandles[process];
        AutoResetEvent errorWaitHandle = _ErrorWaitHandles[process];
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
        return new() {
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