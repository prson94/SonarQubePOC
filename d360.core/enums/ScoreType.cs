using System.ComponentModel;

namespace d360.core.enums
{
    public enum ScoreType
    {
        [Name("Governance Score"), ReadOnly(false), Description("")]
        Governance = 1,
        [Name("Data Quality Score"), ReadOnly(true), Description("")]
        DataQuality = 2,
        [Name("Perceptional Score"), ReadOnly(true), Description("")]
        Perceptional = 3
    }    
}