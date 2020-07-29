import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { MetricFieldTypeViewModel } from '../models/metrics.model';

@Pipe({ name: 'metricConditionDisabledFilter' })
export class MetricConditionDisabledFilterPipe implements PipeTransform {


    transform(items: any[], invalidIds: number[]): any {
        var filtered = items.filter(function (item) {
            return invalidIds.indexOf(+item.value) === -1;
        });
        return filtered
    }
}