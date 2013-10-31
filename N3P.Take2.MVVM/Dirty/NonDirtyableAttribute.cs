using System;

namespace N3P.MVVM.Dirty
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NonDirtyableAttribute : Attribute
    {
    }
}
