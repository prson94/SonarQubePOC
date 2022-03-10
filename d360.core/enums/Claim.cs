namespace d360.core.enums
{
    public enum Claim
    {
        [Description("Allow permission to read.")]
        Read = 1,
        
        [Description("Allow permission to create.")]
        Create = 2,
        
        [Description("Allow permission to update.")]
        Update = 3,
        
        [Description("Allow permission to remove.")]
        Delete = 4
    }
}
