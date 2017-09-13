using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using Dapper;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Design.PluralizationServices;
using System.Linq;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<Score> Score { get; set; }

        public DbSet<ScoreMetric> ScoreMetrics { get; set; }

        public DbSet<ScoreType> ScoreTypes { get; set; }

        public DbSet<ScoreTypeMetric> ScoreTypeMetrics { get; set; }

        public DbSet<ScoreTypeMetricVersion> ScoreTypeMetricVersions { get; set; }

        public DbSet<Statistic> Statistics { get; set; }

        public DbSet<StatisticType> StatisticTypes { get; set; }

        public DbSet<StatisticTypeCheckOption> StatisticTypeCheckOptions { get; set; }

        #endregion

        #region Engine Methods

        public List<StatisticDetail> GetStatisticDetailsByType(SystemObjects type, int id)
        {
            return Query<StatisticDetail>($"GetStatisticDetails '{type.ToString()}', {id}").ToList();
        }

        public IEnumerable<dynamic> GetStatisticTypeRollupCheckOptions()
        {
            var sql = @"select * from (
			select	'ArtifactType|' + cast(ID as varchar(15)) as ID, 'Artifacts :: ' + Name as Name from ArtifactType
			) O order by Name";
            return Query<dynamic>(sql).ToList();
        }

        public ObjectStatisticTileModel GetObjectStatistics(SystemObjects type, int id)
        {
            var model = new ObjectStatisticTileModel { Items = new List<ObjectStatisticTileItemModel>() };

            var list = Database.Connection.Query<RawObjectStatistic>("[tile].[GetObjectStatistics] @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).ToList();
            
            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

            list.ForEach(i =>
            {
                switch (i.Group)
                {
                    case "Comments":
                        model.CommentCount = i.Value;                        
                        model.CommentLast = i.MostRecent;
                        break;
                    case "Followers":
                        model.FollowerCount = i.Value;                        
                        break;
                    case "Score":
                        model.Score = i.Value;                        
                        break;
                    case "Issues":
                        model.IssueCount = i.Value;                        
                        model.IssueLast = i.MostRecent;
                        break;
                    default:
                        model.Items.Add(new ObjectStatisticTileItemModel { Count = i.Value, Name = pluralize.Pluralize(i.Name ?? ""), TypeID = i.TypeID });
                        break;
                }
            });

            return model;
        }

        #endregion
    }
}
