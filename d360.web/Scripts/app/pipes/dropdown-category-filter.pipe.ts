import { Pipe, PipeTransform } from '@angular/core';
import { SelectItemGroup } from 'primeng/api';

@Pipe({ name: 'dropdownCategoryFilter' })
export class DropdownCategoryGroupPipe implements PipeTransform {
	transform(items: SelectItemGroup[], relIdx: number): any {
		let selectlist: SelectItemGroup[] = [];
		if (!relIdx) {
			return items;
		}
		else {
			selectlist = items.filter((x) => x.label.endsWith(relIdx.toLocaleString()));
		}
        return selectlist;
    }
}