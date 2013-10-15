using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using N3P.MVVM.Dirty;
using N3P.MVVM.Undo;

namespace N3P.MVVM
{
    public class BindableBase<TModel> : IExportStateRestorer, IServiceProviderProvider
        where TModel : BindableBase<TModel>
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

        public Action GetStateRestorer()
        {
            var actions = new List<Action<BindableBase<TModel>>> { m => m._values.Clear() };
            var dirtyHandler = _serviceProvider == null ? null : GetService<DirtyableService>();
            var wasDirty = dirtyHandler != null && dirtyHandler.IsDirty;

            foreach (var entry in _values)
            {
                var key = entry.Key;

                if (entry.Value == null)
                {
                    actions.Add(m => m._values[key] = null);
                    continue;
                }

                var bindable = entry.Value as IExportStateRestorer;
                var value = entry.Value;

                if (bindable != null)
                {
                    var act = bindable.GetStateRestorer();
                    actions.Add(m =>
                    {
                        m._values[key] = value;
                        act();
                    });
                    continue;
                }

                var bindableDictionary = entry.Value as IDictionary;

                if (bindableDictionary != null)
                {
                    actions.Add(m => ((IDictionary)(m._values[key] = value)).Clear());

                    foreach (var child in bindableDictionary)
                    {
                        var childKey = ((dynamic)child).Key;
                        var childValue = ((dynamic)child).Value;

                        if (childKey is IExportStateRestorer)
                        {
                            var childKeyAct = ((IExportStateRestorer)childKey).GetStateRestorer();

                            if (childValue is IExportStateRestorer)
                            {
                                var childValueAct = ((IExportStateRestorer)childValue).GetStateRestorer();
                                actions.Add(m =>
                                {
                                    childKeyAct();
                                    childValueAct();
                                    m._values[childKey] = childValue;
                                });
                            }
                            else
                            {
                                actions.Add(m =>
                                {
                                    childKeyAct();
                                    ((IDictionary)m._values)[childKey] = childValue;
                                });
                            }
                        }
                        else if (childValue is IExportStateRestorer)
                        {
                            var childValueAct = ((IExportStateRestorer)childValue).GetStateRestorer();
                            actions.Add(m =>
                            {
                                childValueAct();
                                ((IDictionary)m._values[key])[childKey] = childValue;
                            });
                        }
                        else
                        {
                            actions.Add(m => m._values[childKey] = childValue);
                        }
                    }

                    continue;
                }

                var bindableList = entry.Value as IList;

                if (bindableList != null)
                {
                    actions.Add(m => ((IList)(m._values[key] = value)).Clear());

                    foreach (var child in bindableList)
                    {
                        var childVal = child;
                        if (child is IExportStateRestorer)
                        {
                            var childAct = ((IExportStateRestorer)child).GetStateRestorer();
                            actions.Add(m =>
                            {
                                childAct();
                                ((IList)m._values[key]).Add(childVal);
                            });
                        }
                        else
                        {
                            actions.Add(m => ((IList)m._values[key]).Add(childVal));
                        }
                    }

                    continue;
                }

                actions.Add(m => m._values[key] = value);
            }

            actions.Add(m =>
            {
                dirtyHandler = GetService<DirtyableService>();

                if (wasDirty)
                {
                    dirtyHandler.MarkDirty();
                }
                else
                {
                    dirtyHandler.Clean();
                }
            });

            return () =>
            {
                foreach (var action in actions)
                {
                    action(this);
                }
            };
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
        }
    }
}