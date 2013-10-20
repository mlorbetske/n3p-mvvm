using System;
using System.ComponentModel;

namespace N3P.MVVM.ChangeTracking
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class NotifyOnChangeAttribute : BindingBehaviorAttributeBase
    {
        public NotifyOnChangeAttribute()
            : base(afterSet: AfterSet)
        {
        }

        public static void AfterSet(IServiceProvider serviceProvider, object model, string propertyName, object proposedValue, ref object currentValue, bool changed)
        {
            if (changed)
            {
                var inp = serviceProvider.GetService<INotifyPropertyChanged>() as NotifyPropertyChanged;

                if (inp != null)
                {
                    inp.OnPropertyChanged(model, propertyName);
                }
            }
        }

        public override Type ServiceType
        {
            get { return typeof (INotifyPropertyChanged); }
        }

        public override bool IsGlobalServiceOnly
        {
            get { return true; }
        }

        public override object GetService(object model)
        {
            return new NotifyPropertyChanged();
        }

        internal class NotifyPropertyChanged : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(object model, string propertyName)
            {
                var handler = PropertyChanged;
                
                if (handler != null)
                {
                    handler(model, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
