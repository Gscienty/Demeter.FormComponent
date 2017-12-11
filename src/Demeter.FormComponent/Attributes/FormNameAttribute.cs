using System;

namespace Demeter.FormComponent.Attributes
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class FormNameAttribute : Attribute
    {
        public string Name { get; private set; }
        public bool Searching { get; set; }

        public FormNameAttribute(string name)
        {
            this.Name = name;
            this.Searching = false;
        }
    }
}