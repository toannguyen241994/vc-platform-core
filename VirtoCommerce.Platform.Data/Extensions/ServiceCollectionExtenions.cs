using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.ChangeLog;
using VirtoCommerce.Platform.Data.DynamicProperties;
using VirtoCommerce.Platform.Data.ExportImport;
using VirtoCommerce.Platform.Data.PushNotifications;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.Platform.Data.Settings;

namespace VirtoCommerce.Platform.Data.Extensions
{
    public static class ServiceCollectionExtenions
    {

        public static IServiceCollection AddPlatformServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PlatformDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("VirtoCommerce")));
            services.AddTransient<IPlatformRepository, PlatformRepository>();
            services.AddSingleton<Func<IPlatformRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<IPlatformRepository>());
            services.AddSingleton<ISettingsManager, SettingsManager>();
            services.AddSingleton<IPushNotificationManager, PushNotificationManager>();
            services.AddSingleton<IDynamicPropertyService, DynamicPropertyService>();
            services.AddSingleton<IDynamicPropertySearchService, DynamicPropertySearchService>();
            services.AddSingleton<IDynamicPropertyRegistrar, DynamicPropertyService>();
            var inProcessBus = new InProcessBus();
            services.AddSingleton<IHandlerRegistrar>(inProcessBus);
            services.AddSingleton<IEventPublisher>(inProcessBus);
            services.AddSingleton<IChangeLogService, ChangeLogService>();
            services.AddScoped<IPlatformExportImportManager, PlatformExportImportManager>();
            return services;

        }
    }
}
