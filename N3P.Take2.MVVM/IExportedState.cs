using System.Collections.Generic;

namespace N3P.MVVM
{
    public interface IExportedState
    {
        IEnumerable<string> Keys { get; }

        object this[string key] { get; }

        int Count { get; }

        object Apply();
    }
}
