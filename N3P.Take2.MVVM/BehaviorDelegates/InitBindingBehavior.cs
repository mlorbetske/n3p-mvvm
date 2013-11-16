using System;
using System.Reflection;

namespace N3P.MVVM.BehaviorDelegates
{
    public delegate void InitBindingBehavior(IServiceProvider modelServiceProvider, Func<PropertyInfo, IServiceProvider> specializedServiceProviderGetter, object model, Func<string, object> getProperty, Action<string, object> setProperty);
}
