using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using d360.core;
using d360.core.entities;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;

namespace d360.jobs.MatchTechnicalMappingsToBusinessMappings
{
    public class MatchModel
    {
        public int MapRuleItemID { get; set; }
        public int SourceFusionAttributeID { get; set; }
        public int TargetFusionAttributeID { get; set; }
        public int? MapRuleID { get; set; }
        public int SourceIntersectID { get; set; }
        public int TargetIntersectID { get; set; }
        public int? MapID { get; set; }         //to be filled out
        public int? MapItemID { get; set; }     //to be filled out
    }

    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveDevelopmentCompanyIDs();

#if DEBUG                       
                companies = GetActiveDevelopmentCompanyIDs().Where(i => i == 39).ToList();
#endif

                companies.ForEach(companyID =>
                {
                    try
                    {
                        #region Create EF connection

                        var sec = new UriSecurityContextProvider()
                        {
                            CompanyID = companyID,
                            ResourceID = 0,
                            CompanyPrefix = "demo.dev",
                            IsAdministrator = true
                        };
                        var cache = new DummyCachingProvider();
                        var queue = new AzureQueueSource();
                        var community = new CommunityContext(cache, queue, sec);
                        var company = new CompanyContext(community, cache, queue, sec, true);

                        #endregion

                        Console.WriteLine("Getting objects with ripe matches [company id: {0}]", companyID);

                        #region Get Items to process

                        var items = company.Query<MatchModel>(@"
declare @mapRuleItems table (MapRuleItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int, MapRuleID int)
insert into @mapRuleItems
select	I.ID as MapRuleItemID,
		I.SourceFusionAttributeID,
		I.TargetFusionAttributeID,
		R.ID as MapRuleID
from	MapRuleItem I
		left join MapRuleItemMapRule J on J.MapRuleItemID = I.ID
		left join MapRule R on R.ID = J.MapRuleID
where	I.ID not in (select MapRuleItemID from MapRuleItemMapItem)

select	MRI.MapRuleID,
	MRI.MapRuleItemID,
	MRI.SourceFusionAttributeID,
	MRI.TargetFusionAttributeID,
	NULL as MapID,
	NULL as MapItemID,
	SI.SubjectID as SourceIntersectID,
	TI.SubjectID as TargetIntersectID
from	@mapRuleItems MRI 
	inner join [Intersect] SI on SI.Subject = 'Intersect' and SI.Object = 'FusionAttribute' and SI.ObjectID = MRI.SourceFusionAttributeID 
	inner join [Intersect] TI on TI.Subject = 'Intersect' and TI.Object = 'FusionAttribute' and TI.ObjectID = MRI.TargetFusionAttributeID
union
select	J.MapRuleID,
	J.MapRuleItemID,
	MRI.SourceFusionAttributeID,
	MRI.TargetFusionAttributeID,
	J2.MapID,
	J2.MapItemID,
	MI.SourceIntersectID,
	MI.TargetIntersectID
from	MapRule MR
	inner join MapRuleItemMapRule J on J.MapRuleID = MR.ID
	inner join MapRuleItem MRI on MRI.ID = J.MapRuleItemID and MRI.ID not in (select MapRuleItemID from @mapRuleItems)
	inner join MapRuleItemMapItem BM on BM.MapRuleItemID = MRI.ID
	inner join MapItem MI on MI.ID = BM.MapItemID
	inner join MapItemMap J2 on J2.MapItemID = MI.ID
where	MR.ID in (select MapRuleID from @mapRuleItems)").ToList();

                      #endregion

                        Console.WriteLine("Found {0} item(s) with ripe matches [company id: {1}]", items.Count, companyID);

                        #region

                        foreach (var item in items)
                        {
                            // First, create the map item records.
                            if (!item.MapItemID.HasValue)
                            {
                                try
                                {
                                    var mapItem = new MapItem { SourceIntersectID = item.SourceIntersectID, TargetIntersectID = item.TargetIntersectID };
                                    company.Add(mapItem);
                                    item.MapItemID = mapItem.ID;
                                }
                                catch
                                {
                                    Console.WriteLine("Error when attempting to add map item company id: {0}.  Source Intersect: {1}, Target Intersect: {2}.  MapRuleItemID: {3}", companyID, item.SourceIntersectID, item.TargetIntersectID, item.MapRuleItemID);
                                }
                            }

                            //Second, tie the map item to map rule item.
                            if (item.MapItemID.HasValue)
                            {
                                var joinRecord = company.Filter<MapRuleItemMapItem>(i => i.MapItemID == item.MapItemID && i.MapRuleItemID == item.MapRuleItemID).SingleOrDefault();
                                if (joinRecord == null)
                                {
                                    joinRecord = new MapRuleItemMapItem { MapItemID = item.MapItemID.Value, MapRuleItemID = item.MapRuleItemID };
                                    company.Add(joinRecord);
                                }
                            }
                        }
                        
                        #endregion

                        var uniqueMapRuleIDs = items.Select(i => i.MapRuleID).Distinct().ToList();

                        foreach (var uniqueMapRuleID in uniqueMapRuleIDs)
                        {
                            var setItems = items.Where(i => i.MapRuleID == uniqueMapRuleID).ToList();

                            foreach (var setItem in setItems)
                            {
                                if (!setItem.MapID.HasValue)
                                {
                                    var matchingSetItem = setItems.FirstOrDefault(i => i.MapID.HasValue &&
                                        (
                                        (i.SourceFusionAttributeID != setItem.SourceFusionAttributeID && i.TargetFusionAttributeID == setItem.TargetFusionAttributeID && i.TargetIntersectID == setItem.TargetIntersectID) ||
                                        (i.SourceFusionAttributeID == setItem.SourceFusionAttributeID && i.TargetFusionAttributeID != setItem.TargetFusionAttributeID && i.SourceIntersectID == setItem.SourceIntersectID)
                                        )
                                    );
                                    if (matchingSetItem != null)
                                    {
                                        setItem.MapID = matchingSetItem.MapID;
                                        try
                                        {
                                            company.Execute($"insert into MapItemMap values (@map, @mapItem)", new { map = setItem.MapID, mapItem = setItem.MapItemID });
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Error when attempting to associate map to map item for company id: {0}.  Map: {1}, Map Item: {2}.", companyID, setItem.MapID, setItem.MapItemID);
                                        }
                                    }
                                }
                            }
                        }
                  }
                  catch (Exception ex)
                  {
                      Console.WriteLine(ex.GetFullExceptionData());
                  }

              });
            }
            catch (Exception ex)
            {
                mex.Add(ex);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
