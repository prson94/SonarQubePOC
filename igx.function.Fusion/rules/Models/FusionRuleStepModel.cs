using System.Collections.Generic;

namespace igx.function.Fusion.rules
{
    public class FusionRuleStepModel
    {
        public FusionRuleStepModel()
        {
            Settings = new Dictionary<string, string>();
            Mappings = new List<FusionRuleStepMappingModel>();
        }

        public int ID { get; set; }
        public int RuleID { get; set; }
        public int Step { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public Dictionary<string,string> Settings { get; set; }
        public List<FusionRuleStepMappingModel> Mappings { get; set; }
    }
}
