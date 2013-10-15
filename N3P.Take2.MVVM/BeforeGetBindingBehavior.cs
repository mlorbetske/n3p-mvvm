using System;

namespace N3P.MVVM
{
    public delegate void BeforeGetBindingBehavior(IServiceProvider serviceProvider, object model, string propertyName, object currentValue);
}