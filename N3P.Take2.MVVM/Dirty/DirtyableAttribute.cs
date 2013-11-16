using System;
using System.Collections.Specialized;
using System.Reflection;

namespace N3P.MVVM.Dirty
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DirtyableAttribute : BindingBehaviorAttributeBase
    {
        public DirtyableAttribute()
            : base(afterGet: AfterGet, afterSet: AfterSet, init: Initialize, afterSetPriority: int.MaxValue, afterGetPriority: int.MaxValue, initPriority: int.MaxValue)
        {
        }

        private static void Initialize(IServiceProvider serviceprovider, Func<PropertyInfo, IServiceProvider> specializedserviceprovidergetter, object model, Func<string, object> getProperty, Action<string, object> setProperty)
        {
            serviceprovider.GetService<DirtyableService>().Clean();
        }

        public override Type ServiceType
        {
            get { return typeof (DirtyableService); }
        }

        public override object GetService(object model)
        {
            return new DirtyableService((IServiceProviderProvider) model);
        }

        public override bool IsGlobalServiceOnly
        {
            get { return true; }
        }

        private static object AfterGet(IServiceProvider serviceProvider, object model, string propertyName, object value)
        {
            var valInp = value as INotifyCollectionChanged;
            var svc = serviceProvider.GetService<DirtyableService>();

            if (valInp != null)
            {
                NotifyCollectionChangedEventHandler capture;

                if (!svc.CollectionChangeHandlers.TryGetValue(valInp, out capture))
                {
                    capture = svc.CollectionChangeHandlers[valInp] = (sender, args) =>
                    {
                        if (args.NewItems != null)
                        {
                            foreach (var item in args.NewItems)
                            {
                                var spp = item as IServiceProviderProvider;

                                if (spp != null)
                                {
                                    spp.Parents.Add((IServiceProviderProvider) model);
                                }
                            }
                        }

                        svc.MarkDirty();
                    };
                    valInp.CollectionChanged += capture;
                }
            }

            return value;
        }

        private static void AfterSet(IServiceProvider serviceprovider, object model, string propertyname, object proposedvalue, ref object currentvalue, bool changed)
        {
            if (changed && (model.GetType().GetProperty(propertyname).GetCustomAttributes(typeof(NonDirtyableAttribute), true).Length == 0 ||  model.GetType().GetProperty(propertyname).GetCustomAttributes(typeof(DirtyableAttribute), true).Length != 0))
            {
                var service = serviceprovider.GetService<DirtyableService>();
                service.MarkDirty();
            }
        }
    }
}
