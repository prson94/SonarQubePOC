import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { SelectItem } from 'primeng/primeng';

@Pipe({ name: 'arraySelectItemPipe' })
export class ArrayToSelectItemPipe implements PipeTransform {
    transform(items: any): any {
        let selectlist: SelectItem[] = [];

        for (let item of items) {
            selectlist.push({ label: item, value: item });
        }
        return selectlist;
    }
}