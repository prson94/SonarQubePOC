namespace d360.core.entities
{
    public class FusionAttributeItem
    {
        public string ObjectType { get; set; }
        public int? ObjectID { get; set; }
        public string ParentObjectType { get; set; }
        public int? ParentObjectID { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
    }
}
