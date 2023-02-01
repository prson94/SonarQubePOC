import { Component, OnDestroy } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { RelationshipType } from '../../../models/relationship.model';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
	selector: 'd3s-admin-relationships-component',
	templateUrl: 'admin-relationships.component.html'
})

export class AdminRelationshipsComponent extends AdminBaseComponent implements OnDestroy {
	selected: RelationshipType;

	constructor(
		secondaryNavService: SecondaryNavService,
		protected messagesService: MessagesObservableService,
		protected settingsService: CompanySettingsService,
		headerBreadcrumbService: HeaderBreadcrumbService,
		titleService: Title) {
		super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
	}

	selectedItemChange(event) {
		this.selected = event;
		this.baseIntersectTypeUid = this.selected.Uid;
		this.buildSecondaryNavigation({
			intersectTypeUid: this.baseIntersectTypeUid,
			forceRefresh: true,
			excludeTabs: true
		});
	}

	ngOnDestroy() {
		this.clearSidebar();
	}

	get sidePanelStorageKey() {
		return 'configuration_admin_relationship';
	}
}