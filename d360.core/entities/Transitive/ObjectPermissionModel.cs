namespace d360.core.entities
{
    public class ObjectPermissionModel
    {
        public string Role { get; set; }
        public SystemObjects Type { get; set; }
        public int ID { get; set; }
        public bool AllowCreate { get; set; }
        public bool AllowRead { get; set; }
        public bool AllowUpdate { get; set; }
        public bool AllowDelete { get; set; }
    }
}
