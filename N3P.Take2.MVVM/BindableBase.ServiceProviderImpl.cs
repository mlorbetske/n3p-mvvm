using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace N3P.MVVM
{
    public partial class BindableBase<TModel>
    {
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

            public IEnumerable<T> GetServices<T>()
            {
                return _owner._serviceLookup
                    .Where(x => typeof(T).IsAssignableFrom(x.Key))
                    .Select(x => x.Value)
                    .Where(x => x.ContainsKey(_propertyName))
                    .Select(x => x[_propertyName])
                    .Distinct()
                    .Cast<T>()
                    .ToList();
            }

            public IServiceProvider Specialized<T>(Expression<Func<TModel, T>> property)
            {
                return new ServiceProviderImpl(_owner, property, this);
            }

            public IServiceProvider Specialized<T>(PropertyInfo property)
            {
                var modelParam = Expression.Parameter(typeof(T));
                var expr = Expression.MakeMemberAccess(modelParam, property);
                var lambda = Expression.Lambda(expr, new[] { modelParam });
                return new ServiceProviderImpl(_owner, lambda, this);
            }
        }
    }
}
