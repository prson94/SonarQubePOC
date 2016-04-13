using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.ReportTile, "ReportTile")]
    public class ReportTile : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "ReportTileName_Description"), StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public int ReportID { get; set; }
        
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ContentAreaNumber_Name", Description = "ContentAreaNumber_Description")]
        public int ContentAreaNumber { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "CommandText_Name", Description = "CommandText_Description")]
        public string CommandText { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ReportTileType_Name", Description = "ReportTileType_Description")]
        public ReportTileType ReportTileType { get; set; }

        [DataMember]
        public string Settings { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Report Report { get; set; }

        public string GetSettingsPropertyValue(string propertyName)
        {
            var value = "";

            if (!string.IsNullOrEmpty(Settings))
            {
                try
                {
                    var settingsXml = XElement.Parse(Settings);
                    var p = settingsXml.Element(propertyName);
                    if (p != null)
                    {
                        value = p.Value;
                    }
                }
                catch
                { }
            }

            return value;
        }
    }
}
