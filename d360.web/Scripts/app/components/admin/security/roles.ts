import { Component, Input, OnDestroy } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { ReadRole } from "../../../models/security.model";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { CompanySettingsService } from "../../../services/settings.service";
import { AdminBaseComponent } from "../admin-base.component";

@Component({
    selector: "admin-roles",
	templateUrl: "./roles.html",
})
export class Roles extends AdminBaseComponent implements OnDestroy {
	@Input() isStandalonePage: boolean = true;

    selected = ReadRole;

    constructor(
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
    }

	//selectedItemChange(event) {
	//	this.selected = event;

	//	if (this.isStandalonePage) {
	//		this.buildSecondaryNavigation({
	//			forceRefresh: true,
	//			excludeTabs: false
	//		});
	//	}
	//}

	ngOnDestroy() {
		this.clearSidebar();
	}

	//get sidePanelStorageKey() {
	//	return 'configuration_admin_relationship';
	//}
}