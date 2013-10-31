using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using N3P.MVVM.ChangeTracking;
using N3P.MVVM.Dirty;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public class BindableBase<TModel> : IBindable<TModel>
        where TModel : BindableBase<TModel>, new()
    {
        private readonly HashSet<IServiceProviderProvider> _parents = new HashSet<IServiceProviderProvider>();
        private readonly HashSet<IServiceProviderProvider> _children = new HashSet<IServiceProviderProvider>();
        private readonly Dictionary<Type, Dictionary<string, object>> _serviceLookup = new Dictionary<Type, Dictionary<string, object>>();
        private readonly ServiceProviderImpl _serviceProvider;
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public BindableBase()
        {
            InstallTopLevelServices();
            InstallPropertyServices();

            _serviceProvider = new ServiceProviderImpl(this, null, null);
        }

        public IEnumerable<TService> GetServices<TService>()
        {
            return _serviceProvider.GetServices<TService>();
        }

        HashSet<IServiceProviderProvider> IServiceProviderProvider.Parents { get { return _parents; } }

        HashSet<IServiceProviderProvider> IServiceProviderProvider.Children { get { return _children; } }

        public static IEnumerable<TDelegate> GetActions<TDelegate, T>(Expression<Func<TModel, T>> propertyAccessor)
        {
            var memberAccess = propertyAccessor.Body as MemberExpression;

            if (memberAccess == null || memberAccess.Member.GetType().Name.TrimStart("Runtime".ToCharArray()) != "PropertyInfo")
            {
                throw new ArgumentException("Supplied expression does not access a property", "propertyAccessor");
            }

            var property = memberAccess.Member;
            var actions = new List<TDelegate>(typeof(TModel).GetBindingActions<TDelegate>(property));
            return actions.Distinct(DelegateEqualityComparer<TDelegate>.Default);
        }

        public TService GetService<TService>()
        {
            return _serviceProvider.GetService<TService>();
        }

        public TService GetService<TService, T>(Expression<Func<TModel, T>> propertyAccessor)
        {
            return _serviceProvider.Specialized(propertyAccessor).GetService<TService>();
        }

        public IServiceProvider GetServiceProvider<T>(Expression<Func<TModel, T>> propertyAccessor)
        {
            return _serviceProvider.Specialized(propertyAccessor);
        }

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

            public IEnumerable<string> Keys { get {return new string[0];} }

            public object this[string key]
            {
                get { throw new KeyNotFoundException(); }
            }

            public int Count { get { return 0; } }

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

        private class BindableBaseState : IExportedState<TModel>
        {
            private readonly IDictionary<string, object> _stateStore = new Dictionary<string, object>();

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

            public BindableBaseState(IBindable<TModel> bindableBase)
            {
                foreach (var pair in bindableBase.GetStateStore())
                {
                    _stateStore[pair.Key] = GetValueState(pair.Value);
                }
            }

            public override int GetHashCode()
            {
                return _stateStore.GetHashCode();
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

            object IExportedState.Apply(object item)
            {
                return Apply((TModel) item);
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

            public IEnumerable<string> Keys { get { return _stateStore.Keys; } }

            public object this[string key]
            {
                get { return _stateStore[key]; }
            }

            public int Count { get { return _stateStore.Count; } }

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
        }

        IExportedState IExportStateRestorer.ExportState()
        {
            return ExportState();
        }

        public IExportedState<TModel> ExportState() 
        {
            return new BindableBaseState(this);
        }

        protected T Get<T>(Expression<Func<TModel, T>> propertyAccessor)
        {
            var specializedProvider = _serviceProvider.Specialized(propertyAccessor);
            var name = GetPropertyName(propertyAccessor);
            object existing;

            if (!_values.TryGetValue(name, out existing))
            {
                existing = default(T);
            }

            var beforeGet = GetActions<BeforeGetBindingBehavior, T>(propertyAccessor);

            foreach (var behavior in beforeGet)
            {
                behavior(specializedProvider, this, name, existing);
            }


            var afterGet = GetActions<AfterGetBindingBehavior, T>(propertyAccessor);
            var current = existing;

            foreach (var behavior in afterGet)
            {
                existing = behavior(specializedProvider, this, name, existing);
            }

            WireUpParentChildRelationships(existing);

            if (!Equals(existing, current))
            {
                _values[name] = existing;
            }

            return (T)existing;
        }

        private readonly Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler> _notifyCollectionChangedEventHandlers = new Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler>();

        private void WireUpParentChildRelationships(object value)
        {
            var valueProvider = value as IServiceProviderProvider;
            var modelProvider = (IServiceProviderProvider) this;

            if (valueProvider != null)
            {
                valueProvider.Parents.Add(modelProvider);
                modelProvider.Children.Add(valueProvider);
                return;
            }

            var valueDict = value as IDictionary;

            if (valueDict != null)
            {
                foreach (var val in valueDict)
                {
                    var key = ((dynamic)val).Key;
                    var keyProvider = key as IServiceProviderProvider;

                    if (keyProvider != null)
                    {
                        keyProvider.Parents.Add(modelProvider);
                        modelProvider.Children.Add(keyProvider);
                    }

                    var v = ((dynamic)val).Value;
                    var vProvider = v as IServiceProviderProvider;

                    if (vProvider != null)
                    {
                        vProvider.Parents.Add(modelProvider);
                        modelProvider.Children.Add(vProvider);
                    }
                }

                return;
            }

            var valInp = value as INotifyCollectionChanged;

            if (valInp != null)
            {
                NotifyCollectionChangedEventHandler handler;
                if (!_notifyCollectionChangedEventHandlers.TryGetValue(valInp, out handler))
                {
                    handler = _notifyCollectionChangedEventHandlers[valInp] = (sender, args) =>
                    {
                        if (args.OldItems != null)
                        {
                            foreach (var val in args.OldItems)
                            {
                                var valProvider = val as IServiceProviderProvider;

                                if (valProvider != null)
                                {
                                    valProvider.Parents.Remove(modelProvider);
                                    modelProvider.Children.Remove(valProvider);
                                }
                            }
                        }

                        if (args.NewItems != null)
                        {
                            foreach (var val in args.NewItems)
                            {
                                var valProvider = val as IServiceProviderProvider;

                                if (valProvider != null)
                                {
                                    valProvider.Parents.Add(modelProvider);
                                    modelProvider.Children.Add(valProvider);
                                }
                            }
                        }
                    };

                    valInp.CollectionChanged += handler;
                }
            }

            var valueCollection = value as IEnumerable;

            if (valueCollection != null)
            {
                foreach (var val in valueCollection)
                {
                    var valProvider = val as IServiceProviderProvider;

                    if (valProvider != null)
                    {
                        valProvider.Parents.Add(modelProvider);
                        modelProvider.Children.Add(valProvider);
                    }
                }
            }
        }

        protected void Set<T>(Expression<Func<TModel, T>> propertyAccessor, T value)
        {
            var specializedProvider = _serviceProvider.Specialized(propertyAccessor);
            var name = GetPropertyName(propertyAccessor);
            var current = (object)Get(propertyAccessor);
            var valueCopy = (object)value;

            var beforeSet = GetActions<BeforeSetBindingBehavior, T>(propertyAccessor);

            foreach (var behavior in beforeSet)
            {
                if (behavior(specializedProvider, this, name, ref valueCopy, ref current) == BeforeSetAction.Reject)
                {
                    return;
                }
            }

            var changed = !Equals(current, valueCopy);

            if (changed)
            {
                current = _values[name] = valueCopy;
            }

            var afterSet = GetActions<AfterSetBindingBehavior, T>(propertyAccessor);

            foreach (var behavior in afterSet)
            {
                behavior(specializedProvider, this, name, value, ref current, changed);
            }

            WireUpParentChildRelationships(current);
            _values[name] = current;
        }

        private static string GetPropertyName<T>(Expression<Func<TModel, T>> propertyAccessor)
        {
            var memberAccess = propertyAccessor.Body as MemberExpression;

            if (memberAccess == null || memberAccess.Member.Name.TrimStart("Runtime".ToCharArray()) == "PropertyInfo")
            {
                throw new ArgumentException("Supplied expression does not access a property", "propertyAccessor");
            }

            return memberAccess.Member.Name;
        }

        private void InstallPropertyServices()
        {
            foreach (var prop in typeof(TModel).GetProperties())
            {
                InstallServices(prop.GetCustomAttributes(typeof(BindingBehaviorAttributeBase), true), prop.Name);
            }
        }

        private void InstallServices(IEnumerable behaviors, string bag)
        {
            foreach (BindingBehaviorAttributeBase behavior in behaviors)
            {
                var matchedService = behavior.GetService(this);
                if (behavior.ServiceType != null && matchedService != null)
                {
                    Dictionary<string, object> lookup;
                    if (!_serviceLookup.TryGetValue(behavior.ServiceType, out lookup))
                    {
                        lookup = _serviceLookup[behavior.ServiceType] = new Dictionary<string, object>();
                    }

                    if (behavior.IsGlobalServiceOnly)
                    {
                        object service;
                        if (!lookup.TryGetValue("", out service))
                        {
                            lookup[""] = matchedService;
                        }
                    }
                    else
                    {
                        lookup[bag] = matchedService;
                    }
                }
            }
        }

        public void FinializeInitialization()
        {
            foreach (var service in GetServices<IInitializationCompleteCallback>())
            {
                service.OnInitializationComplete();
            }
        }

        private void InstallTopLevelServices()
        {
            InstallServices(typeof(TModel).GetCustomAttributes(typeof(BindingBehaviorAttributeBase), true), "");
        }

        private class ServiceProviderImpl : IServiceProvider
        {
            private readonly BindableBase<TModel> _owner;
            private readonly ServiceProviderImpl _parent;
            private readonly string _propertyName;
            
            public ServiceProviderImpl(BindableBase<TModel> owner, LambdaExpression propertyAccessor, ServiceProviderImpl parent)
            {
                _owner = owner;
                _parent = parent;

                if (propertyAccessor != null)
                {
                    var memberAccess = propertyAccessor.Body as MemberExpression;

                    if (memberAccess == null || memberAccess.Member.Name.TrimStart("Runtime".ToCharArray()) == "PropertyInfo")
                    {
                        throw new ArgumentException("Supplied expression does not access a property", "propertyAccessor");
                    }

                    _propertyName = memberAccess.Member.Name;
                }
                else
                {
                    _propertyName = "";
                }
            }

            public object GetService(Type serviceType)
            {
                object service = null;
                Dictionary<string, object> lookup;

                if (_owner._serviceLookup.TryGetValue(serviceType, out lookup))
                {
                    lookup.TryGetValue(_propertyName, out service);
                }

                return service ?? (_parent != null ? _parent.GetService(serviceType) : null);
            }

            public IServiceProvider Specialized<T>(Expression<Func<TModel, T>> property)
            {
                return new ServiceProviderImpl(_owner, property, this);
            }

            public IEnumerable<T> GetServices<T>()
            {
                return _owner._serviceLookup
                    .Where(x => typeof (T).IsAssignableFrom(x.Key))
                    .Select(x => x.Value)
                    .Where(x => x.ContainsKey(_propertyName))
                    .Select(x => x[_propertyName])
                    .Distinct()
                    .Cast<T>()
                    .ToList();
            }
        }

        public void AcceptState<T>(IExportedState<T> state)
            where T : class, TModel
        {
            foreach (var key in state.Keys)
            {
                _values[key] = state[key];
                ((TModel) this).OnPropertyChanged(key);
            }
        }

        public IDictionary<string, object> GetStateStore()
        {
            return _values;
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                var inp = GetService<INotifyPropertyChanged>();

                if (inp != null)
                {
                    inp.PropertyChanged += value;
                }
            }
            remove
            {
                var inp = GetService<INotifyPropertyChanged>();

                if (inp != null)
                {
                    inp.PropertyChanged -= value;
                }
            }
        }
    }
}