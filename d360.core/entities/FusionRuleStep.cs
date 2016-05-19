using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("RuleStep", Schema = "fusion")]
    public class FusionRuleStep : BaseIntObject
    {
        [DataMember]
        public int RuleID { get; set; }        
        [DataMember]
        public int Step { get; set; }
        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string Action { get; set; }
        [DataMember]
        public string Description { get; set; }

        [ForeignKey("RuleID")]
        public virtual FusionRule FusionRule { get; set; }
                
        private ICollection<FusionRuleStepSetting> _settings;

        [IgnoreDataMember, ForeignKey("RuleStepID")]
        public virtual ICollection<FusionRuleStepSetting> FusionRuleStepSettings
        {
            get { return _settings ?? (_settings = new Collection<FusionRuleStepSetting>()); }
            set { _settings = value; }
        }


        private ICollection<FusionRuleStepMapping> _mappings;

        [IgnoreDataMember, ForeignKey("RuleStepID")]
        public virtual ICollection<FusionRuleStepMapping> FusionRuleStepMappings
        {
            get { return _mappings ?? (_mappings = new Collection<FusionRuleStepMapping>()); }
            set { _mappings = value; }
        }

        public string GetSettingValueByName(string name)
        {
            var setting = this.FusionRuleStepSettings.SingleOrDefault(x => string.Compare(x.Name, name, true) == 0);

            return setting == null ? string.Empty : setting.Value;
        }
    }
}
