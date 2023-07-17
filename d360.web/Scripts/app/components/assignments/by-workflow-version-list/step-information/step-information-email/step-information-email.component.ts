import { Component, Input, OnInit } from '@angular/core';
import { NodeSettings, WorkflowActivityType } from '../../../../../models/workflow.model';
import { ResponsibilityTypeService } from '../../../../../services/responsibility-type.service';
import { GroupService } from '../../../../../services/group.service';
import { isArray } from 'lodash-es';
import { ResponsibilityType } from '../../../../../models/responsibility-type.model';

@Component({
	selector: 'd3s-step-information-email',
	templateUrl: './step-information-email.component.html',
	styleUrls: ['./step-information-email.component.less']
})
export class StepInformationEmailComponent implements OnInit {
	@Input() settings: NodeSettings;
	@Input() sendFormEmail: boolean = false;
	private responsibilities: ResponsibilityType[] = [];
	private groups: { label: string, value: string }[] = [];
	public isLoading: boolean = false;

	protected readonly WorkflowActivityType = WorkflowActivityType;


	constructor(private responsibilityService: ResponsibilityTypeService, private groupService: GroupService) {
	}


	ngOnInit(): void {
		if (!this.sendFormEmail) {
			this.load();
		}
	}

	load(): void {
		this.isLoading = true;
		if (this.settings['MessageRecipientType'] === 'Responsibility') {
			this.responsibilityService.getResponsibilityTypes()
				.subscribe((r: ResponsibilityType[]) => {
					this.responsibilities = r;
					this.isLoading = false;
				});
			if (this.settings.ResponsibilityTypeID != null) {
				if (!isArray(this.settings.ResponsibilityTypeID)) {
					const id = this.settings.ResponsibilityTypeID;
					delete this.settings.ResponsibilityTypeID;
					this.settings.ResponsibilityTypeID = [];
					this.settings.ResponsibilityTypeID.push(id);
				}
			}
		} else if (this.settings['MessageRecipientType'] === 'Group') {
			this.groupService.getGroups().subscribe((GroupList) => {
				this.isLoading = false;
				this.groups = GroupList.items.map((g) => {
					return { value: g.Uid, label: g.Name };
				});
				if (this.settings.MessageToGroup != null) {
					if (!this.groups.find((g) => g.value === this.settings.MessageToGroup)) {
						this.groups.push({
							value: this.settings.MessageToGroup,
							label: '<invalid group>'
						});
					}
				}
			});
		}
	}

	getResponsibilityName(i: number): string {
		const id = this.settings.ResponsibilityTypeID[+i];
		if (id == null || +id < 0) {
			return '';
		}

		const r: ResponsibilityType = this.responsibilities.find((r: ResponsibilityType): boolean => r.ID === +id);

		if (r != null) {
			return r.Name;
		}
		return '';
	}

	getGroupName(): string {
		return (this.settings.MessageToGroup == null) ? '<none>' : this.groups.find((g): boolean => g.value === this.settings.MessageToGroup).label;
	}
}
