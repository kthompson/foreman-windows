using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Foreman
{
    class Procfile : ProcfileBase
    {
        #region Properties

        public List<ProcfileEntry> ProcfileEntries { get; private set; }

        public string WorkingDirectory
        {
            get { return new FileInfo(this.FileName).DirectoryName; }
        }

        public string FileName { get; private set; }

        #endregion

        #region Initialization

        public Procfile(string strFilename)
        {
            this.FileName = strFilename;

            ProcfileEntries = new List<ProcfileEntry>();

            var strContents = File.ReadAllText(strFilename);

            foreach (var strLine in strContents.Split('\n'))
            {
                var arrLine = strLine.Split(new[] {':'}, 2);

                if (arrLine.Length != 2) 
                    continue;

                var objProcfileEntry = new ProcfileEntry(ProcfileEntries.Count + 1, arrLine[0].Trim(), arrLine[1].Trim(), WorkingDirectory);
                AddProcfileEntry(objProcfileEntry);
            }
        }

        #endregion

        #region Process Control

        protected override void StartInternal()
        {
            foreach (var procfileEntry in ProcfileEntries)
            {
                var objThread = new Thread(procfileEntry.Start);
                objThread.Start();
            }
        }

        protected override void StopInternal()
        {
            OnStatusReceived("stopping all processes");

            foreach (var objProcfileEntry in ProcfileEntries)
            {
                objProcfileEntry.Stop();
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            while (ProcfileEntries.Count > 0)
            {
                var entry = ProcfileEntries[0];
                ProcfileEntries.RemoveAt(0);

                entry.ProcessDataReceived -= ProcfileEntryOnProcessDataReceived;
                entry.StatusReceived -= ProcfileEntryOnStatusReceived;

                entry.Dispose();
            }
        }

        #endregion

        #region Event Helpers

        private void AddProcfileEntry(ProcfileEntry procfileEntry)
        {
            procfileEntry.ProcessDataReceived += ProcfileEntryOnProcessDataReceived;
            procfileEntry.StatusReceived += ProcfileEntryOnStatusReceived;
            ProcfileEntries.Add(procfileEntry);
        }

        private void ProcfileEntryOnStatusReceived(object sender, ProcfileEventArgs args)
        {
            OnStatusReceived(args);
        }

        private void ProcfileEntryOnProcessDataReceived(object sender, ProcfileEventArgs args)
        {
            OnProcessDataReceived(args);
        }

        #endregion
    }
}
