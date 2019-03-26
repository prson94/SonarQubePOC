import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { SelectItem } from 'primeng/components/common/api';

@Pipe({ name: 'arraySelectItemPipe' })
export class ArrayToSelectItemPipe implements PipeTransform {
    transform(items: any): any {
        let selectlist: SelectItem[] = [];

        for (let item   of items) {
            let data: string[] = (item as string).split("!~!");
           if (data.length==2)
                selectlist.push({ label: data[0], value: data[1] });
            else
                selectlist.push({ label: item, value: item });
        }
        return selectlist;
    }
}