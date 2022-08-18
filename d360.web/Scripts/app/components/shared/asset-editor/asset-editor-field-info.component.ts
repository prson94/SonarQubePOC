import {
	ChangeDetectionStrategy,
	Component,
	Input,
	OnChanges,
	SimpleChanges
} from '@angular/core';

import * as _ from 'lodash';

enum IgInfoButtonSize {
	S = 's',
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
	tooltipText: string = $localize`View Information`;
	@Input() size: IgInfoButtonSize = IgInfoButtonSize.L;
	@Input() object: { objectID: string, fieldName: string };
    @Input() selected: { objectID: string, fieldName: string };

	ngOnChanges(changes: SimpleChanges): void {
		if (changes.object || changes.selected) {
			this.isSelected = _.isEqual(this.selected, this.object);
		}
	}
}
