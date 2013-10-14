using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace N3P.Take2.MVVM
{
    public class BindableBase<TModel>
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        private void InstallTopLevelServices()
        {
            InstallServices(typeof(TModel).GetCustomAttributes(typeof(BindingBehaviorAttributeBase), true), "");
        }

        private void InstallServices(IEnumerable behaviors, string bag)
        {
            foreach (BindingBehaviorAttributeBase behavior in behaviors)
            {
                var matchedService = behavior.Service;
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

        private void InstallPropertyServices()
        {
            foreach (var prop in typeof(TModel).GetProperties())
            {
                InstallServices(prop.GetCustomAttributes(typeof(BindingBehaviorAttributeBase), true), prop.Name);
            }
        }

        public BindableBase()
        {
            InstallTopLevelServices();
            InstallPropertyServices();

            _serviceProvider = new ServiceProviderImpl(this, null, null);
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

        private static string GetPropertyName<T>(Expression<Func<TModel, T>> propertyAccessor)
        {
            var memberAccess = propertyAccessor.Body as MemberExpression;

            if (memberAccess == null || memberAccess.Member.Name.TrimStart("Runtime".ToCharArray()) == "PropertyInfo")
            {
                throw new ArgumentException("Supplied expression does not access a property", "propertyAccessor");
            }

            return memberAccess.Member.Name;
        }

        private readonly Dictionary<Type, Dictionary<string, object>> _serviceLookup = new Dictionary<Type, Dictionary<string, object>>();

        private readonly ServiceProviderImpl _serviceProvider;

        private class ServiceProviderImpl : IServiceProvider
        {
            private readonly BindableBase<TModel> _owner;
            private readonly string _propertyName;
            private readonly ServiceProviderImpl _parent;

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

            public IServiceProvider Specialized<T>(Expression<Func<TModel, T>> property)
            {
                return new ServiceProviderImpl(_owner, property, this);
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
        }

        public static IEnumerable<TDelegate> GetActions<TDelegate, T>(Expression<Func<TModel, T>> propertyAccessor)
        {
            var actions = new List<TDelegate>(typeof(TModel).GetBindingActions<TDelegate>());

            var memberAccess = propertyAccessor.Body as MemberExpression;

            if (memberAccess == null || memberAccess.Member.GetType().Name.TrimStart("Runtime".ToCharArray()) != "PropertyInfo")
            {
                throw new ArgumentException("Supplied expression does not access a property", "propertyAccessor");
            }

            var property = memberAccess.Member;
            actions.AddRange(property.GetBindingActions<TDelegate>());
            return actions.Distinct(DelegateEqualityComparer<TDelegate>.Default);
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

            foreach (var behavior in afterGet)
            {
                existing = behavior(specializedProvider, this, name, existing);
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

            _values[name] = current;
        }
    }
}