namespace Demeter.FormComponent.AspNetCore.Extension
{
    public sealed class DemeterFormSettings
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string FormCollection { get; set; }
        public string ElasticSearchConnectionString { get; set; }
    }
}