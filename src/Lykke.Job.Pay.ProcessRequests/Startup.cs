using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Job.Pay.ProcessRequests.Core;
using Lykke.Job.Pay.ProcessRequests.Models;
using Lykke.Job.Pay.ProcessRequests.Modules;
using Lykke.JobTriggers.Extenstions;
using Lykke.Logs;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.Pay.ProcessRequests
{
    public class Startup
    {
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver =
                        new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", "Process Requests Job");
            });

            var builder = new ContainerBuilder();

            var appSettings = Configuration.LoadSettings<AppSettings>();

            var log = CreateLogWithSlack(services, appSettings);

            builder.RegisterModule(new JobModule(appSettings.CurrentValue.ProcessRequestJob, log));

            builder.AddTriggers();

            builder.Populate(services);

            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware("Process Request", ex => new ErrorResponse { ErrorMessage = "Technical problem" });

            app.UseMvc();

            app.UseSwagger(c =>

            {

                c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);

            });

            app.UseSwaggerUI(x =>

            {

                x.RoutePrefix = "swagger/ui";

                x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");

            });

            app.UseStaticFiles();

            appLifetime.ApplicationStopped.Register(() =>
            {
                ApplicationContainer.Dispose();
            });
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, IReloadingManager<AppSettings> appSettings)

        {

            var consoleLogger = new LogToConsole();

            var aggregateLogger = new AggregateLogger();



            aggregateLogger.AddLog(consoleLogger);



            // Creating slack notification service, which logs own azure queue processing messages to aggregate log

            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueIntegration.AzureQueueSettings

            {

                ConnectionString = appSettings.CurrentValue.ProcessRequestJob.Db.LogsConnString,

                QueueName = appSettings.CurrentValue.SlackNotifications.AzureQueue.QueueName

            }, aggregateLogger);



            var dbLogConnectionStringManager = appSettings.Nested(x => x.ProcessRequestJob.Db.LogsConnString);

            var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;



            // Creating azure storage logger, which logs own messages to concole log

            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))

            {

                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(

                    AzureTableStorage<LogEntity>.Create(dbLogConnectionStringManager, "JobPayProcessRequestsLog", consoleLogger),

                    consoleLogger);



                var slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(slackService, consoleLogger);



                var azureStorageLogger = new LykkeLogToAzureStorage(

                    persistenceManager,

                    slackNotificationsManager,

                    consoleLogger);



                azureStorageLogger.Start();



                aggregateLogger.AddLog(azureStorageLogger);

            }



            return aggregateLogger;

        }
    }
}