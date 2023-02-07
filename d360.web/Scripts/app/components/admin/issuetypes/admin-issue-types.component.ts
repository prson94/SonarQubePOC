import { Component, ViewEncapsulation } from '@angular/core';
import { WorkflowIssueType } from '../../../models/workflow.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { WorkflowService } from '../../../services/workflow.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';
import { Router } from '@angular/router';
import { SidePanelService } from '../../../services/side-panel.service';

/*global $localize*/
// eslint-disable-next-line no-var
declare var CurrentResourceID

@Component({
	selector: 'd3s-admin-issue-types',
	templateUrl: 'admin-issue-types.component.html',
	styleUrls: ['admin-issue-types.component.less'],
	encapsulation: ViewEncapsulation.None,
	providers: [WorkflowService],
})

export class AdminIssueTypesComponent extends AdminBaseComponent {
	issueTypes: WorkflowIssueType[] = [];
	selected: WorkflowIssueType;
	showEditor: boolean = false;
	showDelete: boolean = false;
	theDeleteCallback: Function;

	editorTitle = $localize`Action Type`;

	get deleteModalTitle(): string {
		return $localize`Are you sure you want to delete the action type [${this.selected?.Name}]?`;
	}

	get sidePanelStorageKey() {
		return 'configuration_workflow_actions_' + CurrentResourceID;
	}

	constructor(
		headerBreadcrumbService: HeaderBreadcrumbService,
		protected messagesService: MessagesObservableService,
		secondaryNavService: SecondaryNavService,
		protected settingsService: CompanySettingsService,
		private sidePanelService: SidePanelService,
		private router: Router,
		titleService: Title,
		private workflowService: WorkflowService) {
		super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
		this.areaName = StringConstants.Section_Actions;
		this.adminHeading = StringConstants.Section_Actions;
		this.tabTitle = $localize`Action Types`;
		this.setCommonItems();
		this.theDeleteCallback = this.deleteIssueType.bind(this);

		this.sidePanelService.editClickSource$.subscribe((res) => {
			this.selected = res as WorkflowIssueType;
			this.OnEdit();
		});
	}

	ngOnInit() {
		this.load();
	}

	ngOnDestroy() {
		this.clearSidebar();
	}

	private deleteIssueType(uid: string) {
		this.isLoading = true;
		this.workflowService.deleteWorkflowIssueType(uid)
			.subscribe((result) => {
				if (result) {
					this.showMessageForApiResponse(this.messagesService, result);
					if (result.Success) {
						this.issueTypes = this.issueTypes.filter((x) => x.Uid !== uid);
					}
					this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null;
				}
				this.isLoading = false;
				this.showDelete = false;
			});
	}

	selectedItemChange(callback: Function = null) {
		if (this.selected) {
			this.baseAssetTypeUid = this.selected.Uid;
			if (!this.selected.ID) {
				this.workflowService.getIssueByUID(this.selected.Uid)
					.subscribe((result) => {
						this.selected.ID = result.ID;
						this.isLoading = false;
						this.buildSecondaryNavigation({ assetTypeUid: this.baseAssetTypeUid, forceRefresh: true });
						if (callback) {
							callback();
						}
					});
			} else {
				this.buildSecondaryNavigation({ assetTypeUid: this.baseAssetTypeUid, forceRefresh: true });
				if (callback) {
					callback();
				}
			}
		}

	}
	private load() {
		this.isLoading = true;
		this.workflowService.getAdminWorkflowIssueTypes()
			.subscribe((result) => {
				this.issueTypes = result.sort((a, b) => a.Name.localeCompare(b.Name));

				this.issueTypes.forEach((type) => {
					if (!type.Description) {
						type.Description = "---";
					}
					if (!type.UpdatedByName) {
						type.UpdatedByName = "---";
					}
					if (!type.UpdatedOn) {
						type.UpdatedOn = "---"
					}
					type.Description = type.Description.replace(/<[^>]*>/g, '');
				});
				this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null;
				this.selectedItemChange();
				this.isLoading = false;
			});
	}

	private showAdd() {
		this.selected = null;
		this.showEditor = true;
		this.showDelete = false;
	}

	private saveIssueType(event) {
		this.isLoading = true;

		this.workflowService.saveIssueType(event.item)
			.subscribe((result) => {
				this.isLoading = false;
				this.showMessageForApiResponse(this.messagesService, result);
				if (result.Success) {
					this.load();
				}
				this.showEditor = false;
			});
	}

	private closeEditor() {
		this.showEditor = false;
		if (!this.selected) { this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null; }
	}

	private OnEdit() {
		this.selectedItemChange(() => {
			this.showEditor = true;
			this.showDelete = false;
		});
	}

	private OnDelete() {
		this.selectedItemChange(() => {
			this.showDelete = true;
			this.showEditor = false;
		});
	}

	open($event: PointerEvent, uid: string) {
		$event.preventDefault();
		const url = `/admin/configuration/WorkflowActions/${uid}/fields`;
		this.router.navigateByUrl(url);
	}
}
