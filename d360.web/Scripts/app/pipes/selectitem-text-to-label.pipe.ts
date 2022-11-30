import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'selectItemTextToLabel' })
export class SelectItemTextToLabelPipe implements PipeTransform {
    transform(items: any): any {
        for (const item of items) {
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