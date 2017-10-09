using Demeter.FormComponent;
using MongoDB.Bson.Serialization.Attributes;

namespace Demeter.FileComponent
{
    public class DemeterFile : DemeterForm
    {
        public string MimeType { get; set; }

        [BsonIgnore]
        public byte[] Content { get; set; }

        public DemeterFile() : base() { }

        public DemeterFile(string id) : base(id) { }
    }
}