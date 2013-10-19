using System;
using System.Collections.Specialized;

namespace N3P.MVVM.Undo
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class UndoableAttribute : BindingBehaviorAttributeBase
    {
        public UndoableAttribute()
            : base(afterGet: AfterGet, beforeSet: BeforeSet, afterGetPriority: int.MaxValue, afterSetPriority: int.MaxValue)
        {
        }

        public override Type ServiceType
        {
            get { return typeof (UndoHandler); }
        }

        public override bool IsGlobalServiceOnly
        {
            get { return true; }
        }

        public override object GetService(object model)
        {
            var handler = new UndoHandler((IExportStateRestorer) model);
            handler.MakeVolatile();
            return handler;
        }

        private static object AfterGet(IServiceProvider serviceProvider, object model, string propertyName, object value)
        {
            var handler = serviceProvider.GetService<UndoHandler>();
            var valInp = value as INotifyCollectionChanged;

            if (valInp != null)
            {
                NotifyCollectionChangedEventHandler capture;

                if (!handler.CollectionChangeHandlers.TryGetValue(valInp, out capture))
                {
                    capture = handler.CollectionChangeHandlers[valInp] = (sender, args) => handler.MakeVolatile();
                    valInp.CollectionChanged += capture;
                }
            }

            return value;
        }

        private static BeforeSetAction BeforeSet(IServiceProvider serviceprovider, object model, string propertyname, ref object proposedValue, ref object currentvalue)
        {
            var handler = serviceprovider.GetService<UndoHandler>();
            handler.MakeVolatile();
            return BeforeSetAction.Accept;
        }
    }
}
