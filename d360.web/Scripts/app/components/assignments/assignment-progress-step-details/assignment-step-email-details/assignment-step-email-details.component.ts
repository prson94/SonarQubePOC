import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { EmailRecipients, EmailSettings, WorkflowStepDetail } from '../../../../models/workflow.model';
import { WorkflowHelpers } from '../../../../static/workflow-helpers';
import { CompanySettingsService } from '../../../../services/settings.service';
import { GroupService } from '../../../../services/group.service';

@Component({
	selector: 'd3s-assignment-step-email-details',
	templateUrl: './assignment-step-email-details.component.html',
	styleUrls: ['./assignment-step-email-details.component.less']
})
export class AssignmentStepEmailDetailsComponent extends BaseComponent implements OnInit {
	@Input() step: WorkflowStepDetail = null;
	@Input() isAggregate: boolean = false;
	@Input() formEmailDetails: boolean = false;
	recipients: EmailRecipients[] = [];
	showAll: boolean = false;
	emailSettings: EmailSettings;
	groupName: string;
	isLoading: boolean = false;
	helper = WorkflowHelpers;


	constructor(
		private groupService: GroupService,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit() {
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
		if (this.step) {
			if (!this.isAggregate && this.step.ItemSettings.emails.email != null) {
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

			if (this.isAggregate) {
				this.emailSettings = this.step.EventSettings;
			} else {
				this.emailSettings = this.step.Settings;
			}

		}
	}

	toggleShowAll(): void {
		this.showAll = !this.showAll;
	}
}
