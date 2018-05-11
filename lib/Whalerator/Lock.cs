using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator
{
    public class Lock : IDisposable
    {
        private readonly Action releaseAction;
        private readonly Func<TimeSpan, bool> extendAction;

        public Lock(Action releaseAction, Func<TimeSpan, bool> extendAction)
        {
            this.releaseAction = releaseAction;
            this.extendAction = extendAction;
        }

        public bool Extend(TimeSpan time)
        {
            return extendAction(time);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    releaseAction();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Lock() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
