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
            : base(init: Initialize, initPriority: int.MinValue)
        {
            _service = new InitializationConfig
            {
                InitializationParametersStaticPropertyName = initializationParametersStaticPropertyName,
                StaticInitializationMethodName = staticInitializationMethodName
            };
        }

        private static void Initialize(IServiceProvider modelServiceProvider, Func<PropertyInfo, IServiceProvider> specializedServiceProviderGetter, object model, Func<string, object> getProperty, Action<string, object> setProperty)
        {
            foreach (var prop in model.GetType().GetProperties())
            {
                var attrs = prop.GetCustomAttributes(typeof (InitializeAttribute), false);
                
                if (attrs.Length == 0)
                {
                    continue;
                }

                var val = GetInitValue(specializedServiceProviderGetter(prop), model, prop);
                setProperty(prop.Name, val);
            }
        }

        private static object GetInitValue(IServiceProvider serviceprovider, object model, PropertyInfo prop)
        {
            var cfg = serviceprovider.GetService<InitializationConfig>();
            var propType = prop.PropertyType;

            cfg.HasRun = true;
            object[] initializationParameters = null;

            if (!string.IsNullOrEmpty(cfg.InitializationParametersStaticPropertyName))
            {
                var initProp = propType.GetProperty(cfg.InitializationParametersStaticPropertyName, BindingFlags.Public | BindingFlags.Static);

                initProp = initProp ?? model.GetType().GetProperty(cfg.InitializationParametersStaticPropertyName, BindingFlags.Public | BindingFlags.Static);

                if (initProp == null)
                {
                    var val = cfg.InitializationParametersStaticPropertyName;
                    return (cfg.DefaultValue = () => val)();
                }

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
    }
}
