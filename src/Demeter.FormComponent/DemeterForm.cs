using Demeter.FormComponent.Model;
using MongoDB.Bson;
using Nest;

namespace Demeter.FormComponent
{
    public abstract class DemeterForm
    {
        public string Id { get; set; }
        public Occurence CreateOn { get; private set; }
        public Occurence DeleteOn { get; private set; }

        public DemeterForm() : this(ObjectId.GenerateNewId().ToString())
        {
            this.CreateOn = new Occurence();
        }

        public DemeterForm(string id)
        {
            this.Id = id;
        }

        public void Delete()
        {
            this.DeleteOn = new Occurence();
        }

        public static TForm QueryHitTransfer<TForm>(IHit<TForm> hit)
            where TForm : DemeterForm
        {
            TForm source = hit.Source;
            source.Id = hit.Id;
            return source;
        }
    }
}