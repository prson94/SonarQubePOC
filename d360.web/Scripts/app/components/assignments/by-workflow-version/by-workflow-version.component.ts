import { Component } from '@angular/core';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AssignmentByVersion, NodeModel } from '../../../models/workflow.model';
import { SidePanelButton } from '../../../models/side-panel.model';

/*global $localize*/

@Component({
	selector: 'd3s-by-workflow-version',
	templateUrl: './by-workflow-version.component.html',
	styleUrls: ['./by-workflow-version.component.less']
})
export class ByWorkflowVersionComponent extends BaseComponent {
	sidePanelOpen: boolean = true;
	sidePanelStorageKey: string = 'WorkflowVersionList_' + this.companySettingsService.CurrentResourceID;
	showSidePanel: boolean = true;
	secondarySidePanelOpen: boolean = false;
	secondarySidePanelTab: string = 'pendingAssignments';
	selectedAssignmentByVersion: AssignmentByVersion[];
	versionStepId: number;
	selectedNode: NodeModel;
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

	secondarySidePanelButtons: SidePanelButton[] = [new SidePanelButton({
		label: $localize`Pending Assignments`,
		tooltip: $localize`Pending Assignments on Step`,
		disabledTooltip: null,
		nothingSelectedMessage: $localize`Select a Step from the Workflow diagram to display its pending assignments`,
		notApplicableMessage: $localize`Pending Assignments is not available for the selected Step`,
		multipleSelectedMessage: $localize`Select a single Step to display it’s pending assignments`,
		key: 'pendingAssignments',
		icon: 'fa-step-forward',
		disabled: false,
		visible: true,
		needsSelection: true
	}),
		new SidePanelButton({
			label: $localize`Activity Information`,
			tooltip: $localize`Activity Information`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select a Step from the Workflow diagram to display its information`,
			notApplicableMessage: $localize`Information data is not available for the selected Step`,
			multipleSelectedMessage: $localize`Select a single Step to display it’s information`,
			key: 'information',
			icon: 'fa-info-circle',
			disabled: false,
			visible: true,
			needsSelection: true
		})
	];

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

	nodeSelection(event: NodeModel): void {
		this.versionStepId = parseInt(event?.key) ?? null;
		this.selectedNode = event;
		this.secondarySidePanelOpen = true;
	}
}
