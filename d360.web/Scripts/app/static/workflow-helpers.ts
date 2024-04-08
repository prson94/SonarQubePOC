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

	static workflowStateDetail(state: string): {title: string, body: string} {
		switch (state) {
			case 'Error':
				return {
					title: $localize`Failed: unidentified code error`,
					body: $localize`<p>Assignment failed and cannot be completed due to an unknown reason.</p><p><b>Contact support to troubleshoot the issue.</b> After the issue is fixed, a new instance will need to be initiated.</p>`
				};
			case 'Failed':
				return {
					title: $localize`Failed: assignment failed for an unknown reason`,
					body: $localize`<p>Assignment failed and cannot be completed due to an unknown reason.</p><p><b>Contact support to troubleshoot the issue.</b> After the issue is fixed, a new instance will need to be initiated.</p>`
				};			
			case 'HTTPRequestError':
				return {
					title: $localize`Failed: HTTP Request failed`,
					body: $localize`<p>Assignment Failed and cannot be completed because the last HTTP Request was unsuccessful.</p><p><b>Configuration of the HTTP Request activity needs to be reviewed.</b> After the issues are fixed, a new instance will need to be initiated.</p>`
				};
			case 'NoValidTransitions':
				return {
					title: $localize`Failed: no valid transitions found`,
					body: $localize`<p>Assignment Failed and cannot be completed because no valid transitions were found after the last completed step</p><p><b>Workflow configuration needs to be reviewed.</b> After transition issues are fixed, a new instance will need to be initiated.</p>`
				};
			case 'InvalidInitiator':
				return {
					title: $localize`Failed: Invalid Initiator`,
					body: $localize`<p>Assignment Failed and cannot be completed because the initiator cannot be identified.</p><p><b>Confirm the status of the initiating user.</b> After the issue is fixed, a new instance will need to be initiated.</p>`
				};
			case 'NoValidAssignee':
				return {
					title: $localize`Failed: No valid form recipient found`,
					body: $localize`<p>Assignment Failed and cannot be completed because no valid recipients were found for the last form activity.</p><p><b>Configuration of the Form activity needs to be reviewed.</b> After the issues are fixed, a new instance will need to be initiated.</p>`
				};
			case 'InvalidRecipient':
				return {
					title: $localize`Failed: no valid recipient found`,
					body: $localize`<p>Assignment Failed and cannot be completed because no valid recipient was found after the last completed step</p><p><b>Workflow configuration needs to be reviewed.</b> After the issues are fixed, a new instance will need to be initiated.</p>`
				};
			default: 
				return {
					title: $localize`Failed: failed for an unknown reason`,
					body: $localize`<p>Assignment failed and cannot be completed due to an unknown reason.</p><p><b>Contact support to troubleshoot the issue.</b> After the issue is fixed, a new instance will need to be initiated.</p>`
				};
		}
	}
}
