import { Pipe, PipeTransform } from '@angular/core';
import { createWriteStream } from 'fs';
import { isString, isObject, isArray } from 'util';

@Pipe({
    name: 'simpleSearch'
})
export class SimpleSearch implements PipeTransform {
    transform(items: any, filter: string, defaultFilter: boolean): any {
        if (!filter) {
            return items;
        }

        if (!Array.isArray(items)) {
            return items;
        }

        const loop = (items) => {
            if (isString(items.Name) && items.Name.toLowerCase().indexOf(filter.toLowerCase()) != -1)
                return true

            if (isArray(items.Items)) {
                let tempItems = [];
                return (tempItems = items.Items.filter(loop)).length;
            }
        }
        return items.filter(x => loop(x));
    }
}