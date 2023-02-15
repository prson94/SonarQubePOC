import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';

import { isEqual } from "lodash-es";

enum IgInfoButtonSize {
	CHIP = 'chip',
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
	private readonly emptyUid = '00000000-0000-0000-0000-000000000000';
	isSelected: boolean;
	isVisible: boolean;
	tooltipText: string = $localize`View Information`;
	@Input() size: IgInfoButtonSize = IgInfoButtonSize.L;
	@Input() object: { objectID: string, fieldName: string };
    @Input() selected: { objectID: string, fieldName: string };

	ngOnChanges(changes: SimpleChanges): void {
		if (changes.object || changes.selected) {
			this.isVisible = this.object.objectID && this.object.objectID !== String(0) && this.object.objectID !== this.emptyUid;
			if (this.isVisible) {
				this.isSelected = isEqual(this.selected, this.object);
			}
		}
	}
}
