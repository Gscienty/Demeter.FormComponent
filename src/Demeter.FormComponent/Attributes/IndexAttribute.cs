using System;

namespace Demeter.FormComponent.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class IndexAttribute : Attribute
    {
        private readonly IndexType _indexType;

        public IndexType IndexType => this._indexType;

        public bool IsUnique { get; set; } = false;
        public string Combine { get; set; }

        public IndexAttribute(IndexType type)
        {
            this._indexType = type;
        }
    }
}