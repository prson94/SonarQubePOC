using System;
using System.Collections.Generic;

namespace d360.core.entities.Metric
{
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
        Equals = 1,
        NotEquals = 2,
        GreaterThan = 3,
        LessThan = 4
    }

    public enum MeasureResultOperation
    {
        Average = 1,
        Maximum = 2,
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
        public Guid Uid { get; set; }
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