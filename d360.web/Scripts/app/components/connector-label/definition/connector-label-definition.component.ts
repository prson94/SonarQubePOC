import { Component, Input, ViewEncapsulation, EventEmitter, Output } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { Router } from "@angular/router";
import { ConnectorLabel } from "../../../models/connectorLabel.model";
import { ConnectorLabelService } from "../../../services/connectorLabel.service";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { CompanySettingsService } from "../../../services/settings.service";
import { AdminBaseComponent } from "../../admin/admin-base.component";


@Component({
	selector: 'd3s-connector-label-definition',
	templateUrl: './connector-label-definition.component.html',
	styleUrls: ['./connector-label-definition.component.less'],
	encapsulation: ViewEncapsulation.None,
	providers: [ConnectorLabelService]
})

export class ConnectorLabelDefinitionComponent extends AdminBaseComponent {
	@Input() label: ConnectorLabel;
	@Input() isSidePanel: boolean = true;
	@Output() onLinkClicked = new EventEmitter();
	@Output() onEdit = new EventEmitter();

	constructor(private router: Router,
		headerBreadcrumbService: HeaderBreadcrumbService,
		titleService: Title,
		secondaryNavService: SecondaryNavService,
		protected settingsService: CompanySettingsService
	) {
		super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
	}

	open(newTab: boolean = false) {
		const url = `connectorLabel/${this.label.uid}`;
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
}
