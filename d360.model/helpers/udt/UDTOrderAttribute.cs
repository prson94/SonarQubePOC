using System;

namespace d360.model.DataAccessLayer.repositories
{
    internal class UDTOrderAttribute : Attribute
    {
        public UDTOrderAttribute(int order)
        {
            Order = order;
        }

        public int Order { get; }
    }
}
