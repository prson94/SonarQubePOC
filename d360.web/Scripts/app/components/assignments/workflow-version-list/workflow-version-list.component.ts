import { Component, OnInit } from '@angular/core';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { CompanySettingsService } from '../../../services/settings.service';
import { SecondaryNavItem } from '../../../models/secondaryNav.model';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';

@Component({
	selector: 'd3s-workflow-version-list',
	templateUrl: './workflow-version-list.component.html',
	styleUrls: ['./workflow-version-list.component.less']
})
export class WorkflowVersionListComponent extends BaseComponent implements OnInit {
	sidePanelOpen: boolean = false;
	sidePanelStorageKey: string = 'WorkflowVersionList_' + this.companySettingsService.CurrentResourceID;

	constructor(
		public sidePanelService: SidePanelService,
		private companySettingsService: CompanySettingsService,
		private titleService: Title,
		secondaryNavService: SecondaryNavService,
		headerBreadcrumbService: HeaderBreadcrumbService
	) {
		super(companySettingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerBreadcrumbService;
	}

	ngOnInit(): void {
		this.displayBreadCrumbs();
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}

	private displayBreadCrumbs(): void {
		this.setBrowserTitle(this.titleService, 'Assignments');
		this.breadcrumbsService.clearBreadcrumbs();
		this.breadcrumbsService.clearCurrentObjectInfo();
		this.secondaryNavService.clearItems();
		this.secondaryNavService.clearCurrentObject();
		this.secondaryNavService.setCurrentArea('Assignments', 'fa-list-ul', $localize`By Workflow Version`);
		this.secondaryNavService.showHeader(true);
		this.fieldNav = new SecondaryNavItem(
			$localize`Assignments`,
			'assignments',
			null,
			'/assignments', null, 1);
		this.secondaryNavService.showItem(this.fieldNav);
	}
}
