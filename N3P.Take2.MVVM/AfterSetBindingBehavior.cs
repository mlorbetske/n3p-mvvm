using System;

namespace N3P.Take2.MVVM
{
    public delegate void AfterSetBindingBehavior(IServiceProvider serviceProvider, object model, string propertyName, object proposedValue, ref object currentValue, bool changed);
}