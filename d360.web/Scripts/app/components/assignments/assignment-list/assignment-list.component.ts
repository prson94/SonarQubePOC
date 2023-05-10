import { Component, OnDestroy, OnInit } from '@angular/core'
import { Title } from '@angular/platform-browser'
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service'
import { CompanySettingsService } from '../../../services/settings.service'
import { ActivatedRoute, Router } from '@angular/router'
import { SecondaryNavService } from '../../../services/right-sidebar.service'
import { Observable } from 'rxjs'
import { AdvancedFilterFieldType } from '../../assets-grid/advanced-filtering/advanced-filtering.models'
import { SidePanelService } from '../../../services/side-panel.service'
import { WebAnalyticsService } from '../../../services/web-analytics.service'
import { DataProfileService } from '../../../services/dataprofile.service'
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly'
import { FeatureFlags } from '../../../services/feature-flags.enum'
import { IOutputData } from 'angular-split'
import { BaseComponent } from '../../shared/base.component'
import { WorkflowMonitorItem } from '../../../models/workflowmonitor.model'
import { SidePanelButton } from '../../../models/side-panel.model'
import { SecondaryNavItem } from '../../../models/secondaryNav.model'

@Component({
	selector: 'd3s-assignment-list',
	templateUrl: './assignment-list.component.html',
	styleUrls: ['./assignment-list.component.less'],
	providers: [DataProfileService]
})
export class AssignmentListComponent extends BaseComponent implements OnInit, OnDestroy {
	selectedType: any = null
	simpleFilter: string = ''
	advancedFilter: string = ''
	rowsPerPage: number = 25
	currentPageNumber: number = 1
	showSidePanel: boolean = true
	sidePanelOpen: boolean = false
	sidePanelTab: string = 'information'
	sidePanelStorageKey: string = 'AssignmentList_' + this.settingsService.CurrentResourceID
	sortField: string
	sortOrder: number
	isExportInProgress: boolean = false
	isContainsSearchDefault: boolean = false

	filterFields$: Observable<AdvancedFilterFieldType[]>

	secondarySidePanel: string
	resourceUid: any
	secondarySidePanelOpen: boolean
	showDelete: boolean = false
	showEditor: boolean = false
	showAddButton: boolean = false
	selectedWorkflowId: number = 876
	selectedWorkflowItems: WorkflowMonitorItem[]
	sidePanelButtons: SidePanelButton[] = [
		new SidePanelButton({
			label: $localize`Assignment Progress`,
			tooltip: $localize`Assignment Progress`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select an Assignment from the list to display its progress`,
			notApplicableMessage: $localize`Progress data is not available for the selected Assignment`,
			multipleSelectedMessage: $localize`Select a single Assignment to display it’s progress`,
			key: 'progress',
			icon: 'fa-step-forward',
			disabled: false,
			visible: true,
			needsSelection: true
		}), new SidePanelButton({
			label: $localize`Assignment Information`,
			tooltip: $localize`Assignment Information`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select an Assignment from the list to display its information`,
			notApplicableMessage: $localize`Information data is not available for the selected Assignment`,
			multipleSelectedMessage: $localize`Select a single Assignment to display it’s information`,
			key: 'information',
			icon: 'fa-info-circle',
			disabled: false,
			visible: true,
			needsSelection: true
		})
	]


	constructor(private route: ActivatedRoute,
				protected router: Router,
				headerBreadcrumbService: HeaderBreadcrumbService,
				private titleService: Title,
				public sidePanelService: SidePanelService,
				webAnalyticsService: WebAnalyticsService,
				private dataProfileService: DataProfileService,
				secondaryNavService: SecondaryNavService,
				protected settingsService: CompanySettingsService,
				private featureFlagService: LaunchDarklyService) {
		super(settingsService)
		this.secondaryNavService = secondaryNavService
		this.breadcrumbsService = headerBreadcrumbService
		this.isContainsSearchDefault = this.featureFlagService.variation<boolean>(FeatureFlags.ContainsSearchDefaultUiFlag)
	}

	ngOnInit(): void {
		this.clearSidebar()
		this.displayBreadCrumbs()
	}

	selectRow(row: any): void {
		this.secondarySidePanelOpen = false
		this.selectedType = row
	}

	displayBreadCrumbs(): void {
		this.setBrowserTitle(this.titleService, 'Assignments')
		this.breadcrumbsService.clearBreadcrumbs()
		this.breadcrumbsService.clearCurrentObjectInfo()
		this.secondaryNavService.clearItems()
		this.secondaryNavService.clearCurrentObject()
		this.secondaryNavService.setCurrentArea('Assignments', 'fa-list-ul', $localize`Assignments`)
		this.secondaryNavService.showHeader(true)
		this.fieldNav = new SecondaryNavItem(
			$localize`By Workflow Version`,
			'byWorkflowVersion',
			null,
			'/assignments/by-workflow-version', null, 1)
		this.secondaryNavService.showItem(this.fieldNav)
	}

	ngOnDestroy(): void {

	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey)
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen)
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen)
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event)
	}

	workflowSelectionChanged(workflowMonitorItems: WorkflowMonitorItem[]): void {
		this.selectedWorkflowItems = workflowMonitorItems
		this.selectRow(workflowMonitorItems)
	}

	sidePanelLinkClicked(link: any) {
		this.secondarySidePanelOpen = true
		this.secondarySidePanel = 'user'
		this.resourceUid = link.resourceUid;
	}
}
