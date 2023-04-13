import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RelationshipType } from '../../../../models/relationship.model';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { RelationshipsService } from '../../../../services/relationships.service';
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

	constructor(
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		breadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService
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
				this.isLoading = false;
			}
		);
	}
}
