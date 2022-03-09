using System;

namespace d360.core.entities.Contracts
{
    /// <summary>
    /// This interface is used in the DbContext when checking whether object have these two fields present.  
    /// If so, this tells the DbContext to update these values with the current date and resource ID.  
    /// These values are then used on the table triggers to add an audit record.
    /// This should be used only on customer-specific objects!
    /// </summary>
    public interface ICreatedMetadata
    {
        DateTime? CreatedOn { get; set; }

        int? CreatedBy { get; set; }
    }
}
