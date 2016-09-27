
import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { Http } from '@angular/http';
import { SelectItem } from 'primeng/primeng';

@Pipe({ name: 'dropdownItemToSelectItemPipe' })
export class DropdownItemToSelectItemPipe implements PipeTransform {
    transform(items: any): any {
        let selectlist: SelectItem[] = [];

        for (let item of items) {
            selectlist.push({ label: item.Text, value: item.Value });
        } 
        return selectlist;
    }
}