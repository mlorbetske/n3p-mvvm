using System;

namespace N3P.Take2.MVVM
{
    public delegate object AfterGetBindingBehavior(IServiceProvider serviceProvider, object model, string propertyName, object currentValue);
}