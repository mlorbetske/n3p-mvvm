using System.Collections.Generic;

namespace N3P.MVVM
{
    internal interface IServiceProviderProvider
    {
        TService GetService<TService>();

        HashSet<IServiceProviderProvider> Parents { get; }
    }
}