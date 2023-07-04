import { StepType, WorkflowActivityType, WorkflowChangeType } from '../models/workflow.model';

export class WorkflowHelpers {
	static activityTypeName(workflowActivityType: WorkflowActivityType): string {
		switch (workflowActivityType) {
			case WorkflowActivityType.EmailNotification:
				return $localize`Email Notification`;
			case WorkflowActivityType.FieldChange:
				return $localize`Field Change`;
			case WorkflowActivityType.RelationshipUpdate:
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

	static getActivityTypeIcon(workflowActivityType: WorkflowActivityType, stepType?: StepType): string {
		switch (workflowActivityType) {
			case WorkflowActivityType.EmailNotification:
				return 'fa-envelope';
			case WorkflowActivityType.FieldChange:
				return 'fa-id-card';
			case WorkflowActivityType.RelationshipUpdate:
				return 'fa-users';
			case WorkflowActivityType.HTTPRequest:
				return 'fa-globe';
			case WorkflowActivityType.HTTPResponse:
				return 'fa-cogs';
			case WorkflowActivityType.Form:
				return 'fa-sliders';
			case WorkflowActivityType.Delete:
				return 'fa-trash';
			case WorkflowActivityType.None:
				if (stepType === StepType.Start) {
					return 'fa-play-circle';
				} else {
					return 'fa-stop-circle';
				}
			default:
				return '';

		}
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