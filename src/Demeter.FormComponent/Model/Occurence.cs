using System;

namespace Demeter.FormComponent.Model
{
    public class Occurence
    {
        public DateTime Instance { get; private set; }

        public Occurence() : this(DateTime.UtcNow) { }

        public Occurence(DateTime occurenceInstance)
        {
            this.Instance = occurenceInstance;
        }

        public override int GetHashCode()
        {
            return this.Instance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Occurence) obj);
        }
    }
}