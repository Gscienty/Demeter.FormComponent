using System.Collections.Generic;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nest;

namespace Demeter.FormComponent.AspNetCore.Extension
{
    public static class ConfigurationExtension
    {
        public static void AddDemeterForm<TForm>(
            this IServiceCollection services,
            IConfiguration configuration,
            string formTheme)
            where TForm : DemeterForm, new()
        {
           IConfigurationSection section = configuration.GetSection("DemeterForm");
           
           services.Configure<Dictionary<string, DemeterFormSettings>>(options => {
               var themeSection = section.GetSection(formTheme);
                var settings = new DemeterFormSettings
                {
                    ConnectionString = themeSection.GetSection("ConnectionString").Value,
                    Database = themeSection.GetSection("Database").Value,
                    FormCollection = themeSection.GetSection("FormCollection").Value,
                    ElasticSearchConnectionString = themeSection
                        .GetSection("ElasticSearchConnectionString").Exists()
                        ? themeSection
                            .GetSection("ElasticSearchConnectionString").Value
                        : null
                };

                if (options.ContainsKey(formTheme) == false)
                {
                    options.Add(formTheme, settings);
                }
                else
                {
                    options[formTheme] = settings;
                }
           });

           services.AddSingleton<IFormStore<TForm>>(provider => {
                var option = provider
                .GetService<IOptions<Dictionary<string, DemeterFormSettings>>>()
                .Value[formTheme];
                var client = new MongoClient(option.ConnectionString);
                var database = client.GetDatabase(option.Database);
                var elasticSearch = option.ElasticSearchConnectionString == null
                    ? null
                    : new ElasticClient(
                        new ConnectionSettings(new Uri(option.ElasticSearchConnectionString))
                            .DefaultIndex(option.FormCollection)
                    );

                return new DemeterFormStore<TForm>(
                    database,
                    option.FormCollection,
                    elasticSearch
                );
           });
        }
    }
}