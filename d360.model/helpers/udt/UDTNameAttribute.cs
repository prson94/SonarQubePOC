using System;

namespace d360.model.DataAccessLayer.repositories
{
    internal class UDTNameAttribute : Attribute
    {
        public UDTNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
