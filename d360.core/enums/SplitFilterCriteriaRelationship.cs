namespace d360.core.enums
{
    public enum SplitFilterCriteriaRelationship
    {
        [Description("When match with object")]
        Object = 1,
        
        [Description("When match with subject")]
        Subject = 2,
        
        [Description("When match with both object and subject")]
        Both = 3,
    }
}
