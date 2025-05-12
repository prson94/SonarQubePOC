import { Component, OnDestroy, OnInit } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { AdminBaseComponent } from "../../components/admin/admin-base.component";
import { Breadcrumb } from "../../models/breadcrumb.model";
import { SecondaryNavItem } from "../../models/secondaryNav.model";
import { ReadRole } from "../../models/security.model";
import { HeaderBreadcrumbService } from "../../services/header-breadcrumb.service";
import { MessagesObservableService } from "../../services/messages-observable.service";
import { SecondaryNavService } from "../../services/right-sidebar.service";
import { CompanySettingsService } from "../../services/settings.service";
import { RolesSidePanelWrapper } from "./roles/roles-sidepanel-wrapper";
import { RoleList } from "./roles/role-list";

@Component({
    selector: "admin-roles",
	templateUrl: "./roles.html",
	standalone: true,
	imports: [
		RoleList,
		RolesSidePanelWrapper
	]
})
export class Roles extends AdminBaseComponent implements OnInit, OnDestroy {
    selected = ReadRole;

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
		this.breadcrumbsService.showBreadcrumb(new Breadcrumb($localize`Roles`));

		this.setBrowserTitle(this.breadcrumbsService.getTitleService(), $localize`Security`);

		this.secondaryNavService.setCurrentArea("Security", "fa-lock", null);

		this.secondaryNavService.clearItems();
		this.secondaryNavService.showItem(new SecondaryNavItem(`Roles`, 'Roles', null, `/admin/security/roles`, null, 1));
		this.secondaryNavService.showItem(new SecondaryNavItem(`Policies`, 'SecurityPolicies', null, `/admin/security/policies`, null, 2));
		this.secondaryNavService.showHeader(true);
    }

	selectedItemChange(event) {
		this.selected = event;
	}

	ngOnDestroy() {
		this.clearSidebar();
	}

	get sidePanelStorageKey() {
		return 'configuration_admin_security_roles';
	}
}