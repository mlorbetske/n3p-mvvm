using System.Collections;
using System.Collections.Generic;
using System.Linq;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public partial class BindableBase<TModel>
    {
        private class ListState : IExportedState
        {
            private readonly IList _target;
            private readonly List<KeyValuePair<object, object>> _values = new List<KeyValuePair<object, object>>();

            public ListState(IList list)
            {
                _target = list;

                foreach (var value in list)
                {
                    var exportable = value as IExportStateRestorer;

                    if (exportable != null)
                    {
                        _values.Add(new KeyValuePair<object, object>(value, exportable.ExportState()));
                        continue;
                    }

                    var nestedList = value as IList;

                    if (nestedList != null)
                    {
                        _values.Add(new KeyValuePair<object, object>(value, new ListState(nestedList)));
                        continue;
                    }

                    _values.Add(new KeyValuePair<object, object>(value, value));
                }
            }

            public int Count { get { return 0; } }

            public IEnumerable<string> Keys { get { return new string[0]; } }

            public object this[string key]
            {
                get { throw new KeyNotFoundException(); }
            }

            public object Apply(object item)
            {
                _target.Clear();

                foreach (var listItem in _values)
                {
                    var state = listItem.Value as IExportedState;

                    if (state != null)
                    {
                        _target.Add(state.Apply(listItem.Key));
                        continue;
                    }

                    _target.Add(listItem.Value);
                }

                return _target;
            }

            public override bool Equals(object obj)
            {
                var list = obj as ListState;

                if (list == null)
                {
                    return false;
                }

                foreach (var item in _values)
                {
                    if (!list._values.Any(x => Equals(x, item)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return _values.Aggregate(0, (x, y) => x ^ y.Value.GetHashCode());
            }
        }
    }
}
