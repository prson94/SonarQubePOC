import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AdminBaseComponent } from '../../components/admin/admin-base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SecondaryNavItem } from '../../models/secondaryNav.model';
import { ReadSecurityPolicy } from '../../models/security.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CompanySettingsService } from '../../services/settings.service';
import { PolicyList } from './policies/policy-list';
import { PoliciesSidePanelWrapper } from './policies/policies-sidepanel-wrapper';

@Component({
    selector: 'admin-policies',
	templateUrl: './policies.html',
	standalone: true,
	imports: [
		PolicyList,
		PoliciesSidePanelWrapper,
	],
})

export class Policies extends AdminBaseComponent implements OnInit, OnDestroy {
	selected: ReadSecurityPolicy;

	constructor(
		secondaryNavService: SecondaryNavService,
		headerBreadcrumbService: HeaderBreadcrumbService,
		titleService: Title,
		protected messagesService: MessagesObservableService,
		protected settingsService: CompanySettingsService) {
		super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
	}

	ngOnInit(): void {
		this.breadcrumbsService.clearBreadcrumbs();
		this.breadcrumbsService.showBreadcrumb(new Breadcrumb($localize`Administration`));
		this.breadcrumbsService.showBreadcrumb(new Breadcrumb($localize`Security`));
		this.breadcrumbsService.showBreadcrumb(new Breadcrumb($localize`Policies`));

		this.setBrowserTitle(this.breadcrumbsService.getTitleService(), $localize`Security`);

		this.secondaryNavService.setCurrentArea("Security", "fa-lock", null);

		this.secondaryNavService.clearItems();
		this.secondaryNavService.showItem(new SecondaryNavItem(`Roles`, 'Roles', null, `/admin/security/roles`, null, 1));
		this.secondaryNavService.showItem(new SecondaryNavItem(`Security Policies`, 'SecurityPolicies', null, `/admin/security/policies`, null, 2));
		this.secondaryNavService.showHeader(true);
	}

	selectedItemChange(event) {
		this.selected = event;
	}

	ngOnDestroy() {
		this.clearSidebar();
	}

	get sidePanelStorageKey() {
		return 'configuration_admin_security_policies';
	}
}