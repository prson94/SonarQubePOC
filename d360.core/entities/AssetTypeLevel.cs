using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetTypeLevel : BaseObject
    {
        [
            DataMember, 
            Key, 
            Column(Order = 1), 
            Display(ResourceType = typeof(resources.Fields), 
                Name = "AssetType_Name", 
                Description = "AssetType_Description")
        ]
        public int AssetTypeID { get; set; }

        [
            DataMember, 
            Key, 
            Column(Order = 2), 
            Display(ResourceType = typeof(resources.Fields), 
                Name = "Level_Name", 
                Description = "Level_Description")
        ]
        public int Level { get; set; }

        [        
            DataMember, 
            Display(ResourceType = typeof(resources.Fields), 
                Name = "Name_Name", 
                Description = "Name_Description"),
            Required(AllowEmptyStrings = false, 
                ErrorMessageResourceType = typeof(resources.Fields), 
                ErrorMessageResourceName = "Name_ErrorRequired"), 
            StringLength(250)
        ]
        public string Name { get; set; }

        [
            DataMember, 
            Display(ResourceType = typeof(resources.Fields), 
            Name = "Description_Name", 
            Description = "Description_Description")
        ]
        public string Description { get; set; }

        #region Navigation Properties

        [
            IgnoreDataMember, 
            ForeignKey("AssetTypeID"), 
            Display(ResourceType = typeof(resources.Fields), 
            Name = "AssetType_Name", 
            Description = "AssetType_Description")
        ]
        public virtual AssetType AssetType { get; set; }

        #endregion
    }

    public class AssetTypeLevelApiViewModel
    {
        [DataMember]
        public int Level { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}
