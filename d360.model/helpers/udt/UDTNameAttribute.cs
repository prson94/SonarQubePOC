using System;

namespace d360.model.DataAccessLayer.repositories
{
    class UDTNameAttribute : Attribute
    {
        public UDTNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
