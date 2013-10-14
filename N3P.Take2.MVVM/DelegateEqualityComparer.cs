using System;
using System.Collections.Generic;

namespace N3P.Take2.MVVM
{
    public class DelegateEqualityComparer<TDelegate> : IEqualityComparer<TDelegate>
    {
        public static readonly IEqualityComparer<TDelegate> Default = new DelegateEqualityComparer<TDelegate>();

        public bool Equals(Delegate x, Delegate y)
        {
            return x.Method == y.Method;
        }

        public int GetHashCode(Delegate obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(TDelegate x, TDelegate y)
        {
            return Equals((Delegate)(object)x, (Delegate)(object)y);
        }

        public int GetHashCode(TDelegate obj)
        {
            return GetHashCode((Delegate)(object)obj);
        }
    }
}