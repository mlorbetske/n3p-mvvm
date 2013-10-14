using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace N3P.Take2.MVVM
{
    public static class BindingActionExtensions
    {
        public static IEnumerable<TDelegate> GetBindingActions<TDelegate>(this MemberInfo type)
        {
            return type.GetCustomAttributes(true)
                .OfType<BindingBehaviorAttributeBase>()
                .SelectMany(x => x.AllActions.OfType<TDelegate>());
        }

        public static ServiceLookupResult GetService(this MemberInfo type, Type serviceType)
        {
            return type.GetCustomAttributes(true)
                .OfType<BindingBehaviorAttributeBase>()
                .Where(x => x.ServiceType == serviceType)
                .Select(x => new ServiceLookupResult
                {
                    Service = x.Service,
                    IsGlobalService = x.IsGlobalServiceOnly
                })
                .SingleOrDefault();
        }
    }
}
