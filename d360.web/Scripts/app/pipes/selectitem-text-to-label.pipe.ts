import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'selectItemTextToLabel' })
export class SelectItemTextToLabelPipe implements PipeTransform {
    transform(items: any): any {
        for (const item of items) {
            if (item.label == null && item.Text != null) {
                item.label = item.Text;
            }
            if (item.value == null && item.Value != null) {
                item.value = item.Value;
            }
        }
        return items;
    }
}