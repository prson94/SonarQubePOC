import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Model } from '../models/model.model';


@Pipe({ name: 'modelType' })
export class ModelTypePipe implements PipeTransform {
    transform(items: Model[], type: string): any {
        if (!type || type.length == 0) return items;

        let search = type.toLowerCase();

        return items.filter((item) => item.TaxonomyTypeClass && item.TaxonomyTypeClass.toLowerCase().includes(search));
    }
}