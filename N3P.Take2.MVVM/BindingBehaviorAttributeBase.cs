using System;
using System.Collections.Generic;

namespace N3P.MVVM
{
    public abstract class BindingBehaviorAttributeBase : Attribute
    {
        private readonly List<Delegate> _behaviors = new List<Delegate>();

        public IEnumerable<Delegate> AllActions
        {
            get { return _behaviors; }
        }

        public virtual Type ServiceType { get { return null; } }

        public virtual object GetService(object model) { return null; }
        public virtual bool IsGlobalServiceOnly { get { return false; } }

        internal int BeforeGetPriority { get; private set; }
        internal int AfterGetPriority { get; private set; }
        internal int BeforeSetPriority { get; private set; }
        internal int AfterSetPriority { get; private set; }

        protected BindingBehaviorAttributeBase(BeforeGetBindingBehavior beforeGet = null, AfterGetBindingBehavior afterGet = null, BeforeSetBindingBehavior beforeSet = null, AfterSetBindingBehavior afterSet = null, int beforeGetPriority = int.MaxValue / 2, int afterGetPriority = int.MaxValue / 2, int beforeSetPriority = int.MaxValue / 2, int afterSetPriority = int.MaxValue / 2)
        {
            BeforeGetPriority = beforeGetPriority;
            AfterGetPriority = afterGetPriority;
            BeforeSetPriority = beforeSetPriority;
            AfterSetPriority = afterSetPriority;

            if (beforeGet != null)
            {
                _behaviors.Add(beforeGet);
            }

            if (afterGet != null)
            {
                _behaviors.Add(afterGet);
            }

            if (beforeSet != null)
            {
                _behaviors.Add(beforeSet);
            }

            if (afterSet != null)
            {
                _behaviors.Add(afterSet);
            }
        }
    }
}