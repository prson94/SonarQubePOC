import {
	ChangeDetectionStrategy,
	Component,
	Input,
	OnChanges,
	SimpleChanges
} from '@angular/core';

import * as _ from 'lodash';

enum IgInfoButtonSize {
	M = 'm',
	L = 'l'
}

@Component({
    selector: 'asset-editor-field-info',
    templateUrl: './asset-editor-field-info.component.html',
	styleUrls: ['./asset-editor-field-info.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetEditorFieldInfoComponent implements OnChanges {
	isSelected: boolean;
	@Input() size: IgInfoButtonSize = IgInfoButtonSize.L;
	@Input() item: any;
    @Input() selected: any;

	ngOnChanges(changes: SimpleChanges): void {
		if (changes.item || changes.selected) {
			this.isSelected = _.isEqual(this.selected, this.item);
		}
	}
}
