using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities.Metric
{
    public class MeasureDefinitionSupportedTokensAttribute : Attribute
    {
        public MeasureDefinitionValueToken[] Tokens { get; set; }
        public MeasureDefinitionSupportedTokensAttribute(params MeasureDefinitionValueToken[] tokens)
        {
            Tokens = tokens;
        }
    }

    #region Enums

    public enum GovernanceMeasureCheck
    { 
        External = 0 ,
        Field = 1,
        Ownership = 2,
        Predicate = 3,
        Relationship = 4
    }

    public enum GovernanceMeasureOperator
    {
        [Display(Name = "is"), MeasureDefinitionSupportedTokensAttribute(MeasureDefinitionValueToken.Blank, MeasureDefinitionValueToken.Null)]
        Equals = 1,
        [Display(Name = "is not"), MeasureDefinitionSupportedTokensAttribute(MeasureDefinitionValueToken.Blank, MeasureDefinitionValueToken.Null)]
        NotEquals = 2,
        [Display(Name = "greater than"), MeasureDefinitionSupportedTokensAttribute()]
        GreaterThan = 3,
        [Display(Name = "greater than or equals"), MeasureDefinitionSupportedTokensAttribute()]
        GreaterThanEquals = 4,
        [Display(Name = "less than"), MeasureDefinitionSupportedTokensAttribute()]
        LessThan = 5,
        [Display(Name = "less than or equals"), MeasureDefinitionSupportedTokensAttribute()]
        LessThanEquals = 6,
        [Display(Name = "in"), MeasureDefinitionSupportedTokensAttribute()]
        In = 7,
        [Display(Name = "not in"), MeasureDefinitionSupportedTokensAttribute()]
        NotIn = 8
    }

    public enum MeasureDefinitionValueToken
    {
        [Display(Name = "[null]")]
        Null = 1,
        [Display(Name = "[blank]")]
        Blank = 2
    }

    public enum MeasureResultOperation
    {
        [Display(Name = "Average")]
        Average = 1,
        [Display(Name = "Maximum")]
        Max = 2,
        [Display(Name = "Minimum")]
        Minimum = 3
    }

    #endregion

    public class DataQualityMeasureDefinition
    {
        public MeasureResultOperation ResultOperation { get; set; }
    }

    public class GovernanceMeasureDefinition
    {
        public GovernanceMeasureCheck Check { get; set; }
        public Guid? TypeUid { get; set; }
        public GovernanceMeasureOperator Operator { get; set; }
        public string Value { get; set; }
        public string RangeStartValue { get; set; }
        public string RangeEndValue { get; set; }
        public List<string> Values { get; set; }
    }

    public class PerceptualMeasureDefinition
    {
        public Guid QuestionTypeUid { get; set; }
        public int NumberOfSurveysToConsider { get; set; }
    }

    public class RollupMeasureDefinition
    {
        public MeasureResultOperation ResultOperation { get; set; }
        public bool CrossDescendancy { get; set; }
    }

    public class UserMeasureDefinition
    {
        public MeasureResultOperation ResultOperation { get; set; }
    }
}