using d360.core.entities;

namespace igx.functions.Queue.fusion
{
    public class FusionChangeTableValue
    {        
        public FusionChangeTableValue(Field fieldVal, string oldValue, string action)
        {
            FusionAttributeID = fieldVal.ObjectID;
            Value = fieldVal.Value;            
            Action = action;
            this.OldValue = oldValue;
            FieldTypeID = fieldVal.FieldTypeID;
        }

        public FusionChangeTableValue(FusionAttributeTempTableValue x, string oldValue, string action)
        {
            this.Action = action;
            this.OldValue = oldValue;
            this.Value = x.Name;
            this.FusionAttributeID = x.ID;
        }

        public int FusionAttributeID { get; set; }
        public int FieldTypeID { get; set; }
        public string Value { get; set; }
        public string OldValue { get; set; }
        public string Action { get; set; }
    }
}