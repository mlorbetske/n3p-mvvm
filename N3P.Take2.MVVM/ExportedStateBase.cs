using System.Collections;
using System.Collections.Generic;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public partial class BindableBase<TModel>
    {
        private abstract class ExportedStateBase : IExportedState
        {
            public virtual IEnumerable<string> Keys
            {
                get { return new string[0]; }
            }

            public virtual object this[string key]
            {
                get { throw new KeyNotFoundException(); }
            }

            public static IExportedState ExportItemState(object value)
            {
                var exportable = value as IExportStateRestorer;

                if (exportable != null)
                {
                    return exportable.ExportState();
                }

                var dict = value as IDictionary;

                if (dict != null)
                {
                    return new DictionaryState(dict);
                }

                var nestedList = value as IList;

                if (nestedList != null)
                {
                    return new ListState(nestedList);
                }

                return new IdentityState(value);
            }

            public virtual int Count
            {
                get { return 0; }
            }

            public abstract object Apply();
        }
    }
}