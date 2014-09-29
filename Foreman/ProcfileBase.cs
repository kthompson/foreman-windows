using System;

namespace Foreman
{
    abstract class ProcfileBase : IDisposable
    {
        #region Variables

        private bool _started;

        #endregion

        #region Events

        public virtual event EventHandler<ProcfileEventArgs> ProcessDataReceived;

        public virtual event EventHandler<ProcfileEventArgs> StatusReceived;

        protected virtual void OnProcessDataReceived(string header, string text)
        {
            var args = new ProcfileEventArgs(header, text, DateTime.Now);

            OnProcessDataReceived(args);
        }

        protected void OnProcessDataReceived(ProcfileEventArgs args)
        {
            var handler = this.ProcessDataReceived;
            if (handler == null)
                return;

            handler(this, args);
        }

        protected virtual void OnStatusReceived(string text)
        {
            OnStatusReceived(new ProcfileEventArgs("system", text, DateTime.Now));
        }

        protected virtual void OnStatusReceived(ProcfileEventArgs args)
        {
            var handler = this.StatusReceived;
            if (handler == null)
                return;

            handler(this, args);
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (_started)
                return;

            _started = true;

            StartInternal();
        }

        public void Stop()
        {
            if (!_started)
                return;
            
            _started = false;

            StopInternal();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Protected Methods

        protected abstract void StartInternal();

        protected abstract void StopInternal();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Stop();
        }

        #endregion
    }
}