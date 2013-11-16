using System;

namespace N3P.MVVM.BehaviorDelegates
{
    public delegate void BeforeGetBindingBehavior(IServiceProvider serviceProvider, object model, string propertyName, object currentValue);
}