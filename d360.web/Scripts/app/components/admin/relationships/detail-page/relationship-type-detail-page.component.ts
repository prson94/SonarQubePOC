import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IOutputData } from 'angular-split';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { LinkClickInterceptor } from '../../../../services/href-click-service';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { SidePanelService } from '../../../../services/side-panel.service';
import { BaseComponent } from '../../../shared/base.component';

/*global $localize*/

@Component({
	selector: 'd3s-relationship-type-detail-page',
	templateUrl: './relationship-type-detail-page.component.html',
	styleUrls: ['./relationship-type-detail-page.component.less'],
	encapsulation: ViewEncapsulation.None
})
export class RelationshipTypeDetailPageComponent extends BaseComponent implements OnInit, OnDestroy {
	relationshipTypeUid: string = '';
	private sub: any;

	uid: string;
	assetType: { Name: string };

	sidePanelStorageKey: string = '';
	selectedItem: Record<string, object>;

	sidePanelOpen = false;
	selectedForInfoPanel: unknown;

	constructor(
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		breadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService,
		public sidePanelService: SidePanelService,
		private linkClickInterceptor: LinkClickInterceptor
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = breadcrumbService;
	}

	ngOnDestroy() {
		if (this.sub) {
			this.sub.unsubscribe();
		}
	}

	ngOnInit() {
		this.isLoading = true;
		this.sub = this.route.params.subscribe(
			(params) => {
				this.relationshipTypeUid = this.baseIntersectTypeUid = params['uid'];
				this.buildSecondaryNavigation({ intersectTypeUid: this.relationshipTypeUid });
				this.sidePanelStorageKey = "side_panel_relationship_type_Details_" + this.relationshipTypeUid;
				this.isLoading = false;
			}
		);
	}

	onLinkClicked($event) {
		if ($event) {
			this.selectedItem = $event;
			this.sidePanelService.setSidePanelState({ expanded: true });
		}
	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}
}
