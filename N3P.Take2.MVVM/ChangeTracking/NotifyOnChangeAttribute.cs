using System;
using System.ComponentModel;

namespace N3P.Take2.MVVM.ChangeTracking
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
                    inp.OnPropertyChanged(propertyName);
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

        public override object Service
        {
            get { return new NotifyPropertyChanged(); }
        }

        private class NotifyPropertyChanged : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
