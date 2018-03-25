namespace igx.jobs.fusionruleprocessor
{
    public class FusionRuleStepMappingModel
    {
        public int RuleStepID { get; set; }
        public string SourceFieldName { get; set; }
        public string TargetFieldName { get; set; }
        public int SourceFieldTypeID { get; set; }
        public int TargetFieldTypeID { get; set; }
        public bool IsConstantValue { get; set; }
        public string ConstantValue { get; set; }
    }
}
