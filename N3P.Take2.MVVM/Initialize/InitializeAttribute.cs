using System;
using System.Reflection;

namespace N3P.MVVM.Initialize
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class InitializeAttribute : BindingBehaviorAttributeBase
    {
        private class InitializationConfig
        {
            public bool HasRun { get; set; }

            public string StaticInitializationMethodName { get; set; }

            public string InitializationParametersStaticPropertyName { get; set; }

            public Func<object> DefaultValue { get; set; }
        }

        public override Type ServiceType
        {
            get { return typeof (InitializationConfig); }
        }

        private readonly InitializationConfig _service;

        public override object GetService(object model)
        {
            return _service;
        }

        public InitializeAttribute(string staticInitializationMethodName = "", string initializationParametersStaticPropertyName = "")
            : base(afterGet: AfterGetBindingBehavior, afterGetPriority: int.MinValue)
        {
            _service = new InitializationConfig
            {
                InitializationParametersStaticPropertyName = initializationParametersStaticPropertyName,
                StaticInitializationMethodName = staticInitializationMethodName
            };
        }

        private static object AfterGetBindingBehavior(IServiceProvider serviceprovider, object model, string propertyname, object currentvalue)
        {
            var cfg = serviceprovider.GetService<InitializationConfig>();
            var prop = model.GetType().GetProperty(propertyname);
            var propType = prop.PropertyType;

            if (IsDefault(propType, currentvalue))
            {
                if (cfg.HasRun)
                {
                    return cfg.DefaultValue();
                }

                cfg.HasRun = true;
                object[] initializationParameters = null;

                if (!string.IsNullOrEmpty(cfg.InitializationParametersStaticPropertyName))
                {
                    var initProp = propType.GetProperty(cfg.InitializationParametersStaticPropertyName, BindingFlags.Public | BindingFlags.Static);
                    
                    initProp = initProp ?? model.GetType().GetProperty(cfg.InitializationParametersStaticPropertyName, BindingFlags.Public | BindingFlags.Static);

                    if (initProp.PropertyType != typeof (object[]))
                    {
                        return (cfg.DefaultValue = () => initProp.GetValue(null, null))();
                    }

                    initializationParameters = (object[]) initProp.GetValue(null, null);
                }

                if (!string.IsNullOrEmpty(cfg.StaticInitializationMethodName))
                {
                    var initMethod = propType.GetMethod(cfg.StaticInitializationMethodName, BindingFlags.Public | BindingFlags.Static);

                    initMethod = initMethod ?? model.GetType().GetMethod(cfg.StaticInitializationMethodName, BindingFlags.Public | BindingFlags.Static);

                    return (cfg.DefaultValue = () => initMethod.Invoke(null, initializationParameters))();
                }

                return (cfg.DefaultValue = () => Activator.CreateInstance(propType, initializationParameters))();
            }

            return currentvalue;
        }

        private static readonly MethodInfo GetDefaultMethod = ((Func<object>)GetDefault<object>).Method.GetGenericMethodDefinition();

        private static T GetDefault<T>()
        {
            return default(T);
        }

        private static bool IsDefault(Type type, object value)
        {
            return Equals(GetDefaultMethod.MakeGenericMethod(type).Invoke(null, null), value);
        }
    }
}
