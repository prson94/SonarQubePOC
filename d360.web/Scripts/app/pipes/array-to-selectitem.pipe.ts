import { Pipe, PipeTransform } from '@angular/core';
import { SelectItem } from 'primeng/api';

@Pipe({ name: 'arraySelectItemPipe' })
export class ArrayToSelectItemPipe implements PipeTransform {
    transform(items: any, option: string): any {
        const selectlist: SelectItem[] = [];
        const useLabelAsValue: boolean = option && option.toLowerCase() === 'labelasval';
        for (const item of items) {
            const data: string[] = (item as string).split("!~!");
            if (data.length === 2)
                {selectlist.push({ label: data[0], value: useLabelAsValue ? data[0] : data[1] });}
            else
                {selectlist.push({ label: item, value: item });}
        }
        return selectlist;
    }
}