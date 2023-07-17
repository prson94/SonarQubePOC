import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import {
	AssignmentVersionItem,
	NodeModel,
	WorkflowDiagramModel,
	WorkflowDiagramNode,
	WorkflowEventRegistration,
	WorkflowTypeNew
} from '../../../models/workflow.model';
import { SidePanelButton } from '../../../models/side-panel.model';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { AssetDetailClickEvent, LinkClickInterceptor } from '../../../services/href-click-service';
import { Subscription } from 'rxjs';

/*global $localize*/

@Component({
	selector: 'd3s-by-workflow-version',
	templateUrl: './by-workflow-version-list.component.html',
	styleUrls: ['./by-workflow-version-list.component.less']
})
export class ByWorkflowVersionListComponent extends BaseComponent implements OnInit, OnDestroy {
	sidePanelOpen: boolean = true;
	sidePanelStorageKey: string = 'WorkflowVersionList_' + this.companySettingsService.CurrentResourceID;
	showSidePanel: boolean = true;
	secondarySidePanelOpen: boolean = false;
	secondarySidePanelTab: string = 'pendingAssignments';
	selectedAssignmentVersionItems: AssignmentVersionItem[];
	versionStepId: number;
	selectedNode: NodeModel;
	workflowEvent: WorkflowEventRegistration;
	nodeList: WorkflowDiagramNode[];
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
	workflowTypeNew: WorkflowTypeNew;
	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	private linkInterceptorSubscription: Subscription;

	constructor(
		public sidePanelService: SidePanelService,
		private companySettingsService: CompanySettingsService,
		private titleService: Title,
		private linkClickInterceptor: LinkClickInterceptor,
		secondaryNavService: SecondaryNavService,
		headerBreadcrumbService: HeaderBreadcrumbService
	) {
		super(companySettingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerBreadcrumbService;
	}

	ngOnInit(): void {
		this.linkInterceptorSubscription = this.linkClickInterceptor.getEvents().subscribe((event: AssetDetailClickEvent) => {
			this.linkClickInterceptor.handleEvent(this.sidePanelSwitcherComponent, event);
			this.secondarySidePanelTab = '';
			this.secondarySidePanelOpen = true;
		});
	}

	ngOnDestroy(): void {
		this.linkInterceptorSubscription?.unsubscribe();
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

	nodeSelection(event: { NodeModel: NodeModel, WorkflowDiagramModel: WorkflowDiagramModel }): void {
		this.versionStepId = parseInt(event?.NodeModel?.key) ?? null;
		this.selectedNode = event.NodeModel;
		this.nodeList = event.WorkflowDiagramModel.Nodes;
		this.workflowEvent = event.WorkflowDiagramModel.Event;
		this.workflowTypeNew = event.WorkflowDiagramModel.Type;
		this.secondarySidePanelTab = 'pendingAssignments';
		this.secondarySidePanelOpen = true;
	}

	closeSecondarySidePanel() {
		if(this.sidePanelSwitcherComponent?.isInitialized) {
			this.secondarySidePanelOpen = false;
			this.sidePanelSwitcherComponent.isInitialized = false;
		}
	}
}
