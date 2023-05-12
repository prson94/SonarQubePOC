import { Component, Input, OnInit } from '@angular/core';

@Component({
	selector: 'd3s-relation-lookup-field-type-editor',
	templateUrl: './relation-lookup-field-type-editor.component.html',
	styleUrls: ['./relation-lookup-field-type-editor.component.less']
})
export class RelationLookupFieldTypeEditorComponent implements OnInit {
	@Input() uid: string;
	@Input() assetTypeUid: string;

	constructor() { }

	ngOnInit(): void {
	}

}
