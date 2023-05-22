import { Component, Input } from '@angular/core';

@Component({
	selector: 'd3s-relation-lookup-field-type-editor',
	templateUrl: './relation-lookup-field-type-editor.component.html',
	styleUrls: ['./relation-lookup-field-type-editor.component.less']
})
export class RelationLookupFieldTypeEditorComponent {
	@Input() uid: string;
	@Input() assetTypeUid: string;
}
