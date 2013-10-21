using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using N3P.MVVM.Undo;

namespace N3P.MVVM.Dirty
{
    public class DirtyableService: IInitializationCompleteCallback
    {
        private readonly IServiceProviderProvider _model;

        internal DirtyableService(IServiceProviderProvider model)
        {
            _model = model;
        }

        public bool IsDirty { get; private set; }
        public IExportedState CleanVersion { get; private set; }

        public void MarkDirty()
        {
            MarkDirtyInternal();
        }

        private void MarkDirtyInternal()
        {
            foreach (var parent in _model.Parents)
            {
                var svc = parent.GetService<DirtyableService>();

                if (svc != null)
                {
                    svc.MarkDirtyInternal();
                }
            }

            var s = _model.GetService<DirtyableService>();
            var oldDirty = s.IsDirty;
            s.IsDirty = true;

            if (oldDirty ^ s.IsDirty)
            {
                s.OnDirtyStateChanged();
            }
        }

        private void CleanInternal()
        {
            foreach (var child in _model.Children)
            {
                var svc = child.GetService<DirtyableService>();

                if (svc != null)
                {
                    svc.CleanInternal();
                }
            }

            var s = _model.GetService<DirtyableService>();
            var oldDirty = s.IsDirty;
            s.IsDirty = false;

            if (oldDirty ^ s.IsDirty)
            {
                s.OnDirtyStateChanged();
            }

            CleanVersion = ((IExportStateRestorer) _model).ExportState();
        }

        public void Clean()
        {
            CleanInternal();
        }

        private void OnDirtyStateChanged()
        {
            var handler = DirtyStateChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler DirtyStateChanged;

        internal readonly Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler> CollectionChangeHandlers = new Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler>();
        
        public void OnInitializationComplete()
        {
            Clean();
        }
    }
}