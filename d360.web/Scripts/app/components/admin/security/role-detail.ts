import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { SidePanelService } from '../../../services/side-panel.service';
import { ReadRole } from '../../../models/security.model';
import { SecurityService } from '../../../services/security.service';

/*global $localize*/

@Component({
	selector: 'role-detail',
	templateUrl: './role-detail.html',
	styleUrls: ['./role-detail.less'],
	encapsulation: ViewEncapsulation.None
})
export class RoleDetail implements OnChanges, OnDestroy {
	@Input() uid: string;
	@Input() isDetailsPage: boolean = false;
	@Output() onLinkClicked = new EventEmitter();

	isLoading: boolean = false;
	item: ReadRole;

	loadSub: Subscription;

	constructor(
		private securityService: SecurityService,
		private sidePanelService: SidePanelService,
		private router: Router) {
		this.sidePanelService.refreshSource$.subscribe(() => {
			this.loadData();
		});
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes && changes.relationshipTypeUid.previousValue !== changes.relationshipTypeUid.currentValue) {
			this.loadData();
		}
	}

	ngOnDestroy() {
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
	}

	loadData() {
		this.isLoading = true;
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
		//this.loadSub = this.relationshipTypeService.getRelationshipType(this.relationshipTypeUid)
		//	.subscribe((res) => {
		//		this.relationshipType = res[0];
		//		const uiModel = RelationshipType.ConvertToUIModeldata(this.relationshipType);
		//		this.formattedRelationshipTypeName = this.relationshipType.Subject.Name + " - " + this.relationshipType.Predicate.Name + " - " + this.relationshipType.Object.Name;

		//		this.hasEdit = !uiModel.IsEditDisabled;
		//		this.isLoading = false;
		//	});
	}
}
