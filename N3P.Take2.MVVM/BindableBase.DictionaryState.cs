using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace N3P.MVVM
{
    public partial class BindableBase<TModel>
    {
        private class DictionaryState : ExportedStateBase, IEquatable<IDictionary>
        {
            private readonly IDictionary _target;
            private readonly List<KeyValuePair<IExportedState, IExportedState>> _values = new List<KeyValuePair<IExportedState, IExportedState>>();

            public DictionaryState(IDictionary dictionary)
            {
                _target = dictionary;

                foreach (var value in dictionary.Keys)
                {
                    var key = ExportItemState(value);
                    var val = ExportItemState(dictionary[value]);
                    _values.Add(new KeyValuePair<IExportedState, IExportedState>(key, val));
                }
            }

            public override object Apply()
            {
                _target.Clear();

                foreach (var listItem in _values)
                {
                    _target.Add(listItem.Key.Apply(), listItem.Value.Apply());
                }

                return _target;
            }

            public bool Equals(IDictionary other)
            {
                if (ReferenceEquals(other, null) || other.Count != _values.Count)
                {
                    return false;
                }

                foreach (var key in other.Keys)
                {
                    if (_values.All(x => !Equals(x.Key, key) && !Equals(x.Value, other[x])))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as IDictionary);
            }

            public override int GetHashCode()
            {
                return _values.Aggregate(0, (x, y) => x ^ (y.Key != null ? y.Key.GetHashCode() : 0) ^ (y.Value != null ? y.Value.GetHashCode() : 0));
            }
        }
    }
}
