using d360.core;
using d360.core.entities.Metric;
using d360.core.queue;
using d360.extensions.queue;
using Dapper;
using igx.jobs.scoreprocessor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class MeasureRemovedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
			var Db = GetCompanyContext();

			ExecutionRecord = getExecution(Db.Connection);
			var executionItems = getExecutionItems(Db.Connection, 0);

			if (executionItems.Count > 0)
			{ 
				var measureChangedModel = executionItems[0].GetPayload<MeasureRemovedModel>();

				if (measureChangedModel == null)
				{
					throw new ArgumentNullException("measureChangedModel","Cannot load score file from storage");
				}

				if (measureChangedModel.EffectiveEndDate.Date <= DateTime.UtcNow.Date)
				{			
					Db.CreateMeasureRemovedResultExecution(measureChangedModel.MetricAssetVersionUid);
					updateExecutionMarkingItemsAsComplete(Db.Connection, ExecutionRecord);
				}
				else
				{
					// Set a delay of processing until we reach this effective date.
					var queue = new AzureQueueSource();
					var timespan = measureChangedModel.EffectiveEndDate.Date.Subtract(DateTime.UtcNow.Date.AddDays(1));
					var minutesToAdd = new Random().Next(2, 7);
					timespan = timespan.Add(new TimeSpan(0, minutesToAdd, 0));
					await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), Info, timespan);
				}			
			}

        }
    }
}
