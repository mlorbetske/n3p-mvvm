using System;

namespace N3P.Take2.MVVM
{
    public delegate BeforeSetAction BeforeSetBindingBehavior(IServiceProvider serviceProvider, object model, string propertyName, ref object proposedValue, ref object currentValue);
}