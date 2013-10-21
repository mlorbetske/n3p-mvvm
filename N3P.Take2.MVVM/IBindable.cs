using System.Collections.Generic;
using System.ComponentModel;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public interface IBindable<in TModel> : INotifyPropertyChanged, IExportStateRestorer<TModel>, IServiceProviderProvider
        where TModel : class, IBindable<TModel>
    {
        void AcceptState<T>(IExportedState<T> state)
            where T : class, TModel;

        IDictionary<string, object> GetStateStore();
    }
}