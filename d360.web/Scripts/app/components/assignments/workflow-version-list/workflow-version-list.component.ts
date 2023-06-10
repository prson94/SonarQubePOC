import { Component, OnInit } from '@angular/core';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { CompanySettingsService } from '../../../services/settings.service';
import { SecondaryNavItem } from '../../../models/secondaryNav.model';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { WorkflowByType } from '../../../models/workflow.model';
import { SidePanelButton } from '../../../models/side-panel.model';

@Component({
	selector: 'd3s-workflow-version-list',
	templateUrl: './workflow-version-list.component.html',
	styleUrls: ['./workflow-version-list.component.less']
})
export class WorkflowVersionListComponent extends BaseComponent implements OnInit {
	sidePanelOpen: boolean = true;
	sidePanelStorageKey: string = 'WorkflowVersionList_' + this.companySettingsService.CurrentResourceID;
	showSidePanel: boolean = true;
	selectedWorkflowByType: WorkflowByType[];
	sidePanelButtons: SidePanelButton[] = [new SidePanelButton({
		label: $localize`Assignments on Workflow Version`,
		tooltip: $localize`Assignments on Workflow Version`,
		disabledTooltip: null,
		nothingSelectedMessage: $localize`Select a Workflow from the list to display its information`,
		notApplicableMessage: $localize`Information data is not available for the selected Workflow`,
		multipleSelectedMessage: $localize`Select a single Workflow to display it’s information`,
		key: 'information',
		icon: 'fa-info-circle',
		disabled: false,
		visible: true,
		needsSelection: true
	})];

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

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
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
