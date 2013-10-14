using System;

namespace N3P.Take2.MVVM
{
    public static class ServiceProviderExtensions
    {
        public static TService GetService<TService>(this IServiceProvider serviceProvider)
        {
            return (TService) serviceProvider.GetService(typeof (TService));
        }
    }
}