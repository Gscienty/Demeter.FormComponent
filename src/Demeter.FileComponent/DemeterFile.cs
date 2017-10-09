using Demeter.FormComponent;
using MongoDB.Bson.Serialization.Attributes;

namespace Demeter.FileComponent
{
    public class DemeterFile : DemeterForm
    {
        public string MimeType { get; private set; }

        [BsonIgnore]
        public byte[] Content { get; private set; }

        public DemeterFile() : base() { }
    }
}