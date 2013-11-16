using System.Collections;
using System.Collections.Generic;
using System.Linq;
using N3P.MVVM.ChangeTracking;
using N3P.MVVM.Dirty;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public partial class BindableBase<TModel>
    {
        private class BindableBaseState : IExportedState<TModel>
        {
            private readonly IDictionary<string, object> _stateStore = new Dictionary<string, object>();

            public BindableBaseState(IBindable<TModel> bindableBase)
            {
                foreach (var pair in bindableBase.GetStateStore())
                {
                    _stateStore[pair.Key] = GetValueState(pair.Value);
                }
            }

            public int Count { get { return _stateStore.Count; } }

            public IEnumerable<string> Keys { get { return _stateStore.Keys; } }

            public object this[string key]
            {
                get { return _stateStore[key]; }
            }

            public object Apply(TModel item)
            {
                var realItem = item ?? new TModel();
                var state = realItem.GetStateStore();
                var stateKeys = new HashSet<string>(state.Keys);
                var demandKeys = new HashSet<string>(_stateStore.Keys);
                var toRemove = stateKeys.Except(demandKeys).ToList();
                var toAddOrUpdate = demandKeys.Except(toRemove);

                foreach (var key in toRemove)
                {
                    state.Remove(key);
                    realItem.OnPropertyChanged(key);
                }

                foreach (var key in toAddOrUpdate)
                {
                    object current;
                    state.TryGetValue(key, out current);
                    var exported = _stateStore[key] as IExportedState;

                    if (exported != null)
                    {
                        var result = exported.Apply(current);
                        state[key] = result;
                    }
                    else
                    {
                        state[key] = _stateStore[key];
                    }

                    realItem.OnPropertyChanged(key);
                }

                realItem.SetDirtyState(this);
                return realItem;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                var model = obj as TModel;

                if (model != null)
                {
                    return Equals(model);
                }

                var exportedModel = obj as IExportedState<TModel>;

                if (exportedModel != null)
                {
                    return Equals(exportedModel);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return _stateStore.GetHashCode();
            }

            object IExportedState.Apply(object item)
            {
                return Apply((TModel)item);
            }

            private static object GetValueState(object value)
            {
                var exportable = value as IExportStateRestorer;

                if (exportable != null)
                {
                    return exportable.ExportState();
                }

                var dict = value as IDictionary;
                //TODO: Implement dictionary state export

                var list = value as IList;

                if (list != null)
                {
                    return new ListState(list);
                }

                return value;
            }
            private bool Equals(TModel other)
            {
                return Equals(other.ExportState());
            }

            private bool Equals(IExportedState other)
            {
                var stateKeys = new HashSet<string>(_stateStore.Keys);
                var otherKeys = new HashSet<string>(other.Keys);

                //This is technically incorrect I think, if we've persisted a default value then the key count could be
                //  different but the items would still be value-equal
                if (stateKeys.Count != otherKeys.Count)
                {
                    return false;
                }

                stateKeys.IntersectWith(otherKeys);

                //If intersecting the sets of keys resulted in a different number of keys being present
                //  then different elements have been set amongst the value stores, return false
                if (otherKeys.Count != stateKeys.Count)
                {
                    return false;
                }

                foreach (var key in stateKeys)
                {
                    //If the values for the entries aren't value-equal
                    if (!Equals(_stateStore[key], other[key]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
