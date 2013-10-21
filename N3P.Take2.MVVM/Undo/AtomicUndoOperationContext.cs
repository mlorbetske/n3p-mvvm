using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace N3P.MVVM.Undo
{
    public class AtomicUndoOperationContext
    {
        private class UndoOperationContext<T> : IDisposable
            where T : class, IBindable<T>
        {
            private readonly T _item;
            private readonly IServiceProviderProvider[] _suppressEntirely;

            public UndoOperationContext(T item, IServiceProviderProvider[] suppressEntirely)
            {
                _item = item;
                _item.MakeVolatile();
                _suppressEntirely = suppressEntirely;

                foreach (var entry in _suppressEntirely)
                {
                    var handler = entry.GetService<UndoHandler>();

                    if (handler != null)
                    {
                        ++handler.CaptureSuspensionDepth;
                    }
                }

                _item.SuspendAutoUndoStateCapture();
            } 

            public void Dispose()
            {
                _item.ResumeAutoUndoStateCapture();

                foreach (var entry in _suppressEntirely)
                {
                    var handler = entry.GetService<UndoHandler>();

                    if (handler != null)
                    {
                        --handler.CaptureSuspensionDepth;
                    }
                }
            }
        }

        public static IDisposable Get<T>(T item, params IServiceProviderProvider[] suppressEntirely)
            where T : class, IBindable<T>
        {
            return new UndoOperationContext<T>(item, suppressEntirely);
        }
    }
}
