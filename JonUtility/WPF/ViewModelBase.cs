namespace JonUtility.WPF
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class ViewModelBase : IDisposable, INotifyPropertyChanged
    {
        private bool disposed;

        public event PropertyChangedEventHandler PropertyChanged;
        
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.OnDispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void OnDispose(bool disposing)
        {
        }

        protected void RaisePropertyChanged([CallerMemberName]string prop = "")
        {
            if (String.IsNullOrEmpty(prop))
            {
                return;
            }

            var propertyChanged = this.PropertyChanged;
            if (propertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}