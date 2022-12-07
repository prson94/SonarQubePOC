import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'metricConditionDisabledFilter' })
export class MetricConditionDisabledFilterPipe implements PipeTransform {


    transform(items: any[], invalidIds: string[], includeID?: string ): any {
        if (invalidIds && invalidIds.length > 0) {
            invalidIds = invalidIds.filter((x) => x !== includeID);
            var filtered = items.filter(function (item) {
                return invalidIds.indexOf(item.value) === -1;
            });

            items.forEach((x) => { if (x.ID === includeID) { x.disabled = false; } });

            return filtered;
        } else {
            return items;

        }
    }
}