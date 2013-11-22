using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using N3P.MVVM.BehaviorDelegates;
using N3P.MVVM.ChangeTracking;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public partial class BindableBase<TModel> : IBindable<TModel>
        where TModel : BindableBase<TModel>
    {
        private readonly HashSet<IServiceProviderProvider> _children = new HashSet<IServiceProviderProvider>();
        private readonly Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler> _notifyCollectionChangedEventHandlers = new Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler>();
        private readonly HashSet<IServiceProviderProvider> _parents = new HashSet<IServiceProviderProvider>();
        private readonly Dictionary<Type, Dictionary<string, object>> _serviceLookup = new Dictionary<Type, Dictionary<string, object>>();
        private readonly ServiceProviderImpl _serviceProvider;
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly HashSet<BindingBehaviorAttributeBase> _initedServices = new HashSet<BindingBehaviorAttributeBase>();

        public BindableBase()
        {
            _serviceProvider = new ServiceProviderImpl(this, null, null);
            InstallTopLevelServices();
            InstallPropertyServices();
            InitTopLevelServices();
        }

        public event PropertyChangedEventHandler PropertyChanged
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

        HashSet<IServiceProviderProvider> IServiceProviderProvider.Children { get { return _children; } }

        HashSet<IServiceProviderProvider> IServiceProviderProvider.Parents { get { return _parents; } }

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

        public void AcceptState(IExportedState state)
        {
            foreach (var key in state.Keys)
            {
                _values[key] = state[key];
                ((IBindable<TModel>)this).OnPropertyChanged(key);
            }
        }

        public IExportedState ExportState()
        {
            return new BindableBaseState(this);
        }

        public void FinializeInitialization()
        {
            foreach (var service in GetServices<IInitializationCompleteCallback>())
            {
                service.OnInitializationComplete();
            }
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

        public IEnumerable<TService> GetServices<TService>()
        {
            return _serviceProvider.GetServices<TService>();
        }

        public IDictionary<string, object> GetStateStore()
        {
            return _values;
        }

        IExportedState IExportStateRestorer.ExportState()
        {
            return ExportState();
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
                InstallServices(prop.GetCustomAttributes(typeof(BindingBehaviorAttributeBase), true).OfType<BindingBehaviorAttributeBase>(), prop.Name);
            }
        }

        private void InstallServices(IEnumerable<BindingBehaviorAttributeBase> behaviors, string bag)
        {
            foreach (var behavior in behaviors.OrderBy(x => x.InitPriority))
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
                        lookup[bag ?? ""] = matchedService;
                    }
                }

                if (string.IsNullOrEmpty(bag))
                {
                    continue;
                }

                var init = behavior.AllActions.OfType<InitBindingBehavior>().SingleOrDefault();

                if (init != null && _initedServices.Add(behavior))
                {
                    init(_serviceProvider, _serviceProvider.Specialized<TModel>, this, s =>
                    {
                        object v;
                        _values.TryGetValue(s, out v);
                        return v;
                    }, (s, o) => _values[s] = o, bag);
                }
            }
        }

        private void InitTopLevelServices()
        {
            var behaviors = typeof (TModel).GetCustomAttributes(typeof (BindingBehaviorAttributeBase), true).OfType<BindingBehaviorAttributeBase>();

            foreach (var behavior in behaviors.OrderBy(x => x.InitPriority))
            {
                var init = behavior.AllActions.OfType<InitBindingBehavior>().SingleOrDefault();

                if (init != null && _initedServices.Add(behavior))
                {
                    init(_serviceProvider, _serviceProvider.Specialized<TModel>, this, s =>
                    {
                        object v;
                        _values.TryGetValue(s, out v);
                        return v;
                    }, (s, o) => _values[s] = o, "");
                }
            }
        }

        private void InstallTopLevelServices()
        {
            InstallServices(typeof(TModel).GetCustomAttributes(typeof(BindingBehaviorAttributeBase), true).OfType<BindingBehaviorAttributeBase>(), "");
        }

        private void WireUpParentChildRelationships(object value)
        {
            var valueProvider = value as IServiceProviderProvider;
            var modelProvider = (IServiceProviderProvider)this;

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

        public Command CreateCommand(Action action)
        {
            return new Command(action);
        }

        public Command<T> CreateCommand<T>(Action<T> action)
        {
            return new Command<T>(action);
        }

        public Command CreateCommand(Action action, Func<bool> canExecute)
        {
            return new Command(action, canExecute);
        }

        public Command<T> CreateCommand<T>(Action<T> action, Func<T, bool> canExecute)
        {
            return new Command<T>(action, canExecute);
        }
    }
}