import { Pipe, PipeTransform } from '@angular/core';
import { SelectItem } from 'primeng/api';

@Pipe({ name: 'dropdownItemToSelectItemPipe' })
export class DropdownItemToSelectItemPipe implements PipeTransform {
    transform(items: any): any {
        const selectlist: SelectItem[] = [];

        for (const item of items) {
            selectlist.push({ label: item.Text, value: item.Value });
        } 
        return selectlist;
    }
}