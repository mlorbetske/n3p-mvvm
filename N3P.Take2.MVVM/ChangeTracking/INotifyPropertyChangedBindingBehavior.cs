using System.ComponentModel;

namespace N3P.Take2.MVVM.ChangeTracking
{
    public interface INotifyPropertyChangedBindingBehavior : INotifyPropertyChanged
    {
        void OnPropertyChanged(string propertyName);
    }
}