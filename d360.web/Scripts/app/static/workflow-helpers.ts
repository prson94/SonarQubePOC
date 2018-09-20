import { StepType, WorkflowActivityType, WorkflowChangeType, EmailTaskRecipientType, FormResponseType } from "../models/workflow.model";

export class WorkflowHelpers {
    static activityTypeName(workflowActivityType: WorkflowActivityType): string {
        switch (workflowActivityType) {
            case WorkflowActivityType.EmailNotification:
                return 'Email Notification';
            case WorkflowActivityType.FieldChange:
                return 'Field Change';
            case WorkflowActivityType.RelationshipUpdate:
                return 'Relationship Update';
            case WorkflowActivityType.StateChange:
                return 'State Change';
            case WorkflowActivityType.StatusChange:
                return 'Status Change';
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

    static formResponseTypeName(responseType: string): string {
        switch (responseType) {
            case 'FirstResponse':
                return 'First Response';
            default:
                return responseType;
        }
    }
}