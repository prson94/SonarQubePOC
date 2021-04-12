namespace igx.jobs.fusionloadprocessor
{
    public enum Action
    {
        NotSet,
        Delete,
        Update
    }

    public class FusionAttributeTempTableValue
    {
        public Action Action { get; set; }

        public int FusionAttributeTypeID { get; set; }

        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public string SourceID { get; set; }


        public string ParentSourceID { get; set; }
        public bool DeletedBit { get; set; }

        public bool IsDeleted()
        {
            return Action == Action.Delete;
        }

        internal static Action ActionFromString(string actionString)
        {
            if (string.IsNullOrEmpty(actionString)) return Action.NotSet;

            switch (actionString.ToUpper())
            {
                case "D":
                    return Action.Delete;
                case "U":
                    return Action.Update;
                default:
                    return Action.NotSet;                    
            }
        }
    }
}
