using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Senders;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.SendGrid;
using VirtoCommerce.NotificationsModule.Smtp;
using VirtoCommerce.NotificationsModule.Web.Infrastructure;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Notifications;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.NotificationsModule.Web
{
    public class Module : IModule
    {
        public ManifestModuleInfo ModuleInfo { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            var snapshot = serviceCollection.BuildServiceProvider();
            var configuration = snapshot.GetService<IConfiguration>();
            serviceCollection.AddDbContext<NotificationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("VirtoCommerce")));
            serviceCollection.AddTransient<INotificationRepository, NotificationRepository>();
            serviceCollection.AddSingleton<Func<INotificationRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<INotificationRepository>());
            serviceCollection.AddSingleton<INotificationService, NotificationService>();
            serviceCollection.AddSingleton<INotificationRegistrar, NotificationService>();
            serviceCollection.AddSingleton<INotificationSearchService, NotificationSearchService>();
            serviceCollection.AddSingleton<INotificationMessageService, NotificationMessageService>();
            serviceCollection.AddSingleton<INotificationSender, NotificationSender>();
            serviceCollection.AddSingleton<INotificationTemplateRenderer, LiquidTemplateRenderer>();
            serviceCollection.AddSingleton<INotificationMessageSenderProviderFactory, NotificationMessageSenderProviderFactory>();
            serviceCollection.AddTransient<INotificationMessageSender, SmtpEmailNotificationMessageSender>();
            serviceCollection.AddTransient<INotificationMessageSender, SendGridEmailNotificationMessageSender>();
            serviceCollection.AddTransient<IEmailSender, EmailNotificationMessageSender>();

            serviceCollection.Configure<EmailSendingOptions>(configuration.GetSection("Notifications"));
            serviceCollection.Configure<SmtpSenderOptions>(configuration.GetSection("Notifications:Smtp"));
            serviceCollection.Configure<SendGridSenderOptions>(configuration.GetSection("Notifications:SendGrid"));
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            AbstractTypeFactory<Notification>.RegisterType<EmailNotification>().MapToType<NotificationEntity>();
            AbstractTypeFactory<Notification>.RegisterType<SmsNotification>().MapToType<NotificationEntity>();
            AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>().MapToType<NotificationTemplateEntity>();
            AbstractTypeFactory<NotificationTemplate>.RegisterType<SmsNotificationTemplate>().MapToType<NotificationTemplateEntity>();
            AbstractTypeFactory<NotificationMessage>.RegisterType<EmailNotificationMessage>().MapToType<NotificationMessageEntity>();
            AbstractTypeFactory<NotificationMessage>.RegisterType<SmsNotificationMessage>().MapToType<NotificationMessageEntity>();
            AbstractTypeFactory<NotificationEntity>.RegisterType<EmailNotificationEntity>();
            AbstractTypeFactory<NotificationEntity>.RegisterType<SmsNotificationEntity>();

            var permissionsProvider = appBuilder.ApplicationServices.GetRequiredService<IKnownPermissionsProvider>();
            permissionsProvider.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions.Select(x =>
                new Permission()
                {
                    GroupName = "Notifications",
                    ModuleId = ModuleInfo.Id,
                    Name = x
                }).ToArray());

            var mvcJsonOptions = appBuilder.ApplicationServices.GetService<IOptions<MvcJsonOptions>>();
            mvcJsonOptions.Value.SerializerSettings.Converters.Add(new PolymorphicJsonConverter());

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                using (var notificationDbContext = serviceScope.ServiceProvider.GetRequiredService<NotificationDbContext>())
                {
                    notificationDbContext.Database.EnsureCreated();
                    notificationDbContext.Database.Migrate();
                }
            }

            var configuration = appBuilder.ApplicationServices.GetService<IConfiguration>();
            var notificationGateway = configuration.GetSection("Notifications:Gateway").Value;
            var notificationMessageSenderProviderFactory = appBuilder.ApplicationServices.GetService<INotificationMessageSenderProviderFactory>();
            switch (notificationGateway)
            {
                case "SendGrid":
                    notificationMessageSenderProviderFactory.RegisterSenderForType<EmailNotification, SendGridEmailNotificationMessageSender>();
                    break;
                default:
                    notificationMessageSenderProviderFactory.RegisterSenderForType<EmailNotification, SmtpEmailNotificationMessageSender>();
                    break;
            }

        }

        public void Uninstall()
        {
        }
    }
}
