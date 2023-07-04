import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { GroupService } from '../../../../services/group.service';
import { WorkflowHelpers } from '../../../../static/workflow-helpers';
import { EmailRecipients, WorkflowStepDetail } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-step-form-details',
	templateUrl: './assignment-step-form-details.component.html',
	styleUrls: ['./assignment-step-form-details.component.less']
})
export class AssignmentStepFormDetailsComponent extends BaseComponent implements OnInit {

	@Input() step: WorkflowStepDetail;
	isLoading: boolean = false;
	groupName: string;
	showAll: boolean = false;
	helper = WorkflowHelpers;
	recipients: EmailRecipients[] = [];

	constructor(
		private groupService: GroupService,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.sortNames();
		if (this.step?.Settings?.MessageRecipientType === 'Group') {
			this.isLoading = true;
			this.groupService.getGroupByUid(this.step.Settings.MessageToGroup).subscribe((data) => {
				this.groupName = data.items[0]?.Name ?? '';
				this.isLoading = false;
			});
		}
	}

	sortNames(): void {
		if (this.step?.ItemSettings?.emails?.email) {
			const sorted: EmailRecipients[] = this.step.ItemSettings.emails.email.slice();

			sorted.sort((a, b) => {
				if (a['name']?.toLowerCase() < b['name']?.toLowerCase()) {
					return -1;
				}
				if (a['name']?.toLowerCase() > b['name']?.toLowerCase()) {
					return 1;
				}
				return 0;
			});

			this.recipients = sorted;
		}
	}

	toggleShowAll(): void {
		this.showAll = !this.showAll;
	}
}
