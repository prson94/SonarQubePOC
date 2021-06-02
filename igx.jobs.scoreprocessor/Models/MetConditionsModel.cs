using System;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.Models
{
    internal class MetConditionsModel
    {
        public bool ConditionMet { get; set; }
        public decimal? SelectedWeight { get; set; }
        public float? SelectedThreshold { get; set; }
        public Guid? SelectedConditionUid
        {
            get
            {
                if (Conditions.Count > 0)
                {
                    return Conditions[0].ConditionUid;
                }
                else
                {
                    return null;
                }
            }
        }
        public List<MetConditionModel> Conditions { get; set; } = new List<MetConditionModel>();

        public List<Guid> ExtraneousConditions
        {
            get
            {
                var uids = new List<Guid>();
                if (Conditions != null)
                {
                    for (int i = 0; i < Conditions.Count; i++)
                    {
                        if (i > 0)
                        {
                            uids.Add(Conditions[i].ConditionUid);
                        }
                    }
                }
                return uids;
            }
        }
    }
}
