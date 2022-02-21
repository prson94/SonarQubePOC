using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace d360.core.entities.Process
{
    public class NodeData : Dictionary<string, string>
    {
        //other keys are custom fields

        private readonly string[] systemFields = new string[] {
            "key","assetTypeName", "assetTypeUid","category",
            "icon","key","loc","refItemColor",
            "isNew","Uid","AssetTypeUid",
            "hasError","objectId","governanceDisplayValue",
            "relCount","isInvalid"
        };

        public string GetHash()
        {
            var data = JsonConvert.SerializeObject(CustomFields);
            return data;
        }

        public Dictionary<string, string> CustomFields
        {
            get
            {
                Dictionary<string, string> ret = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> entry in this)
                {
                    if (!systemFields.Contains(entry.Key))
                    {
                        ret.Add(entry.Key, entry.Value);
                    }
                }
                return ret;
            }
        }

        public void UpdateAssetUid(Guid uid)
        {
            this["key"] = uid.ToString();
        }

        public Guid AssetUid
        {
            get
            {
                return Guid.Parse(this["key"]);
            }
        }

        public Guid AssetTypeUid
        {
            get
            {
                return Guid.Parse(this["assetTypeUid"]);
            }
        }

        public decimal? StepNo
        {
            get
            {
                if (!ContainsKey("StepNo"))
                {
                    return null;
                }

                decimal.TryParse(this["StepNo"], out decimal value);
                return value;
            }
        }

        public bool IsNodeValid
        {
            get
            {
                if (ContainsKey("isInvalid"))
                {
                    return false;
                }

                return true;
            }
        }

        public bool HasAssetType
        {
            get
            {
                return ContainsKey("assetTypeUid");
            }
        }
    }

    public class LinkData
    {
        public Guid from { get; set; }

        public Guid to { get; set; }

        public string fromPort { get; set; }

        public string toPort { get; set; }

        public string label { get; set; }

        public Guid? labelUid { get; set; }

        public IList<double> points { get; set; }
    }

    public class ProcessDiagramModel
    {
        public string @class { get; set; }

        public IList<NodeData> nodeDataArray { get; set; }

        public IList<LinkData> linkDataArray { get; set; }

        public string linkFromPortIdProperty { get; set; }

        public string linkToPortIdProperty { get; set; }
    }
    public class ValidationError
    {
        public Guid AssetTypeUid { get; set; }

        public Guid AssetUid { get; set; }

        public string Error { get; set; }

        public string ErrorType { get; set; }

        public string AssetName { get; set; }
    }

    public class ProcessDiagramBadge
    {
        public Guid AssetUid { get; set; }

        public int RelationshipCount { get; set; }
    }

    public class ProcessDiagramReplaceModel
    {
        public Guid assetFrom { get; set; }

        public Guid assetTo { get; set; }
    }

    public class ProcessDiagramCopyRelationshipModel
    {
        public Guid keyUid { get; set; }

        public int IntersectId { get; set; }

        public string Location { get; set; }

        public int SubjectCardinality { get; set; }

        public int ObjectCardinality { get; set; }
    }

    public class ProcessDiagramCopyMapper
    {
        public Guid oldUid { get; set; }

        public Guid keyUid { get; set; }
    }
}
