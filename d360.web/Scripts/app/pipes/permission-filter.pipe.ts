import { Pipe, PipeTransform } from '@angular/core';
import { ResponsibilityTypeRelationPermission } from '../models/responsibility-type.model';

@Pipe({ name: 'permissionFilter' })
export class PermissionFilterPipe implements PipeTransform {
    transform(items: ResponsibilityTypeRelationPermission[], category: string): any {
        if (!category || category.length == 0) {return items;}
        const search = category.toUpperCase();
        return items.filter((item) => item.Category === category);
    }
}