using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class ConnectorLabelPostModel
    {
        public string Value { get; set; }
    }

    public class ConnectorLabelApiModel
    {

        [DataMember]
        public Guid uid { get; set; }

        [DataMember, StringLength(250)]
        public string Value { get; set; }

        [DataMember]
        public int UseCount { get; set; }

        [DataMember]
        public Guid? CreatedByUid { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public Guid? UpdatedByUid { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }
    }

    public class ConnectorLabelApiDeleteModel
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public bool cascade { get; set; }
    }

    public class ConnectorLabelApiModelWrapper
    {
        [DataMember]
        public int? pageSize { get; set; }

        [DataMember]
        public int? pageNum { get; set; }

        [DataMember]
        public int total { get; set; } = 0;

        [DataMember]
        public IEnumerable<ConnectorLabelApiModel> items { get; set; }
    }
}
