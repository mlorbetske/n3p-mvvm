using System.ComponentModel;

namespace N3P.MVVM.ChangeTracking
{
    public interface INotifyPropertyChangedBindingBehavior : INotifyPropertyChanged
    {
        void OnPropertyChanged(string propertyName);
    }
}