import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { RelationshipLookupDefinition, RelationshipLookupFieldDefinition } from '../../../../models/fieldtype-api.model';
import { FieldsObservableService } from '../../../../services/fieldsObservable.service';

/*global $localize*/

@Component({
	selector: 'd3s-relation-lookup-detail',
	templateUrl: './relation-lookup-detail.component.html',
	styleUrls: ['./relation-lookup-detail.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class RelationLookupDetailComponent implements OnChanges {
	@Input() assetTypeUid: string;
	@Input() fieldName: string;

	definition: RelationshipLookupDefinition;
	loading: boolean = false;

	referencedFields: RelationshipLookupFieldDefinition[] = [];
	sortFields: RelationshipLookupFieldDefinition[] = [];
	filterFields: RelationshipLookupFieldDefinition[] = [];

	constructor(
		private fieldService: FieldsObservableService,
		private cdRef: ChangeDetectorRef
	) {
	}

	ngOnChanges(changes: SimpleChanges): void {

		if (changes) {
			if (changes.assetTypeUid && changes.assetTypeUid.currentValue !== changes.assetTypeUid.previousValue) {
				this.load();
			}
			if (changes.fieldName && changes.fieldName.currentValue !== changes.fieldName.previousValue) {
				this.load();
			}
		}

	}

	load() {
		this.loading = true;
		this.fieldService.getComplexFieldDefinition(this.assetTypeUid, this.fieldName)
			.subscribe((res) => {
				this.definition = res;

				this.definition.fields.forEach((field) => {
					if (field.FieldTypeName === '_assetPath') {
						field.FieldTypeName = $localize`Asset Path`;
					}
					if (field.FieldTypeName === 'DisplayValue') {
						field.FieldTypeName = $localize`Display Value`;
					}
					if (field.FieldTypeName.startsWith('Related Item')) {
						field.FieldTypeName = $localize`Relationship: ` + field.RelationshipTypeName;
					}
					if (field.OverrideDisplayName) {
						field.FieldTypeName = field.OverrideDisplayName;
					}
				});

				this.referencedFields = this.definition.fields.filter((x) => x.Show);
				this.filterFields = this.definition.fields.filter((x) => x.Filter);
				this.sortFields = this.definition.fields.filter((x) => x.SortOrder !== 0);
				
				this.loading = false;
				this.cdRef.markForCheck();
			});
	}
}
