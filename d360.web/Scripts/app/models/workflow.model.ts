import { State } from './asset.model';
import { UID } from './shared.model';


export class Issue {
	Issue: string;
	IssueType: IssueType;
	IssueTypeName: string;
	ResourceName: string;
	ResourceID: number;
	ActivityName: string;
	DateStarted: string;
	WorkflowID: string;
	IssueID: number;
}

export class IssueDetail {
	ActivityName: string;
	AllowAction: boolean;
	DateCompleted: Date;
	DateStarted: Date;
	IsCompleted: boolean;
	Issue: string;
	Name: string;
	Notes: string;
	Object: string;
	ObjectID: number;
	RaisedBy: string;
	RaisedByResourceID: number;
	Url: string;
	WorkflowID: string;
	EllapsedDays: number;
	WorkflowItemID: number;
}

export class WorkflowIssueType {
	Uid: string;
	ID: number;
	Name: string;
	Description: string;
	IsSystem: boolean;
	UpdatedOn: string;
	UpdatedByUid: string;
	UpdatedByName: string;
}

export class WorkflowTypeNew {
	ID: number;
	Name: string;
	Description: string;
	CreatedBy: number;
	CreatedOn: string;
	UpdatedBy: number;
	UpdatedOn: string;
	PublishedVersionID: number;
	Deleted: boolean = false;
	State: State = State.Active;
}

export class WorkflowVersion {
	ID: number;
	TypeID: number;
	Version: number = 1;
	UID: string;
}

//#region diagram

export class WorkflowDiagramModel {
	Type: WorkflowTypeNew = new WorkflowTypeNew();
	Event: WorkflowEventRegistration = new WorkflowEventRegistration();
	Nodes: WorkflowDiagramNode[] = [];
	Links: WorkflowDiagramLink[] = [];

	CurrentVersion: WorkflowVersion;
	PublishedVersion: WorkflowVersion;

}

export class WorkflowDiagramNode {
	Key: string;
	XPosition: string;
	YPosition: string;
	StepType: StepType;
	ActivityType: number;
	ActivityTypeInfo: ActivityTypeInfo;
	Settings: string;
	SettingsObject: any;
	Fields: string;
	FieldsObject: any;
	Name: string;
	RunCount: number;
}

export class WorkflowDiagramLink {
	Key: string;
	FromKey: string;
	ToKey: string;
	FromPortID: string;
	ToPortID: string;
	TransitionType: TransitionType;
	Condition: string;
	ConditionObject: any;
	Settings: string;
	SettingsObject: any;
	Name: string;
}

export class LinkModel {
	key: string;
	from: string;
	to: string;
	name: string;
	category: string = '';
	diagramObjectType: DiagramObjectType = DiagramObjectType.Link;
	frompid: string;
	topid: string;
	icon: string;

	transitionType: TransitionType = TransitionType.Always;
	condition: any = [];
	settings: any = {};
	formInputs: any = [];
	httpInputs: any = [];
	httpResponseInputs: any = [];

	valid: boolean = true;
	errors: string[] = [];
}

export class NodeModel {
	key: string;
	name: string;
	pos: string;
	category: string = 'task';

	diagramObjectType: DiagramObjectType = DiagramObjectType.Node;

	x: string;
	y: string;
	stepType: StepType;
	activityType: number;
	fore: string;
	back: string;
	icon: string;
	activityDescription: string;
	activityName: string;
	runCount: number;

	settings: NodeSettings = new NodeSettings();
	fields: NodeFields = new NodeFields();

	hasMultipleInputs: boolean = false;
	valid: boolean = false;
	errors: string[] = [];
}

export class FieldUpdateSettings {
	Field: any[] = [];
}

export class RelationshipUpdateSettings {
	Relationship: any;
}

export class HTTPRequestSettings {
	Timeout: number = 90;
	Method: string;
	Url: string;
	Body: string;
	Headers: any[] = [];
	lookupFieldsPassedByValue: any;
}

export class HTTPResponseOutput {
	StepId: string;
	StepName: string;
	Id: string;
	Name: string;
	Type: string = 'text';
	Format: string = 'json';
	Path: string;
}

export class HTTPResponseSettings {
	InputStepId: string;
	InputStepName: string;
	Outputs: HTTPResponseOutput[] = [];
}

export class NodeSettings {
	Status: any;
	State: any;
	ProcedureID: any;
	FieldUpdate: FieldUpdateSettings;
	RelationshipUpdate: RelationshipUpdateSettings;
	HTTPRequest: HTTPRequestSettings;
	HTTPResponse: HTTPResponseSettings;

	FormResponseType: any;
	MessageRecipientType: any;
	MessageToUser: any;
	ResponsibilityTypeID: any;
	MessageToGroup: any;
	SendFormEmail: any;
	MessageBodyTemplate: any;
	MessageSubjectTemplate: any;
	ResponsibilitySide: any;
	SendToDefaultUsers: any;

	IncludePreviousFormResponses: any;
	WaitForAllTransitions: any;
}

export class NodeFields {
	form: FormField = new FormField();
}

export class FormField {
	field: any[] = [];
}


export class ActivityTypeInfo {
	ID: number;
	Name: string;
	Description: string;
	BackColor: string;
	ForeColor: string;
	Icon: string;
	IsShow: boolean = true;
}

export class TransitionTypeInfo {
	ID: Number;
	Name: string;
	Description: string;
}

//#endregion

export class WorkflowFormField {
	Label: string;
	FieldType: WorkflowFormFieldType;
	Value: any;
	ID: string;
	AllowMultipleValues: boolean;
	Required: boolean;

}

export class WorkflowForm {
	Fields: WorkflowFormField[] = [];
	Title: string;
	Description: string;
	IsCompleted: boolean;
	IsItemDeleted: boolean;
	IsUserAllowedToComplete: boolean;
	IsFormInvalid: boolean;
	ObjectName: string;
	ObjectType: string;
	ObjectTypeID: number;
	ObjectID: number;
	IssueObject: string;
	IssueObjectID: number;
	IssueObjectName: string;
	IssueTypeName: string;
	TypeName: string;
	AllowReassignObject: boolean;
	AllowReassignResource: boolean;
	IsClearAssignementsAllowed: boolean;
	ObjectUid: string;
}

export class WorkflowTypeItem {
	WorkflowTypeUid: string;
	ActionTypeUid: string;
	ActionType: string;
	AssetTypeUid: string;
	AssetType: string;
	RelationshipTypeUid: string;
	RelationshipType: string;
	Name: string;
	State: string;
	ChangeType: string;
	Description: string;
	Type: string;
	PublishedVersionUid: string;
	PublishedVersion: string;
	CreatedOn: string;
	UpdatedOn: string;
	CreatedBy: string;
	UpdatedBy: string;


}

export class WorkflowListItem {
	ID: number;
	CreatedOn: string;
	CreatedBy: string;
	UpdatedOn: string;
	UpdatedBy: string;
	Name: string;
	TypeName: string;
	ChangeType: WorkflowChangeType;
	Published: string;
	NumberOfEvents: number;
	VersionID: number;
	ItemID: number;
	ChangeTypeName: string;
	State: State;
	Type: string;
	Uid: string;
}

export class WorkflowEventRegistration {
	ID: number = 0;
	TypeID: number;
	Object: string;
	ObjectID: number;
	ChangeType: WorkflowChangeType;
	Condition: string;
	ConditionObject: any = {};
	Settings: string;
	SettingsObject: any = {};
	LastExecuted: any;
	conditions: EventCondition[] = [];
	IssueObject: string = '';
	ScoreType: number;
}

export class WorkflowObjectType {
	value: string;
	id: number;
	type: string;
	label: string;
	count: number;
}

export class ChangeTypeInfo {
	ID: number;
	Name: string;
	Description: string;
}

export class EventCondition {
	FieldTypeID: number = 0;
	Value: any;
	ValueType: string;
	Operator: string;

	fieldName: string;

	//TODO: explore as alternative to mapping manually
	//get FieldTypeID(): number {
	//    return +this['@FieldTypeID'];
	//}

	//set FieldTypeID(val: number) {
	//    this['@FieldTypeID'] = val;
	//}
}


export class WorkflowTaskProcedure {
	ID: number;
	Name: string;
	Procedure: string;
	PassObjectInfo: boolean;
	UpdatedBy: number;
	UpdatedOn: string;
}

//#region enums

export enum WorkflowChangeType {
	Add = 1,
	Update = 2,
	Delete = 3,
	Schedule = 4,
	ScoreUpdate = 5,
	RequestCertification = 8,
}

export enum WorkflowFormFieldType {
	Text = 0,
	Boolean = 1,
	Integer = 2,
	Date = 3,
	TextArea = 4,
	List = 5,
	RelationshipType = 6,
	HTML = 7,
	Link = 8,
}

export enum WorkflowActivityType {
	None = 0,
	EmailNotification = 1,
	StatusChange = 2,
	Form = 3,
	Procedure = 4,
	FieldChange = 5,
	RelationshipUpdate = 6,
	StateChange = 7,
	Delete = 8,
	HTTPRequest = 9,
	HTTPResponse = 10
}

export enum DiagramObjectType {
	Link,
	Node
}

export enum StepType {
	Start = 1,
	Task = 2,
	Terminate = 3,
	Finish = 4
}

export enum TransitionType {
	Always = 1,
	Condition = 2,
	Timer = 3
}

export enum IssueType {
	Issue = 0,
	Challenge = 1
}

export enum WorkflowType {
	None = 0,
	SuggestNewArtifact = 1,
	CertifyArtifact = 2,
	WorkIssue = 3,
	ChallengeArtifact = 4,
	SuggestNewArtifactMulti = 5,
}

export enum FormResponseType {
	FirstResponse = 0,
	All = 1,
	Majority = 2
}

export enum EmailTaskRecipientType {
	None = 0,
	Initiator,
	Responsibility,
	SpecificUser,
	Followers,
	Group
}

export enum ConditionFieldType {
	Field,
	Form,
	Contextual,
	HttpRequest,
	HttpResponse
}

//#endregion

export class WorkflowAssignmentSummary {

	Version: number;
	StepName: string;
	ObjectName: string;
	TypeName: string;
	SendFormEmail: boolean;
}

export class WorkflowAssignmentDetail {
	ItemID: number;
	ItemStepID: number;
	Object: string;
	ObjectID: number;
	ObjectName: string;
	ObjectType: string;
	ObjectTypeID: number;
	StartedBy: string;
	StartedByResourceID: number;
	StartedOn: Date;
	TypeName: string;
	WorkflowName: string;
	StepName: string;
	StepType: StepType;
	ActivityType: WorkflowActivityType;
	responseType: string;
	countAssigned: number;
}

export class WorkflowItemStep {
	ID: number;
	ItemID: number;
	StepID: number;
	Name: string;
	StepType: StepType;
	ActivityType: WorkflowActivityType;
	Assignee: string;
	Complete: boolean;
	StartedOn: string;
	StartedBy: string;
	CompletedOn: string;
	CompletedBy: string;
	Fields: string;
	IsIssueType: boolean;
	Object: string;
	ObjectID: number;
	TypeID: number;
	IsAssignedLoginUser: string;
	FieldsObject: Record<string, any>;
}

export class AssignmentItemStep {
	ActivityType: string;
	Assignments: { AssigneeUid: string }[];
	CompletedByUid: string;
	CompletedOn: string;
	Name: string;
	Responses: {
		fields: {
			'@TotalResources': string;
		}
	};
	Settings: {
		settings: {
			FormResponseType: string;
			IncludePreviousFormResponses: string;
			MessageRecipientType: string;
			MessageToUser: string;
			SendFormEmail: string;
		},
		fields: {
			form: {
				'@title': string;
				field: {
					'@id': string;
					'@label': string;
					'@type': string;
				}
			}
		}
	};
	StartedByUid: string;
	StartedOn: string;
	State: string;
	StepType: string;
	Uid: string;
}

export class AssignmentItem {
	WorkflowItemUid: string;
	WorkflowUid: string;
	WorkflowName: string;
	Initiator: string;
	InitiatorUid: string;
	StartedOn: string;
	CompletedOn: string;
	Status: string;
	AssetUid: string;
	AssetPath: string;
	ActionUid: string;
	ChangeType: string;
	initiatingObjectType: string;
}

export class BulkWorkflowFormModel {
	ItemStepIDs: number[] = [];
	Fields: WorkflowFormField[] = [];
}

export class BulkWorkflowReassignModel {
	ItemStepIDs: number[] = [];
	StepHasFormEmails: boolean;
	OriginalAssigneeResourceID: number = -1;
	OriginalAssigneeResourceName: string = '[unknown user]';
	NewAssigneeResourceID: number = -1;
	NewAssigneeResourceName: string = '';
	StepName: string = 'Form';
	SendFormEmails: boolean = true;
	IsClearOtherAssignmentsAllowed: boolean = false;
	ClearOtherAssignments: boolean = false;
}


export class EmailTaskRecipientTypeInfo {
	ID: number;
	Name: string;
}

export class WorkflowStepDetail {
	ActivityType: WorkflowActivityType;
	AssetId: number;
	AssignedUsers: WorkflowStepAssignedUser[] = [];
	ChangeType: WorkflowChangeType;
	CompletedBy: number;
	CompletedOn: string;
	Condition: any;
	ConditionXml: string;
	EventSettings: any;
	EventSettingsXml: string;
	FieldChanges: WorkflowStepFieldChangeDetail[];
	Fields: {
		form: {
			'@title': string;
			field: [
				{
					'@type': string;
					'@label': string;
					'@required': string;
					'@id': string;
				}
			]
		}
	};
	FieldsXml: string;
	ID: number;
	IsAssignedLoginUser: boolean;
	IsIssueType: boolean;
	IsPublishedVersion: boolean;
	IssueDetails: WorkflowStepIssueDetail;
	ItemFields: WorkflowStepItemFields;
	ItemFieldsXml: string;
	ItemID: number;
	ItemSettings: WorkflowStepItemSettings;
	ItemSettingsXml: string;
	ItemStepID: number;
	Name: string;
	Object: string;
	ObjectID: number;
	ObjectName: string;
	ObjectType: string;
	ObjectTypeID: number;
	ObjectTypeName: string;
	RelationshipChange: WorkflowStepRelationshipChangeDetail;
	Settings: NodeSettings;
	SettingsXml: string;
	StartedBy: number;
	StartedOn: string;
	StateChange: State;
	StepID: number;
	StepType: StepType;
	TypeID: number;
	Version: number;
}

export class WorkflowStepItemFields {
	form: any;
	Reassigned: any;
}

export class WorkflowStepItemSettings {
	emails: any;
	hasPendingForms: boolean;
	hasEmails: boolean;
	Responsibilities: any;
}

export class WorkflowStepAssignedUser {
	CreatedOn: string;
	Email: string;
	FirstName: string;
	FullName: string;
	IsAdministrator: boolean;
	LastLoggedInOn: string;
	LastName: string;
	ResourceID: number;
	State: number;
	Uid: string;
	UpdatedOn: string;
}

export class WorkflowStepReassignment {

	constructor(reassignObject: any = null) {
		if (reassignObject != null) {
			this.ReassignType = reassignObject['@reassignType'];
			this.ObjectType = reassignObject['@objectType'];
			this.ObjectID = reassignObject['@objectId'];
			this.ObjectUid = reassignObject['@objectUid'];
			this.ObjectName = reassignObject['@objectName'];
			this.ByResourceID = reassignObject['@byResourceId'];
			this.FromResourceID = reassignObject['@fromResourceId'];
			this.ToResourceID = reassignObject['@toResourceId'];
			this.ByResourceName = reassignObject['@byResourceName'];
			this.ToResourceName = reassignObject['@toResourceName'];
			this.FromResourceName = reassignObject['@fromResourceName'];
			this.ReassignOn = reassignObject['@reassignOn'];
			this.NewItemId = reassignObject['@newItemId'];
			this.IsBulkReassignment = (this.ReassignType === 'Resource' && this.ByResourceID != null);
		}
	}

	IsBulkReassignment: boolean = false;
	ReassignType: string;
	ObjectType: string;
	ObjectID: number;
	ObjectUid: string;
	ObjectName: string;
	ByResourceID: number;
	ByResourceName: string;
	FromResourceID: number;
	FromResourceName: string;
	ToResourceID: number;
	ToResourceName: string;
	ReassignOn: string;
	NewItemId: number;
}

export class WorkflowStepIssueDetail {
	ID: number;
	IssueID: number;
	IssueTypeID: number;
	IssueName: string;
	ObjectName: string;
	ObjectTypeName: string;
	Object: string;
	ObjectID: number;
	ObjectType: string;
	ObjectTypeID: number;
	AssetId: number;
}

export class WorkflowStepFieldChangeDetail {
	FieldValue: string;
	FieldName: string;
	Asset: string;
	Type: string;
	Value: string;
	UseCurrentDate: boolean;
	FormValue: string;
	AppendValue: string;
	ClearValue: string;
	ObjectType: string;
}


export class WorkflowStepRelationshipChangeDetail {
	TypeName: string;
	Relationship: string;
	AppendValue: boolean;
	ClearValue: boolean;
}

export class ActionEditorModel {
	AssetUid: string;
	AssetTypeUid: string;
	Fields: any;
}

export class AllocationResponsibilityModel {
	Name: string;
	Uid: string;
}

export class AllocationAPIModel {
	AssetTypeUid: string;
	Name: string;
	Class: number;
	Path: string;
	ClassName: string;
	Responsibilities: AllocationResponsibilityModel[];
}

export class AllocationRequestModel {
	AssetTypeUid: string;
	ResponsibilityTypeUid: string[];
}

export class WorkflowReassignmentAsset {
	ID: number;
	Name: string;
	Object: string;
	ObjectID: number;
}

export interface WorkflowTypeModel {
	WorkflowTypeUid: UID;
	ActionTypeUid: UID;
	ActionType: string;
	AssetTypeUid: UID;
	AssetType: string;
	RelationshipTypeUid: UID;
	RelationshipType: string;
	Name: string;
	State: State;
	ChangeType: WorkflowChangeType;
	Description: string;
	Type: string;
	PublishedVersionUid: UID;
	PublishedVersion: number;
	CreatedOn: string;
	UpdatedOn: string;
	CreatedBy: string;
	UpdatedBy: string;
	label?: string;
	value?: string;
	ID?: number;
}

export class WorkflowAssignments {
	items: WorkflowAssignmentItem[] = [];
	pageSize: number;
	pageNum: number;
	total: number;
}

export class WorkflowAssignmentItem {
	workflowItemUid: string;
	workflowUid: string;
	workflowName: string;
	initiator: string;
	initiatorUid: string;
	assetDisplayValue: string;
	StartedOn: string;
	CompletedOn: string;
	Status: string;
	assetTypeUid: string;
	actionTypeUid: string;
	assetUid: string;
	assetPath: string;
}

export class WorkflowByType {
	TypeID: number;
	Name: string;
	VersionName: string;
	Version: number;
	UpdatedOn: string;
	UpdatedBy: string;
	ObjectTypeName: string;
	Object: string;
	ObjectID: number;
	NgUrl: string;
	VersionID: number;
	ObjectNames: string;
	Responsibility: string;
	SpecificUser: string;
	Status: string;
	CurrentStepID: number;
	Settings: string;
	ActivityType: number;
	StepType: number;
	ResponsibleUser: string;
	StartedBy: string;
}

export class AssignmentByVersion {
	WorkflowName: string;
	WorkflowTypeUid: string;
	Version: number;
	Awaiting: number;
	Incomplete: number;
	ChangeType: string;
	InitiatingObjectType: string;
	CreatedOn: string;
	CreatedBy: string;
	CreatedByUid: string;
	UpdatedOn: string;
	UpdatedBy: string;
	UpdatedByUid: string;
}

export class Actions {
	pageSize: number;
	pageNum: number;
	total: number;
	items: [
		{
			Uid: string;
			CompletedOn: string;
			AssetUid: string;
			AssetTypeUid: string;
			AssetTypeName: string;
			ActionTypeUid: string;
			ActionTypeName: string;
			CreatedOn: string;
			CreatedByUid: string;
			UpdatedOn: string;
			UpdatedByUid: string;
		}
	];
}
