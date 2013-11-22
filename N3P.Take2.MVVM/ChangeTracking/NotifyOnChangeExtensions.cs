using System.ComponentModel;

namespace N3P.MVVM.ChangeTracking
{
    public static class NotifyOnChangeExtensions
    {
        public static void OnPropertyChanged<T>(this T sender, string propertyName)
            where T : class, IBindable<T>
        {
            var svc =  sender.GetService<INotifyPropertyChanged>() as NotifyOnChangeAttribute.NotifyPropertyChanged;

            if (svc != null)
            {
                svc.OnPropertyChanged(sender, propertyName);
            }
        }
        public static void OnPropertyChanged<T>(this IBindable<T> sender, string propertyName)
            where T : class, IBindable<T>
        {
            ((T)sender).OnPropertyChanged(propertyName);
        }
    }
}