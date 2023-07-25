import { StepType, WorkflowActivityType, WorkflowChangeType } from '../models/workflow.model';

/*global $localize*/

export class WorkflowHelpers {
	private static activityTypeNames: Record<WorkflowActivityType, string> = {
		0: $localize`None`,
		1: $localize`Email Notification`,
		2: $localize`Status Change`,
		3: $localize`Form`,
		4: $localize`Procedure`,
		5: $localize`Field Change`,
		6: $localize`Relationship Update`,
		7: $localize`State Change`,
		8: $localize`Delete`,
		9: $localize`HTTP Request`,
		10: $localize`HTTP Response`
	};

	static activityTypeName(workflowActivityType: WorkflowActivityType): string {
		return this.activityTypeNames[+workflowActivityType];
	}

	static stepTypeName(stepType: StepType): string {
		return StepType[stepType];
	}

	static changeTypeName(changeType: WorkflowChangeType): string {
		return WorkflowChangeType[changeType];
	}

	static recipientTypeName(recipientType: string): string {
		switch (recipientType) {
			case 'SpecificUser':
				return 'Specific User';
			default:
				return recipientType;
		}
	}

	private static stepActivityTypeIcon: Record<WorkflowActivityType, string> = {
		0: '',
		1: 'fa-envelope',
		2: '',
		3: 'fa-sliders',
		4: '',
		5: 'fa-id-card',
		6: 'fa-users',
		7: '',
		8: 'fa-trash',
		9: 'fa-globe',
		10: 'fa-cogs',
	};

	static getActivityTypeIcon(workflowActivityType: WorkflowActivityType, stepType?: StepType): string {
		const icon: string = this.stepActivityTypeIcon[+workflowActivityType];
		if (!icon && stepType) {
			if (stepType === StepType.Start) {
				return 'fa-play-circle';
			} else {
				return 'fa-stop-circle';
			}
		}
		return icon;
	}

	static formResponseTypeName(responseType: string): string {
		switch (responseType) {
			case 'FirstResponse':
				return $localize`First Response`;
			default:
				return responseType;
		}
	}
}
