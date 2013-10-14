using System;
using System.Diagnostics;

namespace N3P.Take2.MVVM.Logging
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LogAttribute : BindingBehaviorAttributeBase
    {
        private class LoggingService
        {
            public LogEvents LogEvents { get; set; }
        }

        public LogEvents LogEvents { get; set; }

        private readonly LoggingService _service;

        public override Type ServiceType
        {
            get { return typeof(LoggingService); }
        }

        public override object Service
        {
            get { return _service; }
        }

        public LogAttribute(LogEvents events = LogEvents.BeforeGet | LogEvents.AfterGet | LogEvents.BeforeSet | LogEvents.AfterSet)
            : base(BeforeGet, AfterGet, BeforeSet, AfterSet)
        {
            _service = new LoggingService
            {
                LogEvents = events
            };
        }

        public override bool IsGlobalServiceOnly
        {
            get { return true; }
        }

        public static void AfterSet(IServiceProvider serviceProvider, object model, string propertyName, object proposedValue, ref object currentValue, bool changed)
        {
            var loggingService = serviceProvider.GetService<LoggingService>();

            if (!loggingService.LogEvents.HasFlag(LogEvents.AfterSet))
            {
                return;
            }

            Debug.WriteLine("After set {0}.{1} -- {2} => {3} : {4}", model.GetType(), propertyName, proposedValue ?? "(null)", currentValue ?? "(null)", changed);
        }

        public static BeforeSetAction BeforeSet(IServiceProvider serviceProvider, object model, string propertyName, ref object proposedValue, ref object currentValue)
        {
            var loggingService = serviceProvider.GetService<LoggingService>();

            if (!loggingService.LogEvents.HasFlag(LogEvents.BeforeSet))
            {
                return BeforeSetAction.Accept;
            }

            Debug.WriteLine("Before set {0}.{1} -- {2} => {3}", model.GetType(), propertyName, currentValue ?? "(null)", proposedValue ?? "(null)");
            return BeforeSetAction.Accept;
        }

        public static object AfterGet(IServiceProvider serviceProvider, object model, string propertyName, object currentValue)
        {
            var loggingService = serviceProvider.GetService<LoggingService>();

            if (!loggingService.LogEvents.HasFlag(LogEvents.AfterGet))
            {
                return currentValue;
            }

            Debug.WriteLine("After get {0}.{1} -- {2}", model.GetType(), propertyName, currentValue ?? "(null)");
            return currentValue;
        }

        public static void BeforeGet(IServiceProvider serviceProvider, object model, string propertyName, object currentValue)
        {
            var loggingService = serviceProvider.GetService<LoggingService>();

            if (!loggingService.LogEvents.HasFlag(LogEvents.BeforeGet))
            {
                return;
            }
            
            Debug.WriteLine("Before get {0}.{1} -- {2}", model.GetType(), propertyName, currentValue ?? "(null)");
        }
    }
}
