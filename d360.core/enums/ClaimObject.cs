namespace d360.core.enums
{
    public enum ClaimObject
    {
        [Description("Grants a claim on the object.")]
        Root = 1,
        
        [Description("Grants a claim on the object's underlying metadata attributes.")]
        Attribute = 2,
        
        [Description("Grants a claim on the object's underlying owners.")]
        Governance = 3,
        
        [Description("Grants a claim on the object's underlying relationships.")]
        Relationship = 4
    }
}
