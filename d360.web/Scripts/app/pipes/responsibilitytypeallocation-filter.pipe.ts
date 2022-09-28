import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { ResponsibilityTypeRelationAllocationOption } from '../models/responsibility-type.model';

@Pipe({ name: 'responsibilityallocationFilter' })
export class ResponsibilityTypeRelationAllocationOptionFilterPipe implements PipeTransform {
    transform(items: ResponsibilityTypeRelationAllocationOption[]): any {
        return items.filter((item) => !item.IsUsed);
    }
}