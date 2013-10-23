using System.ComponentModel;

namespace N3P.MVVM.ChangeTracking
{
    public static class NotifyOnChangeExtensions
    {
        public static void OnPropertyChanged<T>(this T sender, string propertyName)
            where T : BindableBase<T>, new()
        {
            var svc =  sender.GetService<INotifyPropertyChanged>() as NotifyOnChangeAttribute.NotifyPropertyChanged;

            if (svc != null)
            {
                svc.OnPropertyChanged(sender, propertyName);
            }
        }
    }
}