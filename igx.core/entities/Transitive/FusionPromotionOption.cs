namespace d360.core.entities
{
    public class FusionPromotionOption
    {
        /// <summary>
        /// The Type of object we will promote to.
        /// </summary>
        public string PromotionObjectType { get; set; }

        /// <summary>
        /// The Object Type ID that we will promote to.  If 0, then there is no specific type, just a general promotion to that table.
        /// </summary>
        public int PromotionObjectID { get; set; }
        
        /// <summary>
        /// The name of the type we are promoting to.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The object type ID we need to look up in order to assigned a required parent.
        /// </summary>
        public int? ParentObjectTypeID { get; set; }
    }
}
