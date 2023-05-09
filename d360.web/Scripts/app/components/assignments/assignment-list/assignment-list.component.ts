import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core'
import { Title } from '@angular/platform-browser'
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service'
import { CompanySettingsService } from '../../../services/settings.service'
import { ActivatedRoute, Router } from '@angular/router'
import { SecondaryNavService } from '../../../services/right-sidebar.service'
import { Breadcrumb } from '../../../models/breadcrumb.model'
import { SiteUrlHelpers } from '../../../static/site-url-helpers'
import { Observable } from 'rxjs'
import {
	AdvancedFilterFieldType
} from '../../assets-grid/advanced-filtering/advanced-filtering.models'
import { SidePanelService } from '../../../services/side-panel.service'
import { WebAnalyticsService } from '../../../services/web-analytics.service'
import { DataProfileService } from '../../../services/dataprofile.service'
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly'
import { FeatureFlags } from '../../../services/feature-flags.enum'
import { StringConstants } from '../../../static/string-constants'
import { IOutputData } from 'angular-split'
import { BaseComponent } from '../../shared/base.component'
import { WorkflowMonitorItem } from '../../../models/workflowmonitor.model'

@Component({
	selector: 'd3s-assignment-list',
	templateUrl: './assignment-list.component.html',
	styleUrls: ['./assignment-list.component.less'],
	providers: [DataProfileService]
})
export class AssignmentListComponent extends BaseComponent implements OnInit, OnDestroy {
	@Output() selectedTypeChanged = new EventEmitter()
	sub: any


	selectedType: any = null
	simpleFilter: string = ''
	advancedFilter: string = ''
	rowsPerPage: number = 25
	currentPageNumber: number = 1
	showSidePanel: boolean = true
	sidePanelOpen: boolean = false
	sidePanelTab: string = 'detail'
	sidePanelStorageKey: string
	sortField: string
	sortOrder: number
	isExportInProgress: boolean = false
	isContainsSearchDefault: boolean = false

	filterFields$: Observable<AdvancedFilterFieldType[]>

	readonly menuKey = '~menu'

	secondarySidePanel: string
	resourceUid: any
	secondarySidePanelOpen: boolean
	showDelete: boolean = false
	showEditor: boolean = false
	showAddButton: boolean = false
	selectedWorkflowId: number = 876
	selectedWorkflowItems: WorkflowMonitorItem[]

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

	ngOnInit() {

		this.sidePanelStorageKey = 'SemanticTypes_' + this.settingsService.CurrentResourceID
		this.displayBreadCrumbs()
	}

	selectRow(row: any) {
		this.secondarySidePanelOpen = false
		this.selectedType = row
		if (this.selectedType) {
			this.baseSemanticTypeUid = this.selectedType.uid
			this.buildSecondaryNavigation({
				assetUid: this.selectedType.uid,
				objectId: 0,
				objectType: 'SemanticType',
				buildBreadcrumbOverride: this.displayBreadCrumbs.bind(this)
			})
		}
		this.selectedTypeChanged.emit(row)
	}

	displayBreadCrumbs() {
		this.sub = this.route.params.subscribe((params) => {
			this.breadcrumbsService.getFolderTitle('#SemanticTypes').then((res) => {
				this.setBrowserTitle(this.titleService, res)
				this.breadcrumbsService.clearBreadcrumbs()
				this.breadcrumbsService.showBreadcrumb(new Breadcrumb(res, SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT))

				this.breadcrumbsService.getFolderIcon(res).subscribe((icon) => {
					this.secondaryNavService.setCurrentArea(res, icon, StringConstants.Section_SemanticTypes)
				})
			})

		})
	}

	ngOnDestroy() {
		if (this.sub) {
			this.sub.unsubscribe()
		}
	}

	handleSecondarySidePanelLinkClicked(event: any) {
		this.secondarySidePanelOpen = true
		if (event && event.resourceUid) {
			this.secondarySidePanel = 'user'
			this.resourceUid = event.resourceUid
		} else {
			this.secondarySidePanel = 'status'
		}
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

	workflowSelectionChanged(workflowMonitorItems: WorkflowMonitorItem[]) {
		this.selectedWorkflowItems = workflowMonitorItems
	}

	protected readonly console = console
}
