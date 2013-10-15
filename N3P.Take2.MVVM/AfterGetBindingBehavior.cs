using System;

namespace N3P.MVVM
{
    public delegate object AfterGetBindingBehavior(IServiceProvider serviceProvider, object model, string propertyName, object currentValue);
}