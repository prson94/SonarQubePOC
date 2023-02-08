import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { RelationshipType } from '../../../../models/relationship.model';
import { RelationshipsService } from '../../../../services/relationships.service';
import { SidePanelService } from '../../../../services/side-panel.service';

/*global $localize*/

@Component({
	selector: 'd3s-relationship-type-detail',
	templateUrl: './relationship-type-detail.component.html',
	styleUrls: ['./relationship-type-detail.component.less'],
	encapsulation: ViewEncapsulation.None
})
export class RelationshipTypeDetailComponent implements OnChanges {
	@Input() relationshipTypeUid: string;
	@Output() onLinkClicked = new EventEmitter();

	isLoading: boolean = false;
	relationshipType: RelationshipType;
	hasEdit: boolean = false;

	formattedRelationshipTypeName: string = "";
	constructor(
		private relationshipTypeService: RelationshipsService,
		private sidePanelService: SidePanelService,
		private router: Router) { }

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.relationshipTypeUid.previousValue !== changes.relationshipTypeUid.currentValue) {
			this.loadData();
		}
	}

	loadData() {
		this.isLoading = true;
		this.relationshipTypeService.getRelationshipType(this.relationshipTypeUid)
			.subscribe((res) => {
				this.relationshipType = res[0];
				this.formattedRelationshipTypeName = this.relationshipType.Object.Name + " - " + this.relationshipType.Predicate.Name + " - " + this.relationshipType.Subject.Name;

				this.hasEdit = !this.relationshipType.HasRelationships;
				this.isLoading = false;
			});
	}

	getAssetTypeClassFriendlyName(cs: string) {
		const friendlyNames = {
			"BusinessAsset": $localize`Business Asset`,
			"TechnicalAsset": $localize`Technical Asset`,
			"Model": $localize`Model`,
			"Policy": $localize`Policy`,
			"Rule": $localize`Rule`,
			"DiagramAsset": $localize`Diagram Asset`
		};

		return friendlyNames[`${cs}`];
	}


	open(newTab: boolean = false) {
		const url = `/admin/relationships/${this.relationshipType.Uid}/fields`;
		if (newTab) {
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(url);
		}
	}

	resourceClicked(uid: string) {
		this.onLinkClicked.emit({ uid, type: 'Resource' });
	}

	editClick() {
		this.sidePanelService.editClick(this.relationshipType);
	}
}
