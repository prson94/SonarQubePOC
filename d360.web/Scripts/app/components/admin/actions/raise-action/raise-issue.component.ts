import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router } from '@angular/router';
import { CompanySettingsService } from '../../../../services/settings.service';
import { SiteUrlHelpers } from '../../../../static/site-url-helpers';
import { BaseComponent } from '../../../shared/base.component';


@Component({
    selector: 'd3s-raise-issue',
    template: `           
        <button type="button" igButton class="ig-button-accent" (click)="raiseIssue()" i18n>Take Action</button>
        `,
    styles: [`
        :host{
            float:right;
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class RaiseIssueComponent extends BaseComponent {

    constructor(
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
    }

	load() {
		if (typeof this.resourceUid === 'undefined') {
			return;
		}
		this.isLoading = true;

		const params = { _assetUid: "", _assetTypeUid: "", _resourceUid: "", _limitToActiveWorkflows: "true" };
		if (this.assetUid) {
			params._assetUid = this.assetUid;
			params._resourceUid = this.resourceUid;

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

				})
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