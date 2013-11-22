using System.Collections.Generic;
using System.ComponentModel;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public interface IBindable<in TModel> : INotifyPropertyChanged, IExportStateRestorer, IServiceProviderProvider
        where TModel : class, IBindable<TModel>
    {
        void AcceptState(IExportedState state);

        IDictionary<string, object> GetStateStore();
    }
}