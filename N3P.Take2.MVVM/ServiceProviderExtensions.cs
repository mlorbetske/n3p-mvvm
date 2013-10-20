using System;

namespace N3P.MVVM
{
    public static class ServiceProviderExtensions
    {
        public static TService GetService<TService>(this IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                return default(TService);
            }

            return (TService) serviceProvider.GetService(typeof (TService));
        }
    }
}