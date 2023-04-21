import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { Subscription } from 'rxjs';
import { Breadcrumb } from '../../../../models/breadcrumb.model';
import { ActionEditorModel, WorkflowIssueType } from '../../../../models/workflow.model';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { WorkflowService } from '../../../../services/workflow.service';
import { BaseComponent } from '../../../shared/base.component';


@Component({
	selector: 'd3s-raise-issue',
	templateUrl: 'raise-issue.component.html',
	styleUrls: ['raise-issue.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class RaiseIssueComponent extends BaseComponent implements OnChanges {
	@Input() assetUid: string;
	@Input() assetTypeUid: string;
	resourceUid: string;
	isModalVisible: boolean = false;
	selected: WorkflowIssueType;
	issueTypes: WorkflowIssueType[] = [];
	popupMenu = [];
	subBreadcrumb: Subscription;
	breadCrumbs: Breadcrumb[] = [];
	hasSidePanel: boolean = false;

	constructor(
		protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private cdRef: ChangeDetectorRef,
		private messagesService: MessagesObservableService,
		private breadcrumbService: HeaderBreadcrumbService) {
		super(settingsService);

		this.settingsService.getUserVariables().subscribe((res) => {
			this.resourceUid = res.CurrentResourceUid;
			this.load();
		});

		if (this.subBreadcrumb) {
			this.subBreadcrumb.unsubscribe();
		}
		this.breadCrumbs = [];
		this.subBreadcrumb = this.breadcrumbService.breadcrumbs$.subscribe((b) => {
			this.breadCrumbs.push(b);
		});

	}
	ngOnChanges(changes: SimpleChanges): void {
		if (changes) {
			if (changes.assetUid && changes.assetUid.currentValue !== changes.assetUid.previousValue) {
				this.load();
			}
			if (changes.assetTypeUid && changes.assetTypeUid.currentValue !== changes.assetTypeUid.previousValue) {
				this.load();
			}
			this.breadCrumbs = [];
		}
	}

	load() {
		if (typeof this.resourceUid === 'undefined') {
			return;
		}

		this.isLoading = true;
		this.hasSidePanel = false;

		const params = { _assetUid: "", _assetTypeUid: "", _resourceUid: "", _limitToActiveWorkflows: "true" };
		if (this.assetUid) {
			params._assetUid = this.assetUid;
			params._resourceUid = this.resourceUid;
			this.hasSidePanel = true;

		} else if (this.assetTypeUid) {
			params._assetTypeUid = this.assetTypeUid;
			params._resourceUid = this.resourceUid;
		}

		this.workflowService.getWorkflowIssueTypes(null, null, params)
			.subscribe((result) => {
				this.issueTypes = result;
				this.popupMenu = [];
				this.issueTypes.forEach((issue) => {
					this.popupMenu.push({ title: issue.Name, callback: () => { this.openIssueType(issue); } });
				});
				this.isLoading = false;
				this.cdRef.markForCheck();
			});
	}

	openIssueType(issue) {
		this.isModalVisible = true;
		this.selected = issue;
		this.cdRef.markForCheck();
	}

	onSave($event) {
		const action: ActionEditorModel = new ActionEditorModel();
		action.Fields = $event.item;
		delete action.Fields['IssueTypeID'];

		if (this.assetUid) {
			action.AssetUid = this.assetUid;
		} else {
			action.AssetTypeUid = this.assetTypeUid;
		}

		this.workflowService.raiseIssues($event.actionTypeUid, action)
			.subscribe((res) => {
				this.showMessageForApiResponse(this.messagesService, res);
				this.close();
				this.cdRef.markForCheck();
			});
	}

	close() {
		this.selected = null;
		this.isModalVisible = false;
	}
}