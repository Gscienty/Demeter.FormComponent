using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Nest;
using Demeter.FormComponent.Attributes;

namespace Demeter.FormComponent.AspNetCore.Extension
{
    public static class ConfigurationExtension
    {
        public static void AddDemeterForm<TForm>(this IServiceCollection services, string formName)
            where TForm : DemeterForm, new()
        {
            IConfiguration configuration = services
                .BuildServiceProvider()
                .GetService<IConfiguration>()
                .GetSection("DemeterForm");

            DemeterFormSettings settings = null;
            
            if (configuration.GetSection(formName).Exists())
            {
                settings = ConfigurationExtension.GetDemeterFormSettings<TForm>(
                    services,
                    configuration.GetSection(formName)
                );
            }
            else if (configuration.GetSection("default").Exists())
            {
                settings = ConfigurationExtension.GetDemeterFormSettings<TForm>(
                    services,
                    configuration.GetSection("default")
                );
            }

            if (settings == null)
            {
                return;
            }

            services.AddSingleton<IFormStore<TForm>>(provider =>
            {
                var client = new MongoClient(settings.ConnectionString);
                var database = client.GetDatabase(settings.Database);
                var elasticSearch = settings.ElasticSearchConnectionString == null
                    ? null
                    : new ElasticClient(
                        new ConnectionSettings(new Uri(settings.ElasticSearchConnectionString))
                            .DefaultIndex(settings.FormCollection)
                    );

                return new DemeterFormStore<TForm>(database, settings.FormCollection, elasticSearch);
            });

            services.AddSingleton<FormManager<TForm>>(provider => new FormManager<TForm>(provider));
        }

        private static DemeterFormSettings GetDemeterFormSettings<TForm>(
            IServiceCollection services,
            IConfigurationSection section)
        where TForm : DemeterForm, new()
        {
            var settings = new DemeterFormSettings();

            if (section.GetSection("ConnectionString").Exists())
            {
                settings.ConnectionString = section.GetSection("ConnectionString").Value;
            }
            else
            {
                return null;
            }

            if (section.GetSection("Database").Exists())
            {
                settings.Database = section.GetSection("Database").Value;
            }
            else 
            {
                return null;
            }

            FormNameAttribute formName = typeof(TForm).GetCustomAttribute<FormNameAttribute>();
            if (formName != null)
            {
                settings.FormCollection = formName.Name;
            }
            else if (section.GetSection("FormCollection").Exists())
            {
                settings.FormCollection = section.GetSection("FormCollection").Value;
            }
            else
            {
                return null;
            }

            if (formName != null && formName.Searching)
            {
                settings.ElasticSearchConnectionString = section
                    .GetSection("ElasticSearchConnectionString").Exists()
                    ? section
                        .GetSection("ElasticSearchConnectionString").Value
                    : null;
            }

            return settings;
        }
    }

}