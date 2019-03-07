import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { SelectItem } from 'primeng/components/common/api';

@Pipe({ name: 'selectItemTextToLabel' })
export class SelectItemTextToLabelPipe implements PipeTransform {
    transform(items: any): any {
        for (let item of items) {
            if (item.label == undefined && item.Text != undefined) {
                item.label = item.Text;
            }
            if (item.value == undefined && item.Value != undefined) {
                item.value = item.Value;
            }
        }
        return items;
    }
}