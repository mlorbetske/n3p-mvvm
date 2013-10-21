using System.Collections.Generic;

namespace N3P.MVVM
{
    public interface IExportedState
    {
        IEnumerable<string> Keys { get; }

        object this[string key] { get; }

        int Count { get; }

        object Apply(object item);
    }

    public interface IExportedState<in T> : IExportedState
        where T : class
    {
        object Apply(T item);
    }
}
