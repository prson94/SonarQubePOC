namespace d360.core.entities
{
    public class UIColumnDefinition
    {
        public UIColumnDefinition()
        {
            ColumnType = "string";
            Width = 10;
            SortGroup = "A";
        }

        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string ColumnType { get; set; }
        public int Width { get; set; }
        public string SortGroup { get; set; }
    }
}
