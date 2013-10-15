using System;
using System.Collections;
using System.Collections.Specialized;

namespace N3P.MVVM.Undo
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class UndoableAttribute : BindingBehaviorAttributeBase
    {
        public UndoableAttribute()
            : base(afterGet: AfterGet, beforeSet: AfterSet, afterGetPriority: int.MaxValue, afterSetPriority: int.MaxValue)
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
            var valueProvider = value as IServiceProviderProvider;

            if (valueProvider != null)
            {
                valueProvider.Parents.Add((IServiceProviderProvider) model);
                return value;
            }

            var valueDict = value as IDictionary;

            if (valueDict != null)
            {
                foreach (var val in valueDict)
                {
                    var key = ((dynamic) val).Key;
                    var keyProvider = key as IServiceProviderProvider;

                    if (keyProvider != null)
                    {
                        keyProvider.Parents.Add((IServiceProviderProvider) model);
                    }

                    var v = ((dynamic)val).Value;
                    var vProvider = v as IServiceProviderProvider;
                    
                    if (vProvider != null)
                    {
                        vProvider.Parents.Add((IServiceProviderProvider) model);
                    }
                }

                return value;
            }

            var valInp = value as INotifyCollectionChanged;

            if (valInp != null)
            {
                valInp.CollectionChanged += (sender, args) =>
                {
                    var handler = serviceProvider.GetService<UndoHandler>();
                    handler.MakeVolatile();

                    if (args.NewItems != null)
                    {
                        foreach (var val in args.NewItems)
                        {
                            var valProvider = val as IServiceProviderProvider;

                            if (valProvider != null)
                            {
                                valProvider.Parents.Add((IServiceProviderProvider)model);
                            }
                        }
                    }
                };
            }

            var valueCollection = value as IEnumerable;

            if (valueCollection != null)
            {
                foreach (var val in valueCollection)
                {
                    var valProvider = val as IServiceProviderProvider;

                    if (valProvider != null)
                    {
                        valProvider.Parents.Add((IServiceProviderProvider) model);
                    }
                }
            }

            return value;
        }

        private static BeforeSetAction AfterSet(IServiceProvider serviceprovider, object model, string propertyname, ref object proposedValue, ref object currentvalue)
        {
            var handler = serviceprovider.GetService<UndoHandler>();
            handler.MakeVolatile();
            return BeforeSetAction.Accept;
        }
    }
}
