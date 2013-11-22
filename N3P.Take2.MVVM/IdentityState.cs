using System;
using System.Collections.Generic;

namespace N3P.MVVM
{
    public class IdentityState : IExportedState
    {
        private readonly object _value;

        public IdentityState(object value)
        {
            _value = value;
        }

        public IEnumerable<string> Keys { get { return new string[0]; } }

        public object this[string key]
        {
            get { throw new KeyNotFoundException(); }
        }

        public int Count { get { return 0; } }

        public object Apply()
        {
            return _value;
        }

        public override bool Equals(object obj)
        {
            var state = obj as IExportedState;

            if (state != null)
            {
                return state.Equals(_value);
            }

            return Equals(obj, _value);
        }

        public override int GetHashCode()
        {
            return _value != null ? _value.GetHashCode() : 0;
        }
    }
}
