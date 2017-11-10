namespace d360.core.entities
{
    public partial class GenerateAssetTypeSqlModel
    {
        public string JoinStatement { get; set; }

        public string ColumnStatement { get; set; }

        public string SortStatement { get; set; }

        public string Name { get; set; }

        public int ColumnOrder { get; set; }

        public int SortOrder { get; set; }

        public bool IsListable { get; set; }
    }
}
