using System.Collections.Generic;

namespace N3P.MVVM
{
    internal interface IServiceProviderProvider
    {
        TService GetService<TService>();

        IEnumerable<TService> GetServices<TService>();
            
        HashSet<IServiceProviderProvider> Parents { get; }

        HashSet<IServiceProviderProvider> Children { get; }
    }
}