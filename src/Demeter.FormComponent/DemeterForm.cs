using Demeter.FormComponent.Model;
using MongoDB.Bson;

namespace Demeter.FormComponent
{
    public abstract class DemeterForm
    {
        public string Id { get; private set; }
        public Occurence CreateOn { get; private set; }
        public Occurence DeleteOn { get; private set; }

        public DemeterForm()
        {
            this.Id = ObjectId.GenerateNewId().ToString();
            this.CreateOn = new Occurence();
        }

        public void Delete()
        {
            this.DeleteOn = new Occurence();
        }
    }
}