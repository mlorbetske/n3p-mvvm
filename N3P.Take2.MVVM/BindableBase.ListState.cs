using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace N3P.MVVM
{
    public partial class BindableBase<TModel>
    {
        private class ListState : ExportedStateBase
        {
            private readonly IList _target;
            private readonly List<IExportedState> _values = new List<IExportedState>();

            public ListState(IList list)
            {
                _target = list;

                foreach (var value in list)
                {
                    var result = ExportItemState(value);
                    _values.Add(result);
                }
            }

            public override bool Equals(object obj)
            {
                var list = obj as ListState;

                if (list != null)
                {
                    if (list._values.Count != _values.Count)
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

                var realList = obj as IList;

                if (realList == null || realList.Count != _values.Count)
                {
                    return false;
                }

                foreach (var item in realList)
                {
                    if (_values.All(x => !Equals(x, item)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return _values.Aggregate(0, (x, y) => x ^ y.GetHashCode());
            }

            public override object Apply()
            {
                _target.Clear();

                foreach (var listItem in _values)
                {
                    _target.Add(listItem.Apply());
                }

                return _target;
            }
        }
    }
}
