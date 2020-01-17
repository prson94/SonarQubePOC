using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Scoring
{
    public class AllocationApiGetModel
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetTypeClass assetClassName { get; set; } = AssetTypeClass.BusinessAsset;
        [DataMember]
        public Guid assetTypeUid { get; set; }
        [DataMember]
        public string assetTypePath { get; set; }
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScoreType scoreType { get; set; } = ScoreType.Governance;
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public State state { get; set; } = State.Active;
    }

    public class AllocationApiUpsertModel
    {
        [DataMember]
        public Guid? assetTypeUid { get; set; }

        [DataMember]
        public ScoreType? scoreType { get; set; }
    }
}
