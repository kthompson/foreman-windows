using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Foreman
{
    class ProcfileEntry : ProcfileBase
    {
        #region Initialization

        private readonly int _index;
        private readonly string _workingDirectory;
        private readonly string _command;
        private Process _process;

        public ProcfileEntry(int intIndex, string strName, string strCommand, string workingDirectory)
        {
            _index = intIndex;
            Name = strName;
            _workingDirectory = workingDirectory;
            _command = strCommand.Replace("$PORT", Port.ToString());
        }

        #endregion

        #region Properties

        public string Name { get; private set; }

        public int Port
        {
            get { return (5000 + (100*(_index - 1))); }
        }

        #endregion

        #region Process Control

        protected override void StartInternal()
        {
            _process = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = "cmd.exe",
                    Arguments = "/interactive /c " + _command,
                    WorkingDirectory = _workingDirectory,
                },
                EnableRaisingEvents = true
            };

            _process.OutputDataReceived += DataReceived;
            _process.ErrorDataReceived += DataReceived;
            _process.Exited += ProcessExited;

            OnStatusReceived("starting: " + _command);

            _process.Start();

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        protected override void StopInternal()
        {
            if (_process.HasExited) 
                return;

            OnStatusReceived("stopping process");
            
            ProcessUtilities.KillProcessTree(_process.Id);
        }

        #endregion

        #region Event Handlers

        private void DataReceived(object sender, DataReceivedEventArgs args)
        {
            OnProcessDataReceived(this.Name, args.Data ?? string.Empty);
        }

        private void ProcessExited(object sender, EventArgs args)
        {
            OnStatusReceived("process terminated");
        }

        #endregion
    }
}
