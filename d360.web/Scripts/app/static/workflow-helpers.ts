import { StepType, WorkflowActivityType, WorkflowChangeType } from '../models/workflow.model';

/*global $localize*/

export class WorkflowHelpers {
	static activityTypeName(workflowActivityType: WorkflowActivityType): string {
		switch (workflowActivityType) {
			case WorkflowActivityType.EmailNotification:
				return $localize`Email Notification`;
			case WorkflowActivityType.FieldChange:
				return $localize`Field Change`;
			case WorkflowActivityType.RelationshipChange:
				return $localize`Relationship Update`;
			case WorkflowActivityType.StateChange:
				return $localize`State Change`;
			case WorkflowActivityType.StatusChange:
				return $localize`Status Change`;
			case WorkflowActivityType.HTTPRequest:
				return $localize`HTTP Request`;
			case WorkflowActivityType.HTTPResponse:
				return $localize`HTTP Response`;
			default:
				return WorkflowActivityType[workflowActivityType];

		}
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

	static stepActivityTypeIcon: Record<WorkflowActivityType, string> = {
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
		const icon: string = this.stepActivityTypeIcon[workflowActivityType];
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
				return 'First Response';
			default:
				return responseType;
		}
	}
}
