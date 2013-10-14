using System;

namespace N3P.Take2.MVVM
{
    public delegate void BeforeGetBindingBehavior(IServiceProvider serviceProvider, object model, string propertyName, object currentValue);
}