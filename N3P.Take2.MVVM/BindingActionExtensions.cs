using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace N3P.MVVM
{
    public static class BindingActionExtensions
    {
        public static IEnumerable<TDelegate> GetBindingActions<TDelegate>(this MemberInfo type)
        {
            Func<BindingBehaviorAttributeBase, int> orderSelector = x => int.MaxValue / 2;
            var dlgtType = typeof (TDelegate);

            if (dlgtType == typeof (BeforeGetBindingBehavior))
            {
                orderSelector = x => x.BeforeGetPriority;
            }
            else if (dlgtType == typeof (AfterGetBindingBehavior))
            {
                orderSelector = x => x.AfterGetPriority;
            }
            else if (dlgtType == typeof (BeforeSetBindingBehavior))
            {
                orderSelector = x => x.BeforeSetPriority;
            }
            else if (dlgtType == typeof (AfterSetBindingBehavior))
            {
                orderSelector = x => x.AfterSetPriority;
            }

            return type.GetCustomAttributes(true)
                .OfType<BindingBehaviorAttributeBase>()
                .OrderBy(orderSelector)
                .SelectMany(x => x.AllActions.OfType<TDelegate>());
        }

        public static IEnumerable<TDelegate> GetBindingActions<TDelegate>(this MemberInfo type, MemberInfo child)
        {
            Func<BindingBehaviorAttributeBase, int> orderSelector = x => int.MaxValue / 2;
            var dlgtType = typeof (TDelegate);

            if (dlgtType == typeof (BeforeGetBindingBehavior))
            {
                orderSelector = x => x.BeforeGetPriority;
            }
            else if (dlgtType == typeof (AfterGetBindingBehavior))
            {
                orderSelector = x => x.AfterGetPriority;
            }
            else if (dlgtType == typeof (BeforeSetBindingBehavior))
            {
                orderSelector = x => x.BeforeSetPriority;
            }
            else if (dlgtType == typeof (AfterSetBindingBehavior))
            {
                orderSelector = x => x.AfterSetPriority;
            }

            var tmp = type.GetCustomAttributes(true);
            var tmp2 = child.GetCustomAttributes(true);

            return tmp.Union(tmp2)
                      .OfType<BindingBehaviorAttributeBase>()
                      .OrderBy(orderSelector)
                      .SelectMany(x => x.AllActions.OfType<TDelegate>());
        }
    }
}
