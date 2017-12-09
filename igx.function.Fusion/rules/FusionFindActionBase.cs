namespace igx.function.Fusion.rules
{
    public class FusionFindActionBase : FusionActionBase
    {
        public string FindSearchObject { get; set; }
        public int FindSearchObjectID { get; set; }
        public int FindParent { get; set; }
                
        protected void LoadCommonFindSettings()
        {
            if (Step.Settings.ContainsKey("Object"))
            {
                FindSearchObject = Step.Settings["Object"];
            }

            if(Step.Settings.ContainsKey("ObjectID"))
            {
                if (int.TryParse(Step.Settings["ObjectID"], out int searchObjectID))
                    FindSearchObjectID = searchObjectID;                
            }

            if (Step.Settings.ContainsKey("FindParent"))
            {             
                if (int.TryParse(Step.Settings["FindParent"], out int parent))
                    FindParent = parent;
            }
        }
    }
}
