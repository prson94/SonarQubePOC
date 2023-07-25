import { Component, Input, OnInit } from '@angular/core';
import { NodeModel, WorkflowActivityType } from '../../../../../models/workflow.model';
import { ResponsibilityType } from '../../../../../models/responsibility-type.model';
import { ResponsibilityTypeService } from '../../../../../services/responsibility-type.service';
import { GroupService } from '../../../../../services/group.service';
import { isArray } from 'lodash-es';

/*global $localize*/

@Component({
	selector: 'd3s-step-information-form',
	templateUrl: './step-information-form.component.html'
})
export class StepInformationFormComponent implements OnInit {
	@Input() selectedNode: NodeModel;
	@Input() showFormFields: boolean = false;
	private responsibilities: ResponsibilityType[] = [];
	private groups: { label: string, value: string }[] = [];
	public isLoading: boolean = false;

	protected readonly WorkflowActivityType = WorkflowActivityType;

	constructor(private responsibilityService: ResponsibilityTypeService, private groupService: GroupService) {
	}

	ngOnInit(): void {
		this.load();
	}

	load(): void {
		this.isLoading = true;
		if (this.selectedNode.settings['MessageRecipientType'] === 'Responsibility') {
			this.responsibilityService.getResponsibilityTypes()
				.subscribe((r: ResponsibilityType[]) => {
					this.responsibilities = r;
					this.isLoading = false;
				});
			if (this.selectedNode.settings.ResponsibilityTypeID != null) {
				if (!isArray(this.selectedNode.settings.ResponsibilityTypeID)) {
					const id = this.selectedNode.settings.ResponsibilityTypeID;
					delete this.selectedNode.settings.ResponsibilityTypeID;
					this.selectedNode.settings.ResponsibilityTypeID = [];
					this.selectedNode.settings.ResponsibilityTypeID.push(id);
				}
			}
		} else if (this.selectedNode.settings['MessageRecipientType'] === 'Group') {
			this.groupService.getGroups().subscribe((GroupList) => {
				this.isLoading = false;
				this.groups = GroupList.items.map((g) => {
					return { value: g.Uid, label: g.Name };
				});
				if (this.selectedNode.settings.MessageToGroup != null) {
					if (!this.groups.find((g) => g.value === this.selectedNode.settings.MessageToGroup)) {
						this.groups.push({
							value: this.selectedNode.settings.MessageToGroup,
							label: '<invalid group>'
						});
					}
				}
			});
		} else {
			this.isLoading = false;
		}
	}

	getResponsibilityName(i: number): string {
		const id = this.selectedNode.settings.ResponsibilityTypeID[+i];
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
		return (this.selectedNode.settings.MessageToGroup == null) ? '<none>' : this.groups.find((g): boolean => g.value === this.selectedNode.settings.MessageToGroup).label;
	}

	get responsibilityLabel(): string {
		if(this.selectedNode.settings.ResponsibilityTypeID == null || this.selectedNode.settings.ResponsibilityTypeID.length < 2) {
			return $localize`Responsibility`
		} else {
			return $localize`Responsibilities`
		}
    }

	get recipientType(): string {
		if(this.selectedNode.settings['MessageRecipientType'] == 'SpecificUser') {
			return $localize`Specific User`
		} else {
			return this.selectedNode.settings['MessageRecipientType']
		}
	}

	get responseType(): string {
		if(this.selectedNode.settings?.FormResponseType === 'FirstResponse') {
			return $localize`First Response`
		} else {
			return this.selectedNode.settings?.FormResponseType
		}
	}
}
