using AutoMapper;
using InvoiceTrackerAPI2.Mappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvoiceTrackerAPI2.Tests.Helpers;

public static class MapperFactory
{
    public static IMapper Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<InvoiceMappingProfile>());
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
