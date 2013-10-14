using System;
using System.Collections.Generic;

namespace N3P.Take2.MVVM
{
    public abstract class BindingBehaviorAttributeBase : Attribute
    {
        private readonly List<Delegate> _behaviors = new List<Delegate>();

        public IEnumerable<Delegate> AllActions
        {
            get { return _behaviors; }
        }

        public virtual Type ServiceType { get { return null; } }

        public virtual object Service { get { return null; } }
        public virtual bool IsGlobalServiceOnly { get { return false; } }

        protected BindingBehaviorAttributeBase(BeforeGetBindingBehavior beforeGet = null, AfterGetBindingBehavior afterGet = null, BeforeSetBindingBehavior beforeSet = null, AfterSetBindingBehavior afterSet = null)
        {
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