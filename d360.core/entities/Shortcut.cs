using d360.core.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
	public class Shortcut : BaseIntObject
    {
        [DataMember, Column(TypeName = "varchar"), StringLength(250)]
        public string Name { get; set; }
        
        [DataMember, Column(TypeName = "VARCHAR"), StringLength(50)]
        public string Icon { get; set; }
        
        [DataMember, Column(TypeName = "VARCHAR"), StringLength(250)]
        public string IconUrl { get; set; }
        
        [DataMember, Column(TypeName = "VARCHAR"), StringLength(250)]
        public string Url { get; set; }

        [DataMember, StringLength(500)]
        public string Description { get; set; }

        [DataMember, StringLength(100)]
        public string IconColor { get; set; }

        [DataMember, StringLength(100)]
        public string TitleColor { get; set; }

        [DataMember, StringLength(100)]
        public string BackgroundColor { get; set; }

		[DataMember]
		public int DisplayOrder { get; set; }

		[DataMember]
		public LinkTarget LinkTarget { get; set; }

        [DataMember, NotMapped]
        public string IconPayload { get; set; }

		[DataMember, NotMapped]
		public string FullURL { get; set; }
	}
}
