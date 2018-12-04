import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { MetricFieldTypeViewModel } from '../models/metrics.model';

@Pipe({ name: 'metricConditionDisabledFilter' })
export class MetricConditionDisabledFilterPipe implements PipeTransform {
    transform(items: MetricFieldTypeViewModel[]): any {
        return items.filter(item => item.Disabled === false);
    }
}