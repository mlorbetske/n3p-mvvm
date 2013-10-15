using System.Collections.Generic;
using System.Collections.Specialized;

namespace N3P.MVVM.Dirty
{
    public class DirtyableService
    {
        private readonly IServiceProviderProvider _model;

        internal DirtyableService(IServiceProviderProvider model)
        {
            _model = model;
        }

        public bool IsDirty { get; private set; }

        public void MarkDirty()
        {
            foreach (var parent in _model.Parents)
            {
                var svc = parent.GetService<DirtyableService>();

                if (svc != null)
                {
                    svc.MarkDirty();
                }
            }

            IsDirty = true;
        }

        public void Clean()
        {
            foreach (var child in _model.Children)
            {
                var svc = child.GetService<DirtyableService>();

                if (svc != null)
                {
                    svc.Clean();
                }
            }

            IsDirty = false;
        }

        internal readonly Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler> CollectionChangeHandlers = new Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler>();
    }
}