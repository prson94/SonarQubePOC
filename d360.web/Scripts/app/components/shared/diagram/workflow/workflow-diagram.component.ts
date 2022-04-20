import { MenuItem } from 'primeng/api';
import * as go from 'gojs';
import * as _ from 'lodash';
import {
    Component,
    Input,
    OnInit,
    ElementRef,
    ViewChild,
    HostListener,
    Output,
    EventEmitter,
    OnChanges,
    SimpleChanges
} from '@angular/core';

import { PermissionsService } from '../../../../services/permissions.service';
import { DiagramBaseComponent } from '../diagram-base.component';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import { ObjectDetailService } from '../../../../services/object-detail.service';
import { UriBasedService } from '../../../../services/uri-based.service';
import {
    WorkflowDiagramModel,
    WorkflowDiagramNode,
    WorkflowDiagramLink,
    NodeModel,
    LinkModel,
    DiagramObjectType,
    StepType,
    TransitionType,
    ActivityTypeInfo,
    WorkflowChangeType,
    FormResponseType,
    WorkflowActivityType,
    NodeSettings,
    NodeFields,
    HTTPRequestSettings,
    RelationshipUpdateSettings,
    FieldUpdateSettings,
    HTTPResponseSettings,
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { map, concatMap } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { CompanySettingsService } from '../../../../services/settings.service';
 
declare var window: any;

@Component({
    selector: 'd3s-workflow-diagram',
    templateUrl: './workflow-diagram.component.html',
    providers: [
        PermissionsService,
        WorkflowService,
        ObjectDetailService,
        UriBasedService
    ]
})
export class WorkflowDiagramComponent extends DiagramBaseComponent implements OnInit, OnChanges {
    @Input() id: number = 0;
    @Input() uid: string = "00000000-0000-0000-0000-000000000000";
    @Input() model: WorkflowDiagramModel;
    @Input() version: number = null;
    @Input() readonly: boolean = true;
    @Input() hasClose: boolean = false;
    @Input() hasBack: boolean = false;
    @Input() hasHeader: boolean = true;
    @Input() selection: NodeModel | LinkModel;
    @Input() selectedStepId: string;
    @Input() monitorView: boolean = false;
    @Input() filteredObject: string;
    @Input() filteredObjectId: number;
    @Output() selectedStepIdChange = new EventEmitter();
    @Output() onCloseClick = new EventEmitter();
    @Output() onBackClick = new EventEmitter();
    @Output() selectionChange = new EventEmitter();
    @ViewChild('workflowDiagram', { static: true }) diagramRef;
    @ViewChild('workflowPalette', { static: true }) paletteRef;

    private activityTypes: ActivityTypeInfo[] = [];
    DiagramObjectType = DiagramObjectType;
    StepType = StepType;
    TransitionType = TransitionType;
    WorkflowChangeType = WorkflowChangeType;
    WorkflowActivityType = WorkflowActivityType;
    FormResponseType = FormResponseType;

    fieldTypes: FieldType[] = [];

    //diagram properties
    private initialLinks: go.Link[] = [];
    private initialNodes: go.Node[] = [];
    private selectedData = null;
    private conditions: any[] = [];
    private newKey = -1;
    private HideSqlProcedure: boolean = false;

    menuItems: MenuItem[] = [];
    private isWindowVisible = false;
    isReadOnly: boolean = true;
    private tab = 'info';
    private showNodeTabs = false;
    private showLinkTabs = false;
    private overlayHeader = 'Info';
    private overlayMaxHeight = 500;
    private overlayWidth = 500;

    private objectTypeName = null;

    //hard-coded offsets for diagram and overlay. Avoids issues with rendering completing after ngAfterViewInit()
    private overlayOffset = 391;
    private diagramOffset = 291;

    private hasType = false;

    private formFields: any[] = [];
    private fieldsSub: any;

    private isValid = true;
    private errors: string[] = [];

    constructor(
        private myElement: ElementRef,
        protected permissionsService: PermissionsService,
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService,
        private uriBasedService: UriBasedService,
        private objectDetailService: ObjectDetailService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }
    
    _isLoadingCounter = 0;

    get isLoadingCounter() { 
        return this._isLoadingCounter;
    }

    set isLoadingCounter(value) {
        this._isLoadingCounter = value;
        this.isLoading = this._isLoadingCounter > 0;
    }

    //#region angular

    public ngOnInit() {
        if (this.monitorView) {
            this.overlayWidth = 600;
            this.tab = 'history';
            this.isWindowVisible = true;
            this.HideSqlProcedure = false;
        }

        this.isLoadingCounter++;
    }

    public ngOnChanges(changes: SimpleChanges) {
        let isModelPassed = changes['model'] != null && changes['model'].currentValue != changes['model'].previousValue;
        let isVelueReadOnly = changes['readonly'] != null && changes['readonly'].currentValue != changes['readonly'].previousValue;
        let isIdChanged = changes['id'] != null && changes['id'].currentValue != changes['id'].previousValue;
        let isVersionChanged = changes['version'] != null && changes['version'].currentValue != changes['version'].previousValue
        let isUidChanged = changes['uid'] != null && changes['uid'].currentValue != changes['uid'].previousValue;
        let isSelectedStepIdChanged = changes['selectedStepId'] && changes['selectedStepId'].currentValue != changes['selectedStepId'].previousValue;

        if (isVelueReadOnly) {
            this.isReadOnly = this.readonly.toString().toLowerCase() == 'true' ? true : false;
        }

        if (isModelPassed) {
            this.selectedData = null;
            this.initializeDiagram();
            this.initializeMenuItems();
            this.resizeDiagram();

            this.load();
        }
        //else we need at least an id and preferably a id/version combo
        //without a version we just load the most recent one
        else if (isIdChanged || isVersionChanged || isUidChanged) {
            if (this.diagram != null && this.diagram.div != null) {
                this.diagram.div = null;
            }
            if (this.palette != null && this.palette.div != null) {
                this.palette.div = null;
            }

            this.model = null;
            this.selectedData = null;
            this.initializeDiagram();
            this.initializeMenuItems();
            this.resizeDiagram();

            this.load();
        }

        //if a step selection binding changes, select the appropriate step and show the history for it
        if (isSelectedStepIdChanged) {
            this.diagram.clearSelection();
            let part = this.diagram.findPartForKey(changes['selectedStepId'].currentValue);
            let node = this.diagram.findNodeForKey(changes['selectedStepId'].currentValue);
            if (part) part.isSelected = true;
            if (node) {
                this.selectedData = node.data;
                this.selectionChange.emit(node.data);
                this.tab = 'history';
                this.isWindowVisible = true;
            }
        }
    }

    public ngOnDestroy() {
        //garbage collection
        if (this.diagram != null && this.diagram.div != null)
            this.diagram.div = null;
        if (this.fieldsSub != null)
            this.fieldsSub.unsubscribe();
    }

    //#endregion

    //#region initialization

    private initializeDiagram() {

        this.diagram = this.createDiagram();

        this.diagram.nodeTemplateMap.add('task', this.createTaskNode());
        this.diagram.nodeTemplateMap.add('start', this.createTerminalNode(true));
        this.diagram.nodeTemplateMap.add('finish', this.createTerminalNode(false));
        this.diagram.linkTemplateMap.add('', this.createDefaultLink());

        this.diagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.diagram.addDiagramListener('LinkDrawn', e => this.LinkDrawn(e));
        this.diagram.addDiagramListener('PartCreated', () => this.checkHasMultipleInputs());
        this.diagram.addDiagramListener('ExternalObjectsDropped', e => this.ExternalObjectsDropped(e));
        this.diagram.addDiagramListener('ClipboardPasted', e => this.ClipboardPasted(e));

        this.diagram.grid.visible = false;
        this.diagram.grid.gridCellSize = new go.Size(24, 24);
        this.diagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.diagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.diagram.toolManager.linkingTool.linkValidation = (a, b, c, d) => this.canLink(a, b, c, d);

        this.diagram.toolManager.linkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.relinkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.diagram.toolManager.linkingTool.isEnabled = !this.isReadOnly;
        this.diagram.toolManager.linkingTool.archetypeLinkData = new LinkModel();

        this.diagram.commandHandler.deleteSelection = () => this.deleteSelection();

        this.diagram.validCycle = go.Diagram.CycleAll; //disallow cycles
        this.diagram.maxSelectionCount = 1; //only select 1 item at a time, this makes handling selections a lot easier
    }

    private initializePalette() {
        this.palette = this.createPalette();
    }

    private initializeFormFields() {

        if (this.fieldsSub != null) {
            this.fieldsSub.unsubscribe();
            this.fieldsSub = null;
        }

        this.workflowFieldsService.clearUsedFields();

        this.formFields = [];
        this.diagram.model.nodeDataArray.forEach(n => {

            if (+(<NodeModel>n).activityType == WorkflowActivityType.Form
                && (<NodeModel>n).fields != null
                && (<NodeModel>n).fields.form != null
                && (<NodeModel>n).fields.form.field != null
                && (<NodeModel>n).fields.form.field.length != null) {

                (<NodeModel>n).fields.form.field.forEach(f => {
                    let ff = {};
                    ff['@id'] = f['@id'];
                    ff['@label'] = f['@label'];
                    ff['@FieldName'] = 'Form :: ' + f['@label'];
                    ff['@type'] = f['@type'];
                    if (f['@referenceFieldId'] != null)
                        ff['@referenceFieldId'] = f['@referenceFieldId'];
                    if (f['@intersectTypeId'] != null)
                        ff['@intersectTypeId'] = f['@intersectTypeId'];
                    ff['@stepId'] = (<NodeModel>n).key;
                    ff['@VersionStepID'] = (<NodeModel>n).key;

                    this.formFields.push(ff);
                });

            }
        });

        this.workflowFieldsService.clearFormFields();
        this.workflowFieldsService.setFormFields(this.formFields);

        (<go.GraphLinksModel>this.diagram.model).linkDataArray.forEach(l => {
            if ((<LinkModel>l).condition != null && (<LinkModel>l).condition.length > 0) {
                (<LinkModel>l).condition.forEach(c => {
                    let i = this.formFields.findIndex(f => f['@id'] == c['@FormInputID'] && f['@VersionStepID'] == c['@VersionStepID']);

                    if (i > -1) {
                        c['@FieldName'] = this.formFields[i]['@FieldName'];
                    }

                    this.workflowFieldsService.pushUsedField(c['@FormInputID'], c['@VersionStepID'], (<LinkModel>l).key, (<LinkModel>l).name);
                });
            }

        });

        if (this.fieldsSub == null)
            this.fieldsSub = this.workflowFieldsService.formFields$.subscribe(s => {
                this.formFields = s;
            });
    }

    private initializeMenuItems() {
        this.menuItems = [];

        this.menuItems.push({
            icon: 'fa fa-info-circle'
        });
        if (this.hasClose)
            this.menuItems.push({
                icon: 'fa fa-remove'
            });
    }

    //#endregion

    //#region save/load

    private populateDiagram(): Observable<any> {

        //load from model
        if (this.model != null) {
            if (this.model.Event == null || this.model.Event.Object == null || this.model.Event.ObjectID == null) {
                console.warn('Model passed to workflow diagram with no Event Registration data.');
                this.isLoadingCounter--;
                return of();

            }

            return this.workflowService.getWorkflowFieldTypes(this.model.Event.ObjectID, this.model.Event.Object, true, this.model.Event.IssueObject)
                .pipe(
                    map(r => this.fieldTypes = r),
                    map(() => this.parseData(this.model)),
                    map(() => { this.isLoadingCounter--; }),
                    map(() => { this.resetContentPosition() })
                );
        }

        //if we don't have at least an id at this point, there's nothing we can do
        if (!this.id && this.uid == "00000000-0000-0000-0000-000000000000") {
            this.isLoadingCounter--;
            return of();
        }

        this.isLoadingCounter++;

        return this.workflowService.getWorkflowDiagram(this.id,this.uid, this.version, this.filteredObject, this.filteredObjectId)
            .pipe(
                map(r => {
                    this.model = r;
                    if (this.model.Nodes != null)
                    this.model.Nodes.forEach(n => n.ActivityTypeInfo = this.activityTypes.find(a => a.ID == n.ActivityType));
                    }),
                map(() => this.workflowService.getWorkflowFieldTypes(this.model.Event.ObjectID, this.model.Event.Object, true, this.model.Event.IssueObject)
                    .subscribe(r => this.fieldTypes = r)),
                map(() => this.parseData(this.model)),
                map(() => this.setIssueObject()),
                map(() => {
                    this.isLoadingCounter--;
                    this.hasType = true;
                    this.resetContentPosition();
                    })
                );
    }

    private setIssueObject() {
        if (this.model.Event.ConditionObject != null && this.model.Event.ConditionObject.Condition != null && typeof this.model.Event.ConditionObject.Condition.length != 'undefined') {
            this.conditions = [];
            this.model.Event.ConditionObject.Condition.forEach(function (cond) {
                this.conditions.push(cond);
            }, this);
        }

        if (this.conditions.length > 0) {
            let objectIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObject');
            let objectIdIndex = this.conditions.findIndex(c => c['@ContextualFieldID'] == 'IssueObjectID');
            if (objectIndex > -1 && objectIdIndex > -1) {
                this.model.Event.IssueObject = this.conditions[objectIndex]['@Value'] + '|' + this.conditions[objectIdIndex]['@Value'];
            }
        }
    }

    private parseData(data: WorkflowDiagramModel) {
        this.diagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.diagram.model;
        dm.nodeDataArray = [];
        dm.linkDataArray = [];
        this.initialNodes = [];
        this.initialLinks = [];
        var nodeList = [];
        var linkList = [];

        if (data.Nodes)
            data.Nodes.forEach(n => {
                nodeList.push(<NodeModel>this.convertToDiagramModel(n, DiagramObjectType.Node))
            });

        if (data.Links)
            data.Links.forEach(l => {
                linkList.push(<LinkModel>this.convertToDiagramModel(l, DiagramObjectType.Link))
            });

        nodeList.forEach(n => {
            if (n.activityType == WorkflowActivityType.FieldChange) {
                if (n.settings != null && n.settings.FieldUpdate != null && n.settings.FieldUpdate.Field != null) {


                    let fields = n.settings.FieldUpdate.Field;

                    if (fields.length != null && fields.length > 0) {
                        fields.forEach(field => {
                            if (field['@UseFormValue'] != null && field['@UseFormValue'].toString() == 'true') {
                                if (field['@FormStepId'] != null && field['@FormFieldId'] != null) {
                                    let formNode = nodeList.find(n => n.key == field['@FormStepId'].toString());
                                    if (formNode != null && formNode.fields != null && formNode.fields.form != null && formNode.fields.form.field != null) {
                                        let formField = formNode.fields.form.field.find(f => f['@id'] == field['@FormFieldId']);
                                        if (formField != null) {
                                            field['@FormLabel'] = 'Form :: ' + formField['@label'];
                                        }
                                    }
                                }
                            }

                            if (field['@UseOutputValue'] != null && field['@UseOutputValue'].toString() == 'true') {
                                if (field['@FormStepId'] != null && field['@FormFieldId'] != null) {
                                    let outputNode = nodeList.find(n => n.key == field['@FormStepId'].toString());
                                    if (outputNode != null && outputNode.settings != null && outputNode.settings.HTTPResponse != null && outputNode.settings.HTTPResponse.Outputs != null) {
                                        let outputField = outputNode.settings.HTTPResponse.Outputs.find(f => f.Id == field['@FormFieldId']);
                                        if (outputField != null) {
                                            field['@FormLabel'] = 'HTTP Response :: ' + outputField.Name;
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
            }

            if (n.activityType == WorkflowActivityType.HTTPRequest) {
                this.workflowFieldsService.pushHttpFields(n);
            }

            this.diagram.model.addNodeData(n)
        });
        linkList.forEach(l => dm.addLinkData(l));

        dm.linkDataArray.forEach(l => {
            (<LinkModel>l).formInputs = this.getAvailableFormInputs(<LinkModel>l);
            (<LinkModel>l).httpInputs = this.getAvailableHttpInputs(<LinkModel>l);
            (<LinkModel>l).httpResponseInputs = this.getAvailableHttpOutputs(<LinkModel>l);
        });

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(nodeList);

        this.checkHasMultipleInputs();

        this.diagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private save(publish: boolean = false) {

        let links = []; //(<go.GraphLinksModel>this.myDiagram.model).linkDataArray;
        let nodes = []; //this.myDiagram.model.nodeDataArray;
        var types = this.fieldTypes;
        this.diagram.model.nodeDataArray.forEach(n => {
            if ((<NodeModel>n).activityName === "FieldChange") {
                ((<NodeModel>n).settings.FieldUpdate.Field).forEach(function (fieldNode) {
                    var fieldData = fieldNode["@FieldName"].split("::", 2);
                    if (fieldData.length == 2) {
                        var fieldId = +fieldNode["@FieldId"];
                        var object = fieldData[0];
                        var objectName = fieldData[1];
                        var f = types.filter(x => x.ID == fieldId && x.Object == object && x.Name == objectName)[0];
                        if (f != undefined)
                            fieldNode["@ObjectType"] = object;
                    }
                });
            }

            nodes.push(this.convertToWorkflowModel(<NodeModel>n));
        });

        (<go.GraphLinksModel>this.diagram.model).linkDataArray.forEach(l => {
            links.push(this.convertToWorkflowModel(<LinkModel>l));
        });

        let m = new WorkflowDiagramModel();

        this.model.Type.PublishedVersionID = publish ? -1 : null;
        m.Type = this.model.Type;
        m.Event = this.model.Event;
        m.Nodes = nodes;
        m.Links = links;

        this.isLoadingCounter++;

        this.workflowService.saveWorkflowDiagramModel(m)
            .subscribe(r => {
                this.onCloseClick.emit();
            });
    }
   
    private load() {
        this.getActivityTypes()
            .pipe(
                concatMap(() => of(this.workflowFieldsService.clearHttpRequestFields())),
                concatMap(() => this.populateDiagram()),
                concatMap(() => of(this.initializePalette())),
                concatMap(() => of(this.initializeFormFields())),
                concatMap(() => of(this.getObjectName())),
                concatMap(() => of(this.setWorkflowFields())),
            concatMap(() => of(this.isWindowVisible = (this.monitorView || !this.isReadOnly)))
            ).subscribe();
    }

    private setWorkflowFields() {
        this.workflowFieldsService.setWorkflow(this.model.Event.Object, this.model.Event.ObjectID, this.model.Event.ChangeType);

        let type = this.model.Event.Object;
        let id = this.model.Event.ObjectID;

        if (this.model.Event.IssueObject != null && this.model.Event.IssueObject.indexOf('|') > -1) {
            type = this.model.Event.IssueObject.split('|')[0];
            id = +this.model.Event.IssueObject.split('|')[1];
        }

        if (this.model.Event.ChangeType == WorkflowChangeType.ScoreUpdate
            || this.model.Event.ChangeType == WorkflowChangeType.Update
            || this.model.Event.ChangeType == WorkflowChangeType.RequestCertification
            || this.model.Event.ChangeType == WorkflowChangeType.Schedule) {
            this.workflowService.getScoreTypes(id, type)
                .subscribe(res => {
                    this.workflowFieldsService.setAvailableScoreTypes(res);
                });
        }
    }

    //#endregion

    //#region helper methods

    private resetContentPosition() {
        setTimeout(() => {
            this.diagram.alignDocument(go.Spot.TopLeft, go.Spot.TopLeft);
            this.diagram.requestUpdate();
        }, 200);
    }

    private canLink(fromNode: any, fromPort: any, toNode: any, toPort: any) {

        //can't link to self
        if (fromNode.data.key == toNode.data.key)
            return false;
        //forms can always link, even backwards creating a cycle
        if (fromNode.data.activityType == WorkflowActivityType.Form && fromNode.data.key != toNode.data.key)
            return true;
        if (toNode.data.activityType == WorkflowActivityType.Form && fromNode.data.key != toNode.data.key)
            return true;

        //starting with the toNode, is there a way to traverse back to the fromNode?
        //if so we have a cycle and need to abort
        let links = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).from == toNode.data.key);
        let visitedNodeKeys = [];
        let panic = 0;
        let hasCycle = false;
        while (links != null && links.length > 0) {
            let nodes = [];
            links.forEach(l => {
                let node = this.diagram.model.findNodeDataForKey((<any>l).to);
                //a form step is part of this cycle, so it's valid
                if (node.activityType == WorkflowActivityType.Form) {
                    nodes = [];
                    return;
                }

                if (node.key == fromNode.data.key) { //we found a cycle
                    hasCycle = true;
                    return;
                }
                if (visitedNodeKeys.indexOf(node.key) > -1)
                    return;
                visitedNodeKeys.push(node.key);
                nodes.push(node);
            });

            if (hasCycle)
                return false;

            links = [];
            nodes.forEach(n => {
                let newLinks = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).from == n.key);
                links = links.concat(newLinks);
            });
        }

        return true;
    }

    private getObjectName() {
        if (this.objectTypeName != null || this.monitorView || !this.hasHeader)
            return;

        let obj = this.model.Event.Object;
        this.objectDetailService.getObject(this.model.Event.ObjectID, obj)
            .subscribe(
            r => {
                if (r != null) {
                    this.objectTypeName = r.TypeName + ' :: ' + r.Name;
                } else {
                    this.objectTypeName = '';
                }
            }
        );
    }

    private getAvailableFormInputs(link: LinkModel): string[] {
        let links = [];
        let forms = [];
        let visited = [];

        let nodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).key == link.from);
        visited = visited.concat(nodes.map(n => {
            return (<any>n).key;
        }));

        while (nodes.length > 0) {
            links = [];
            nodes.forEach(n => {
                if ((<NodeModel>n).activityType == WorkflowActivityType.Form) {
                    forms.push((<NodeModel>n).key);
                }

                links = links.concat((<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<LinkModel>l).to == (<NodeModel>n).key));
            });
            nodes = [];
            links.forEach(l => {
                let newNodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).key == (<any>l).from && visited.findIndex(v => v == (<any>n).key) == -1);
                nodes = nodes.concat(newNodes);
                visited = visited.concat(newNodes.map(n => {
                    return (<any>n).key;
                }));
            });
        }

        return forms;
    }

    private getAvailableHttpInputs(link: LinkModel): string[] {
        let links = [];
        let requests = [];
        let visited = [];

        let nodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).key == link.from);
        visited = visited.concat(nodes.map(n => {
            return (<any>n).key;
        }));

        while (nodes.length > 0) {
            links = [];
            nodes.forEach(n => {
                if ((<NodeModel>n).activityType == WorkflowActivityType.HTTPRequest) {
                    requests.push((<NodeModel>n).key);
                }

                links = links.concat((<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<LinkModel>l).to == (<NodeModel>n).key));
            });
            nodes = [];
            links.forEach(l => {
                let newNodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).key == (<any>l).from && visited.findIndex(v => v == (<any>n).key) == -1);
                nodes = nodes.concat(newNodes);
                visited = visited.concat(newNodes.map(n => {
                    return (<any>n).key;
                }));
            });
        }
        return requests;
    }

    private getAvailableHttpOutputs(link: LinkModel): string[] {
        let links = [];
        let requests = [];
        let visited = [];

        let nodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).key == link.from);
        visited = visited.concat(nodes.map(n => {
            return (<any>n).key;
        }));

        while (nodes.length > 0) {
            links = [];
            nodes.forEach(n => {
                if ((<NodeModel>n).activityType == WorkflowActivityType.HTTPResponse) {
                    requests.push((<NodeModel>n).key);
                }

                links = links.concat((<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<LinkModel>l).to == (<NodeModel>n).key));
            });
            nodes = [];
            links.forEach(l => {
                let newNodes = this.diagram.model.nodeDataArray.filter(n => (<any>n).key == (<any>l).from && visited.findIndex(v => v == (<any>n).key) == -1);
                nodes = nodes.concat(newNodes);
                visited = visited.concat(newNodes.map(n => {
                    return (<any>n).key;
                }));
            });
        }
        return requests;
    }

    private getActivityTypes(): Observable<any> {
        return this.workflowService.getActivityTypes()
            .pipe(
                map(r => {
                    let excluded = r.findIndex(a => a.ID == WorkflowActivityType.None);

                    if (excluded >= 0)
                    r.splice(excluded, 1);

                    excluded = r.findIndex(a => a.ID == WorkflowActivityType.StatusChange); //deprecated
                    if (excluded >= 0)
                    r.splice(excluded, 1);

                    excluded = r.findIndex(a => a.ID == WorkflowActivityType.Procedure && a.IsShow == false); 
                    if (excluded >= 0)
                    {
                        r.splice(excluded, 1);
                        this.HideSqlProcedure = true;
                    }
                        

                    excluded = r.findIndex(a => a.ID == WorkflowActivityType.StateChange);
                    if (excluded >= 0)
                        r.splice(excluded, 1);

                    this.activityTypes = r;
                })
            );

    }

    private setOverlayHeaderName(p: any) {
        if (p == null) {
            this.overlayHeader = this.tab;
        } else {
            let a = this.activityTypes.find(a => a.ID == p.activityType);
            let name = this.getNodeDisplayName(p);
            this.overlayHeader = (a == null) ? name : a.Description + ((name == null || name.toLowerCase() == a.Description.toLowerCase()) ? '' : ' - ' + name);
        }
    }

    private selectTab(s: string) {
        this.tab = s;
        switch (s) {
            case 'info':
                break;
        }
    }

    private convertToDiagramModel(model: WorkflowDiagramNode | WorkflowDiagramLink, type: DiagramObjectType): NodeModel | LinkModel {

        if (type == DiagramObjectType.Link) {
            let m: WorkflowDiagramLink = <WorkflowDiagramLink>model;
            let n = new LinkModel();

            if (m.ConditionObject == null && m.Condition != null && m.Condition.toString() === m.Condition && m.Condition.startsWith('{')) {
                let conditions = JSON.parse(m.Condition).Conditions.Condition;
                n.condition = [];
                conditions.forEach(c => n.condition.push(c));

                n.condition.forEach(c => {
                    this.setConditionLabel(c);
                });

            } else if (m.ConditionObject != null) {
                n.condition = [];

                if (m.ConditionObject.Condition != null && m.ConditionObject.Condition.length != null) {
                    n.condition = m.ConditionObject.Condition;
                } else if (m.ConditionObject.Condition != null) {
                    n.condition.push(m.ConditionObject.Condition);
                }

                n.condition.forEach(c => {
                    this.setConditionLabel(c);
                });

            } else {
                n.condition = [];
            }

            n.settings = (m.SettingsObject == null) ? ((m.Settings != null && m.Settings.toString() === m.Settings && m.Settings.startsWith('{')) ? JSON.parse(m.Settings).settings : {}) : m.SettingsObject;
            n.diagramObjectType = DiagramObjectType.Link;
            n.category = '';
            n.from = m.FromKey;
            n.to = m.ToKey;
            n.key = m.Key;
            n.transitionType = m.TransitionType;
            n.frompid = m.FromPortID;
            n.topid = m.ToPortID;
            n.name = m.Name;

            if (n.transitionType == TransitionType.Condition) {
                n.formInputs = this.getAvailableFormInputs(n);
                n.httpInputs = this.getAvailableHttpInputs(n);
                n.httpResponseInputs = this.getAvailableHttpOutputs(n);
            }

            this.setTransitionIcon(n);

            n.valid = this.validateLink(n);

            return n;

        } else if (type == DiagramObjectType.Node) {
            let m: WorkflowDiagramNode = <WorkflowDiagramNode>model;
            let n = new NodeModel();

            n.key = m.Key;
            n.name = m.Name;
            n.pos = `${m.XPosition} ${m.YPosition}`;
            n.x = m.XPosition;
            n.y = m.YPosition;
            n.activityType = m.ActivityType;
            n.stepType = m.StepType;
            n.category = 'task';
            n.fields = m.FieldsObject;
            n.runCount = m.RunCount || 0;

            //special case for Form to deal with XML returning an object when field count = 1 instead of an array
            if (n.activityType == WorkflowActivityType.Form) {

                if (m.Fields != null && m.Fields.toString() === m.Fields && m.FieldsObject == null && m.Fields.startsWith('{')) {
                    n.fields = JSON.parse(m.Fields).fields;

                }

                if (n.fields != null && n.fields.form != null && n.fields.form.field != null && n.fields.form.field.length == null) {
                    let f = _.cloneDeep(n.fields.form.field);

                    n.fields.form.field = [];
                    n.fields.form.field.push(f);
                }
            }

            let activityType: ActivityTypeInfo;

            if (m.ActivityTypeInfo != null)
                activityType = m.ActivityTypeInfo;
            else
                activityType = this.activityTypes.find(a => a.ID == n.activityType);

            if (activityType != null) {
                n.fore = activityType.ForeColor;
                n.back = activityType.BackColor;
                n.icon = activityType.Icon;
                n.activityName = activityType.Name;
                n.activityDescription = activityType.Description;
            }
            else if (n.activityType == WorkflowActivityType.StateChange)
            {
                n.fore = "#fff";
                n.activityDescription = $localize`State Change (Unsupported Activity)`;
            }
            else
            {
                n.fore = "#fff";
            }

            if (m.SettingsObject != null && m.SettingsObject.settings != null)
                n.settings = m.SettingsObject.settings;
            else if (m.SettingsObject != null && !_.isEmpty(m.SettingsObject) && m.SettingsObject.settings == null)
                n.settings = m.SettingsObject;

            if (n.activityType == WorkflowActivityType.FieldChange) {

                if (n.settings.FieldUpdate == null) n.settings.FieldUpdate = new FieldUpdateSettings();
                if (n.settings.FieldUpdate.Field == null) n.settings.FieldUpdate.Field = [];
                //handle obj vs array due to XML parsing

                if (n.settings.FieldUpdate.Field != null && !_.isEmpty(n.settings.FieldUpdate.Field) && n.settings.FieldUpdate.Field.constructor !== Array) {
                    let f = _.cloneDeep(n.settings.FieldUpdate.Field);
                    n.settings.FieldUpdate.Field = [];
                    n.settings.FieldUpdate.Field.push(f);
                }

                //populate field names
                n.settings.FieldUpdate.Field.forEach(f => {
                    let id = f['@FieldId'];
                    let field = this.fieldTypes.find(t => t.ID.toString() == id);
                    if (field) f['@FieldName'] = field.FriendlyName;
                });
            }

            if (n.activityType == WorkflowActivityType.HTTPRequest) {
                if (n.settings.HTTPRequest == null)
                    n.settings.HTTPRequest = new HTTPRequestSettings();
                if (n.settings.HTTPRequest.Headers != null && n.settings.HTTPRequest.Headers.length == null) {
                    n.settings.HTTPRequest.Headers = [n.settings.HTTPRequest.Headers];
                }
                this.workflowFieldsService.pushHttpRequestField({key: n.key, name: n.name });
            }

            if (n.activityType == WorkflowActivityType.HTTPResponse) {
                if (n.settings.HTTPResponse == null) {
                    n.settings.HTTPResponse = new HTTPResponseSettings();
                }
                if (n.settings.HTTPResponse.Outputs != null) {
                    if (n.settings.HTTPResponse.Outputs.length == null) {
                        n.settings.HTTPResponse.Outputs = [n.settings.HTTPResponse.Outputs as any];
                    }

                    n.settings.HTTPResponse.Outputs.forEach(o => {
                        this.workflowFieldsService.pushOutputField(o);
                    });
                }
            }

            if (n.activityType == WorkflowActivityType.RelationshipUpdate) {
                if (n.settings.RelationshipUpdate == null)
                    n.settings.RelationshipUpdate = new RelationshipUpdateSettings();
                if (n.settings.RelationshipUpdate.Relationship == null)
                    n.settings.RelationshipUpdate.Relationship = {};
            }

            if (m.StepType == StepType.Start)
                n.category = 'start';
            else if (m.StepType == StepType.Finish)
                n.category = 'finish';
            else if (m.StepType == StepType.Terminate)
                n.category = 'finish';

            n.valid = this.validateNode(n);

            return n;

        } else {
            console.error(`type value ${type} is not valid`);
            return null;
        }
    }

    private convertToWorkflowModel(model: NodeModel | LinkModel): WorkflowDiagramNode | WorkflowDiagramLink {
        if (model.diagramObjectType == DiagramObjectType.Link) {
            let m: LinkModel = <LinkModel>model;

            let n = new WorkflowDiagramLink();
            n.Key = m.key;
            n.FromKey = m.from;
            n.ToKey = m.to;
            n.TransitionType = m.transitionType;
            n.Name = m.name;


            //clone conditions so we can remove field name and _$visited
            let cond = _.cloneDeep(m.condition);
            cond.forEach(c => {
                delete c['@FieldName'];
                delete c['_$visited'];
                delete c['@ValueLabel'];
            });

            n.Condition = JSON.stringify({ Conditions: { Condition: cond } });
            n.Settings = JSON.stringify({ settings: m.settings });

            n.FromPortID = m.frompid;
            n.ToPortID = m.topid;

            return n;

        } else if (model.diagramObjectType == DiagramObjectType.Node) {
            let m: NodeModel = <NodeModel>model;
            let n = new WorkflowDiagramNode();
            let settings = _.cloneDeep(m.settings);

            //remove name attributes and prime's _$visited property
            if (m.activityType == WorkflowActivityType.FieldChange) {
                if (settings.FieldUpdate != null && settings.FieldUpdate.Field != null && settings.FieldUpdate.Field.length != null) {
                    let fields = settings.FieldUpdate.Field;

                    fields.forEach(f => {
                        delete f['@FieldName'];
                        delete f['@FormLabel'];
                        delete f['_$visited'];
                    });
                }
            }

            if (m.activityType == WorkflowActivityType.HTTPResponse) {
                if (settings.HTTPResponse != null && settings.HTTPResponse.Outputs != null) {
                    settings.HTTPResponse.Outputs.forEach(o => {
                        delete o['@FormFieldId'];
                        delete o['@FormLabel'];
                    });
                }
            }

            if (m.activityType == WorkflowActivityType.EmailNotification) {
                if (m.settings['MessageRecipientType'] == 'Responsibility') {
                    delete m.settings['ResponsibilityTypeName'];
                }
            }

            if (m.activityType == WorkflowActivityType.Form) {
                if (m.settings['SendFormEmail'].toString() == 'true' && m.settings['MessageRecipientType'] == 'Responsibility') {
                    delete m.settings['ResponsibilityTypeName'];
                }
            }

            //remove primeng _$visited property
            if (m.fields != null && m.fields.form != null && m.fields.form.field != null && m.fields.form.field.length != null) {
                m.fields.form.field.forEach(f => {
                    delete f['_$visited'];
                });
            }

            //clean up empty settings
            if ((settings as any).hasOwnProperty('settings') == true && (settings as any).settings == null) {
                delete (settings as any).settings;
            }

            n.Key = m.key;
            n.ActivityType = m.activityType;
            n.Name = m.name;
            n.SettingsObject = settings;
            n.Settings = JSON.stringify({ settings: settings });
            n.Fields = (m.fields != null && m.fields.form != null) ? JSON.stringify({ fields: m.fields }) : '';

            n.StepType = m.stepType;
            n.XPosition = m.pos.split(' ')[0];
            n.YPosition = m.pos.split(' ')[1];

            return n;
        } else {
            console.error(`model value ${model} is not valid`);
            return null;
        }
    }

    private setConditionLabel(condition: any) {
        let i = this.fieldTypes.findIndex(f => f.ID == condition['@FieldTypeID']);
        if (i >= 0) {
            condition['@FieldName'] = this.fieldTypes[i].FriendlyName + (this.fieldTypes[i].Object == 'IssueType' ? ' (Action Field)' : '');
            return;
        }

        if (condition['@FormInputID'] != null) {
            switch (condition['@FormInputID']) {
                case 'statusCode':
                    condition['@FieldName'] = 'HTTP Request :: Status Code';
                    break;
                case 'responseBody':
                    condition['@FieldName'] = 'HTTP Request :: Response Body';
                    break;
                default:
                    let step = this.model.Nodes.find(n => n.Key == condition['@VersionStepID']);
                    if (step != null) {
                        if (step.SettingsObject != null && step.SettingsObject.settings != null && step.SettingsObject.settings.HTTPResponse != null) {
                            let output = step.SettingsObject.settings.HTTPResponse.Outputs.find(o => o.Id == condition['@FormInputID']);
                            condition['@FieldName'] = 'HTTP Response :: ' + output.Name;
                        }
                    }
                    break;
            }
        }
    }

    private setTransitionIcon(n: LinkModel) {
        switch (+n.transitionType) {
            case TransitionType.Always:
                (<go.GraphLinksModel>this.diagram.model).setDataProperty(n, 'icon', '');
                break;
            case TransitionType.Condition:
                (<go.GraphLinksModel>this.diagram.model).setDataProperty(n, 'icon', '\uf121');
                break;
            case TransitionType.Timer:
                (<go.GraphLinksModel>this.diagram.model).setDataProperty(n, 'icon', '\uf017');
                break;
        }
    }

    private checkHasMultipleInputs() {
        this.diagram.model.nodeDataArray.forEach(n => {
            let count = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).to == (<any>n).key).length;
            (<any>n).hasMultipleInputs = (count > 1);
            (<any>n).valid = this.validateNode(<NodeModel>n);
        });

        this.validateDiagram();
    }

    private validateNode(n: NodeModel): boolean {
        if (this.isReadOnly)
            return true;

        n.errors = [];

        switch (n.activityType) {
            case WorkflowActivityType.EmailNotification:

                if (n.settings == null || _.isEmpty(n.settings))
                    return false;
                if (n.settings.MessageSubjectTemplate == null || n.settings.MessageSubjectTemplate.length < 1)
                    return false;
                if (n.settings.MessageBodyTemplate == null || n.settings.MessageBodyTemplate.length < 1)
                    return false;
                if (this.validateEmailRecipient(n) === false) {
                    return false;
                }

                n.errors = n.errors.concat(this.validateTextFields(n.settings.MessageBodyTemplate));

                if (n.errors.length > 0) return false;

                break;
            case WorkflowActivityType.Form:

                if (n.settings == null || _.isEmpty(n.settings))
                    return false;
                if (n.settings.FormResponseType == null || n.settings.FormResponseType == '')
                    return false;
                if (this.validateEmailRecipient(n) === false) {
                    return false;
                }

                if (n.settings.SendFormEmail != null && n.settings.SendFormEmail.toString().toLowerCase() == 'true') {
                    if (n.settings.MessageBodyTemplate == null || n.settings.MessageBodyTemplate.length < 1)
                        return false;

                    n.errors = n.errors.concat(this.validateTextFields(n.settings.MessageBodyTemplate));
                }

                if (n.fields == null || _.isEmpty(n.fields))
                    return false;

                if (n.fields && n.fields.form && n.fields.form["@description"]) {
                    n.errors = n.errors.concat(this.validateTextFields(n.fields.form["@description"]));
                }

                if (n.fields.form == null)
                    return false;
                if (n.fields.form['@title'] == null || n.fields.form['@title'].length < 1)
                    return false;

                
                if (n.errors.length > 0) {
                    return false;
                }
                break;
            case WorkflowActivityType.Procedure:
                if (n.settings.ProcedureID == null || n.settings.ProcedureID == '')
                    return false;
                break;
            case WorkflowActivityType.StatusChange:
                if (n.settings.Status == null || n.settings.Status == '')
                    return false;
                break;
            case WorkflowActivityType.FieldChange:
                if (n.settings == null || n.settings.FieldUpdate == null || n.settings.FieldUpdate.Field == null || _.isEmpty(n.settings.FieldUpdate.Field))
                    return false;

                if (n.settings.FieldUpdate.Field.length == null || n.settings.FieldUpdate.Field.length < 1)
                    return false;

                let fields = n.settings.FieldUpdate.Field;
                let hasInvalidField = false;
                n.errors = [];

                fields.forEach(f => {
                    let refField = this.fieldTypes.find(x => x.ID == +f["@FieldId"]);
                    if (!refField) {
                        hasInvalidField = true;
                        n.errors.push('Invalid field type');
                    }
                    if (f["@IsActionForm"] && f["@IsActionForm"] == 'true' && f["@FormFieldId"]) {
                        var fieldData = f["@FormFieldId"].split('|');
                        if (fieldData[0] == 'IssueType') {
                            refField = this.fieldTypes.find(x => x.Object == 'IssueType' && x.ID == +fieldData[1]);
                        }
                        else {
                            refField = this.fieldTypes.find(x => x.Object != 'IssueType' && x.ID == +fieldData[1]);
                        }

                        if (!refField) {
                            hasInvalidField = true;
                            n.errors.push('Invalid field type');
                        }
                    }
                });
                if (hasInvalidField) return false;
                break;
            case WorkflowActivityType.RelationshipUpdate:
                if (n.settings == null || n.settings.RelationshipUpdate == null || n.settings.RelationshipUpdate.Relationship == null || _.isEmpty(n.settings.RelationshipUpdate.Relationship))
                    return false;
                if (n.settings.RelationshipUpdate.Relationship['@ClearValue'] == null || n.settings.RelationshipUpdate.Relationship['@ClearValue'].toString().toLowerCase() == "false") {
                    if (n.settings.RelationshipUpdate.Relationship['@FormStepId'] == null || n.settings.RelationshipUpdate.Relationship['@FormFieldId'] == null)
                        return false;
                }
                break;
            case WorkflowActivityType.StateChange:
                if (n.settings.State == null || n.settings.State == '')
                    return false;
                break;
            case WorkflowActivityType.HTTPRequest:
                if (n.settings.HTTPRequest == null)
                    return false;
                if (n.settings.HTTPRequest.Url == null)
                    return false;
                else {
                    if (n.settings.HTTPRequest.Url.length < 7)
                        return false;
                    if (n.settings.HTTPRequest.Url.indexOf('http://') != 0
                        && n.settings.HTTPRequest.Url.indexOf('https://') != 0) {
                        return false;
                    }
                }

                if (n.settings.HTTPRequest.Timeout == null)
                    return false;
                else {
                    if (isNaN(n.settings.HTTPRequest.Timeout))
                        return false;
                    if (+n.settings.HTTPRequest.Timeout < 1 || +n.settings.HTTPRequest.Timeout > 600)
                        return false;
                }

                if (n.settings.HTTPRequest.Method == null || n.settings.HTTPRequest.Method == '')
                    return false;
                break;
            case WorkflowActivityType.HTTPResponse:               
                if (n.settings.HTTPResponse == null) {
                    return false;
                }
                if (n.settings.HTTPResponse.InputStepId == null || n.settings.HTTPResponse.InputStepId === '') {
                    return false;
                }
                if (n.settings.HTTPResponse.Outputs == null || n.settings.HTTPResponse.Outputs.length < 1) {
                    return false;
                }
                break;
        }

        return true;
    }

    private validateEmailRecipient(n: NodeModel): boolean {
        if (n.settings.MessageRecipientType == null || n.settings.MessageRecipientType == '')
            return false;
        switch (n.settings.MessageRecipientType) {
            case 'SpecificUser':
                if (n.settings.MessageToUser == null || n.settings.MessageToUser.length < 1)
                    return false;
                break;
            case 'Responsibility':
                if (this.model.Event.Object == 'IntersectType' && n.settings.ResponsibilitySide == null || n.settings.ResponsibilitySide == '')
                    return false;
                if (n.settings.ResponsibilityTypeID == null)
                    return false;
                if (!_.isArray(n.settings.ResponsibilityTypeID) && n.settings.ResponsibilityTypeID < 0) //we still need to check single value here for legacy workflows
                    return false;
                if (_.isArray(n.settings.ResponsibilityTypeID)) {
                    if (n.settings.ResponsibilityTypeID.length < 1)
                        return false;

                    let x = n.settings.ResponsibilityTypeID.findIndex(r => r == null || r == '' || r < 0);
                    if (x > -1)
                        return false;
                }
                break;
            case "Followers":
                let obj = this.model.Event.Object;
                if (obj == 'IntersectType')
                    return false;

                if (!(this.model.Event.ChangeType == WorkflowChangeType.Add ||
                    this.model.Event.ChangeType == WorkflowChangeType.Update ||
                    this.model.Event.ChangeType == WorkflowChangeType.Schedule ||
                    this.model.Event.ChangeType == WorkflowChangeType.RequestCertification))
                    return false;

                if ((this.model.Event.ChangeType == WorkflowChangeType.Add) && !(obj == 'IssueType'))
                    return false

                if ((this.model.Event.ChangeType == WorkflowChangeType.Add) && (obj == 'IssueType')) {
                    if (this.model.Event.IssueObject != null && this.model.Event.IssueObject != '') {
                        let objArr = this.model.Event.IssueObject.split("|", 1);
                        let Issobj = "";
                        if (objArr.length <= 0)
                            Issobj = " ";
                        else
                            Issobj = objArr[0];

                        if (!(Issobj == 'ArtifactType' || Issobj == 'PolicyType' || Issobj == 'RuleType' || Issobj == 'TaxonomyType'))
                            return;
                    }
                }
                break;
            case "Group":
                if (n.settings.MessageToGroup == null || n.settings.MessageToGroup.length != 36)
                    return false;
                break;
        }

        return true;
    }

    private validateTextFields(desc: string): string[] {
        let errors: string[] = [];

        if (!desc) return errors;

        var results = desc.match(/(\[)(.*?)(?=\])/g);
        if (results && results.length) {
            results.forEach(x => {
                var fieldData = x.split('::');

                if (fieldData.length == 2) {
                    var fieldType = fieldData[0].replace('[', '').trim();
                    var fieldName = fieldData[1].trim();
                    let f: any = null;
                    if (fieldType == 'Action Field') {
                        f = this.fieldTypes.find(x => x.Object == 'IssueType' && x.Name == fieldName);
                    }
                    else if (fieldType == 'Asset Field') {
                        f = this.fieldTypes.find(x => x.Object != 'IssueType' && x.Name == fieldName);
                    } else if (fieldType == 'HTTPREQUEST') {
                        f = this.workflowFieldsService.getHttpFields().find(x => x['@stepId'] == fieldData[1].trim());
                    }
                    if (!f) {
                        errors.push('Invalid field type');
                    }
                }
            });
        }
        return errors;
    }

    private validateLink(l: LinkModel): boolean {
        if (this.isReadOnly)
            return true;

        switch (+l.transitionType) {
            case TransitionType.Condition:
                if (l.condition == null || l.condition.length < 1)
                    return false;
                break;
            case TransitionType.Timer:
                if (l.settings == null || l.settings.TimerInterval == null || l.settings.TimerInterval < 1)
                    return false;
                break;
        }
        return true;
    }

    private validateDiagram(): boolean {
        this.isValid = true;
        this.errors = [];

        let model = <go.GraphLinksModel>this.diagram.model;
        let invalidNodeCount = 0;
        let invalidLinkCount = 0;
        let disconnectedNodeCount = 0;
        let startNodes = 0;
        let finishNodes = 0;
        let missingInputCount = 0;
        let StateChangeCount = 0;
        let SqlProcedureCount = 0;
        let missingOutputCount = 0;
        let invalidFieldReferences = 0;

        let startKey = "";
        let finishKey = "";
        let startToFinish = false;

        model.nodeDataArray.forEach(n => {
            let node = <NodeModel>n;

            if (node.valid == false) {
                invalidNodeCount++;
            }
            if (+node.stepType == StepType.Start) {
                startNodes++;
                startKey = node.key;
            }
            if (+node.stepType == StepType.Finish) {
                finishNodes++;
                finishKey = node.key;
            }

            let from = model.linkDataArray.find(l => (<any>l).from == node.key);
            let to = model.linkDataArray.find(l => (<any>l).to == node.key);

            //special case, steps from timer transitions don't require an output
            if (to != null && (+node.stepType == StepType.Task && (<LinkModel>to).transitionType == TransitionType.Timer))
                return;

            if (to != null && (+node.stepType == StepType.Task && +node.activityType == WorkflowActivityType.StateChange)) {
                StateChangeCount++;
            }

            if (this.HideSqlProcedure && to != null && (+node.stepType == StepType.Task && +node.activityType == WorkflowActivityType.Procedure))
            {
                SqlProcedureCount++;
            }

            

            if (to == null && +node.stepType != StepType.Start)
                missingInputCount++;
            if (from == null && +node.stepType != StepType.Finish && +node.stepType != StepType.Terminate)
                missingOutputCount++;
            if (to == null && from == null)
                disconnectedNodeCount++;

            if (node.errors) {
                node.errors.forEach(x => { if (x == 'Invalid field type') invalidFieldReferences++ });
            }
            
        });

        model.linkDataArray.forEach(l => {
            let link = <LinkModel>l;

            if (link.valid == false)
                invalidLinkCount++;
            if (startNodes == 1 && finishNodes == 1 && link.from == startKey && link.to == finishKey)
                startToFinish = true;
        });


        if (invalidNodeCount > 0)
            this.errors.push($localize`There are one or more invalid steps on the diagram (highlighted in red)`);

        if (invalidLinkCount > 0)
            this.errors.push($localize`There are one or more invalid transitions on the digram (highlighted in red)`);

        if (startNodes != 1)
            this.errors.push($localize`There must be exactly 1 start step on the diagram`);

        if (finishNodes != 1)
            this.errors.push($localize`There must be exactly 1 finish step on the diagram`);

        if (disconnectedNodeCount > 0)
            this.errors.push($localize`There are steps on the diagram which are not connected`);

        if (missingInputCount > 0 || missingOutputCount > 0)
            this.errors.push($localize`There are steps on the diagram which are missing an input or output`);

        if (startToFinish)
            this.errors.push($localize`The start step cannot be connected directly to the finish step`);

        if (invalidFieldReferences > 0)
            this.errors.push($localize`There are ${invalidFieldReferences} invalid field references in workflow`);

        if (StateChangeCount > 0)
            this.errors.push($localize`Unsupported workflow activity "State Change" exists in diagram. This workflow activity must be removed.`);

        if (SqlProcedureCount > 0)
            this.errors.push($localize`Wrong workflow activity "Sql Procedure" exists in diagram. This workflow activity must be removed. Procedure configuration missing.`);
        
        if (this.errors.length > 0)
            this.isValid = false;

        return this.isValid;


    }

    private getNodeDisplayName(data): string {
        return data.name ? data.name.substring(0, 36) + (data.name.length > 36 ? '...' : '') : (data.activityDescription || data.activityName);
    }

    //#endregion

    //#region events

    private backClick() {
        this.model.Nodes = [];
        this.model.Links = [];
        this.model.Event.ConditionObject = null;

        this.diagram.model.nodeDataArray.forEach(n => {
            this.model.Nodes.push(<WorkflowDiagramNode>this.convertToWorkflowModel(<NodeModel>n));
        });

        (<go.GraphLinksModel>this.diagram.model).linkDataArray.forEach(l => {
            this.model.Links.push(<WorkflowDiagramLink>this.convertToWorkflowModel(<LinkModel>l));
        });

        this.onBackClick.emit(this.model);
    }

    private changeStep(e: NodeModel) {
        this.diagram.startTransaction('changeStep');

        let n = this.diagram.model.findNodeDataForKey(e.key) as NodeModel;

        //TODO: just set n = e??

        switch (n.activityType) {
            case WorkflowActivityType.EmailNotification: //email
                n.settings.MessageSubjectTemplate = e.settings.MessageSubjectTemplate;
                n.settings.MessageBodyTemplate = e.settings.MessageBodyTemplate;
                n.settings.MessageRecipientType = e.settings.MessageRecipientType;
                n.settings.MessageToUser = e.settings.MessageToUser;
                n.settings.MessageToGroup = e.settings.MessageToGroup;
                n.settings.IncludePreviousFormResponses = e.settings.IncludePreviousFormResponses;
                n.settings.SendToDefaultUsers = e.settings.SendToDefaultUsers;
                n.settings.ResponsibilityTypeID = e.settings.ResponsibilityTypeID;

                if (e.settings.MessageRecipientType == 'Responsibility') {
                    if (this.model.Event.Object == 'IntersectType')
                        n.settings.ResponsibilitySide = e.settings.ResponsibilitySide;
                    else
                        delete e.settings.ResponsibilitySide;
                    delete e.settings.MessageToUser;
                } else {
                    delete e.settings.ResponsibilityTypeID;
                    delete e.settings.ResponsibilitySide;
                }

                break;
            case WorkflowActivityType.StatusChange: //status change
                n.settings.Status = e.settings.Status;
                break;
            case WorkflowActivityType.Form: //form
                n.fields = e.fields;
                n.settings.FormResponseType = e.settings.FormResponseType
                n.settings.SendFormEmail = e.settings.SendFormEmail;
                n.settings.MessageRecipientType = e.settings.MessageRecipientType;
                n.settings.MessageToUser = e.settings.MessageToUser;
                n.settings.MessageToGroup = e.settings.MessageToGroup;
                n.settings.ResponsibilityTypeID = e.settings.ResponsibilityTypeID;
                n.settings.IncludePreviousFormResponses = e.settings.IncludePreviousFormResponses;
                if (n.settings.SendFormEmail == true) {
                    n.settings.MessageBodyTemplate = e.settings.MessageBodyTemplate;
                    n.settings.MessageSubjectTemplate = e.settings.MessageSubjectTemplate;
                } else {
                    delete e.settings.MessageBodyTemplate;
                    delete e.settings.MessageSubjectTemplate;
                }
                if (this.model.Event.Object == 'IntersectType') {
                    n.settings.ResponsibilitySide = e.settings.ResponsibilitySide;
                } else {
                    delete n.settings.ResponsibilitySide;
                }
                break;
            case WorkflowActivityType.Procedure:
                n.settings.ProcedureID = e.settings.ProcedureID;
                break;
            case WorkflowActivityType.FieldChange:
                n.settings.FieldUpdate = e.settings.FieldUpdate;
                break;
            case WorkflowActivityType.RelationshipUpdate:
                n.settings.RelationshipUpdate = e.settings.RelationshipUpdate;
                break;
            case WorkflowActivityType.StateChange: //status change
                n.settings.State = e.settings.State;
                break;
            case WorkflowActivityType.HTTPRequest:
                n.settings.HTTPRequest = e.settings.HTTPRequest;
                this.workflowFieldsService.pushHttpRequestField({ key: n.key, name: n.name });
                break;
            case WorkflowActivityType.HTTPResponse:
                n.settings.HTTPResponse = e.settings.HTTPResponse;  
                break;
        }

        if (e.hasMultipleInputs && e.settings.WaitForAllTransitions != null) {
            n.settings.WaitForAllTransitions = e.settings.WaitForAllTransitions;
        }

        if (!e.hasMultipleInputs && n.settings.WaitForAllTransitions != null)
            delete n.settings.WaitForAllTransitions;

        this.diagram.model.setDataProperty(n as go.ObjectData, 'name', e.name);
        this.diagram.model.setDataProperty(n as go.ObjectData, 'valid', this.validateNode(n));
        this.validateDiagram();

        this.diagram.commitTransaction('changeStep');

    }

    private changeTransition(e: LinkModel) {
        this.diagram.startTransaction('changeTransition');

        let i = (<go.GraphLinksModel>this.diagram.model).linkDataArray.findIndex(l => (<any>l).from == e.from && (<any>l).to == e.to);
        let l = null;
        if (i >= 0)
            l = (<go.GraphLinksModel>this.diagram.model).linkDataArray[i];
        if (l != null) {

            l.transitionType = e.transitionType;

            this.setTransitionIcon(l);
            l.name = e.name;
            l.condition = e.condition;
            l.settings = e.settings;
            l.icon = e.icon;
            l.formInputs = this.getAvailableFormInputs(e);
            l.httpInputs = this.getAvailableHttpInputs(e);
            l.httpResponseInputs = this.getAvailableHttpOutputs(e);
            if (this.selectedData != null && this.selectedData.diagramObjectType == DiagramObjectType.Link) {
                this.selectedData.formInputs = l.formInputs;
                this.selectedData.httpInputs = l.httpInputs;
                this.selectedData.httpResponseInputs = l.httpResponseInputs;
            }

            this.setTransitionIcon(l);
            (<go.GraphLinksModel>this.diagram.model).setDataProperty(l, 'valid', this.validateLink(l));
            this.validateDiagram();
        }

        this.diagram.commitTransaction('changeTransition');
    }

    menuClick(e: any) {
        if (e.icon == 'fa fa-info-circle')
            this.isWindowVisible = !this.isWindowVisible;
        if (e.icon == 'fa fa-remove')
            this.onCloseClick.emit();
        if (e.icon == 'fa fa-floppy-o')
            this.save();
        if (e.icon == 'fa fa-arrow-left')
            this.backClick();
    }

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    private resizeDiagram() {
        let dOffset = (this.hasHeader ? this.diagramOffset : this.diagramOffset - 125);
        let oOffset = (this.hasHeader ? this.overlayOffset : this.overlayOffset - 125);
        this.diagramRef.nativeElement.style.height = (window.innerHeight - dOffset) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - dOffset) + 'px';
        this.overlayMaxHeight = window.innerHeight - oOffset;
    }

    private reOrderLayout() {
        this.diagram.layout.invalidateLayout();
        this.diagram.requestUpdate();
    }

    private ChangedSelection(e: any) {
        let sel = e.diagram.selection;
        if (sel.count == 0) {
            this.selectedData = null;
            this.showNodeTabs = false;
            this.showLinkTabs = false;
            this.selectedStepId = null;
            this.selectedStepIdChange.emit(null);
        } else {
            sel = _.cloneDeep(sel.toArray());

            if (sel != null && sel.length != 0) {
                this.selectedData = sel[0].data;

                if (this.selectedData.diagramObjectType == DiagramObjectType.Node) {
                    this.showNodeTabs = true;
                    this.showLinkTabs = false;
                    this.selectedStepId = this.selectedData.key;
                    this.selectedStepIdChange.emit(this.selectedData.key);
                } else if (this.selectedData.diagramObjectType == DiagramObjectType.Link) {
                    this.showNodeTabs = false;
                    this.showLinkTabs = true;
                }
            }
        }
        this.setOverlayHeaderName(this.selectedData);
        this.selection = this.selectedData;
        this.selectionChange.emit(this.selection);
    }

    private LinkDrawn(e: any) {
        let link = e.subject;
        let l = (<go.GraphLinksModel>this.diagram.model).linkDataArray.findIndex(l => (<any>l).from == link.from && (<any>l).to == link.to);
        this.checkHasMultipleInputs();

        if (l > -1) {
            let k = (<LinkModel>(<go.GraphLinksModel>this.diagram.model).linkDataArray[l]);
            k.formInputs = this.getAvailableFormInputs(k);
            k.httpInputs = this.getAvailableHttpInputs(k);
            k.httpResponseInputs = this.getAvailableHttpOutputs(k);
        }
    }

    private deleteSelection() {

        if (this.isReadOnly)
            return;

        let links: LinkModel[] = [];
        let nodes: NodeModel[] = [];
        let coll: go.Part[] = [];

        this.diagram.selection.each(x => {
            if (x.data.diagramObjectType == DiagramObjectType.Node) {
                nodes.push(x.data);
            } else if (x.data.diagramObjectType == DiagramObjectType.Link) {
                links.push(x.data);
            }
        });

        //get links attached to node if they weren't selected. They will be deleted automagically
        nodes.forEach(n => {
            let to = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).to == n.key);
            let from = (<go.GraphLinksModel>this.diagram.model).linkDataArray.filter(l => (<any>l).from == n.key);

            to.forEach(t => {
                if (links.findIndex(l => l.key == (<any>t).key) < 0)
                    links.push(<LinkModel>t);
            });

            from.forEach(f => {
                if (links.findIndex(l => l.key == (<any>f).key) < 0)
                    links.push(<LinkModel>f);
            });
        });


        links.forEach(l => {
            //remove used fields from global list
            let fields = this.workflowFieldsService.getUsedFields().filter(u => u.transitionId == l.key);
            fields.forEach(f => this.workflowFieldsService.deleteUsedField(f.fieldId, f.stepId, f.transitionId));
            coll.push(this.diagram.findPartForData(l));
        });

        nodes.forEach(n => {
            if (n.activityType == WorkflowActivityType.HTTPRequest) {
                this.workflowFieldsService.deleteHttpRequestField(n.key);
            }
            if (n.activityType == WorkflowActivityType.Form) {
                let canDelete = true;
                if (n.fields.form != null && n.fields.form.field != null) {
                    n.fields.form.field.forEach(f => {
                        if (this.workflowFieldsService.getUsedFields().findIndex(u => u.stepId == n.key) > -1) {
                            canDelete = false;

                            //need to remove pending delete on link
                            let parts = coll.filter(c => c.data.diagramObjectType == DiagramObjectType.Link && c.data.from == n.key);
                            parts.forEach(p => {
                                let i = coll.findIndex(c => c.data.diagramObjectType == DiagramObjectType.Link && c.data.from == p.data.from);
                                let l = this.diagram.findPartForData(p);
                                if (i > -1) {
                                    coll = coll.splice(i, 1);
                                }
                                if (l != null && l.data.condition != null)
                                    l.data.condition.forEach(c => {
                                        this.workflowFieldsService.pushUsedField(c['@FormInputID'], c['@VersionStepID'], l.data.key, l.data.name);
                                    });
                            });
                        }
                    });
                    if (canDelete) {
                        //remove form fields from global list
                        n.fields.form.field.forEach(f => this.workflowFieldsService.deleteFormField(f));
                        coll.push(this.diagram.findPartForData(n));
                    }
                } else {
                    coll.push(this.diagram.findPartForData(n));
                }
            } else {
                coll.push(this.diagram.findPartForData(n));
            }
        });

        this.diagram.removeParts(coll, false);
        this.diagram.clearSelection();
        this.selectedStepId = null;
        this.selectedStepIdChange.emit(null);
        this.selectedData = null;
        this.validateDiagram();
    }

    private ExternalObjectsDropped(e: any) {
        this.diagram.model.nodeDataArray.forEach(n => {

            //gojs doesn't like giving each node its own settings/fields object for some reason
            //set it here if it's empty
            if ((<any>n).settings == null || _.isEmpty((<any>n).settings))
                (<any>n).settings = Object.create({});

            if ((<any>n).fields == null || _.isEmpty((<any>n).fields))
                (<any>n).fields = Object.create({});

            this.diagram.model.setDataProperty(n, 'valid', this.validateNode(<NodeModel>n));
        });

        this.validateDiagram();
    }

    private ClipboardPasted(e) {
        if (e != null && e.subject != null) {
            let nodes = e.subject.toArray();
            
            for (let i = 0; i < nodes.length; i++) {

                if (nodes[i].data.DiagramObjectType == DiagramObjectType.Link) {
                    continue;
                }

                //clone settings
                this.diagram.model.setDataProperty(nodes[i].data, "settings", _.cloneDeep(nodes[i].data.settings));

                //move the copy slightly so it's not directly on top of the original
                nodes[i].location = new go.Point(nodes[i].location.x - (Math.random() * 30), nodes[i].location.y - (Math.random() * 30));
            }
            this.ExternalObjectsDropped(null);
        }
    }

    //#endregion

    //#region templates

    private createPalette(): go.Palette {
        let paletteModel = [];

        //load the palette with the appropriate nodes
        let start = new NodeModel();
        start.category = 'start';
        start.name = 'Start';
        start.diagramObjectType = DiagramObjectType.Node;
        start.stepType = StepType.Start;
        start.activityType = 0;
        start.pos = "0 0";
        start.valid = true;
        start.runCount = 0;

        paletteModel.push(start);

        let finish = new NodeModel();
        finish.category = 'finish';
        finish.name = 'Finish';
        finish.diagramObjectType = DiagramObjectType.Node;
        finish.stepType = StepType.Finish;
        finish.activityType = 0;
        finish.pos = "0 0";
        finish.valid = true;
        finish.runCount = 0;

        paletteModel.push(finish);

        let terminate = new NodeModel();
        terminate.category = 'finish';
        terminate.name = 'Terminate';
        terminate.diagramObjectType = DiagramObjectType.Node;
        terminate.stepType = StepType.Terminate;
        terminate.activityType = 0;
        terminate.pos = "0 0";
        terminate.valid = true;
        terminate.runCount = 0;

        paletteModel.push(terminate);

        this.activityTypes.forEach(a => {

            let m = new NodeModel();

            m.name = a.Description;
            m.category = 'task';
            m.fore = a.ForeColor;
            m.back = a.BackColor;
            m.activityName = a.Name;
            m.icon = a.Icon;
            m.activityDescription = a.Description;
            m.stepType = StepType.Task;
            m.pos = "0 0";
            m.diagramObjectType = DiagramObjectType.Node;
            m.activityType = a.ID;
            m.runCount = 0;
            m.settings = new NodeSettings();
            m.fields = new NodeFields();;
            m.valid = true;

            paletteModel.push(m);

        });

        let pt = this.g(go.Palette, "WorkflowPalette",
            {
                "animationManager.duration": 800,
                nodeTemplateMap: this.diagram.nodeTemplateMap,
                model: new go.GraphLinksModel(paletteModel),
                layout: this.g(go.GridLayout, {
                    sorting: go.GridLayout.Forward,
                    comparer: (a, b) => {
                        if (a.activityType < b.activityType) return -1;
                        if (a.activityType > b.activityType) return 1;
                        return 0;
                    }
                })
            });

        return pt;
    }

    private createDiagram(): go.Diagram {

        let dg = this.g(go.Diagram, 'WorkflowDiagram', {
            initialContentAlignment: go.Spot.TopLeft,
            allowDrop: true,
            "undoManager.isEnabled": !this.isReadOnly
        });

        let model = (dg.model as go.GraphLinksModel);

        model.nodeCategoryProperty = "category";
        model.linkFromPortIdProperty = "frompid";
        model.linkToPortIdProperty = "topid";
        model.nodeDataArray = [];
        model.linkDataArray = [];
        dg.toolManager.hoverDelay = 250;

        return dg;
    }

    private createTaskNode(): go.Node {
        let nodeWidth = 150;
        let nodeHeight = 75;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 10;

        return this.g(go.Node, "Spot",
            new go.Binding("location", "pos", s => go.Point.parse(s)).makeTwoWay(go.Point.stringify),
            {
                locationSpot: go.Spot.Center,
                mouseEnter: (e, obj) => {
                    this.showPorts(obj.part, true);
                },
                mouseLeave: (e, obj) => {
                    this.showPorts(obj.part, false);
                }
            },
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight
            },
                this.g(go.Shape, "RoundedRectangle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 3,
                    spot1: go.Spot.TopLeft,
                    spot2: go.Spot.BottomRight,
                    name: "NodeShape",
                },
                    new go.Binding("fill", "back").makeTwoWay(),
                    new go.Binding("stroke", "valid", v => {
                        return (v || this.isReadOnly) ? nodeBorderColor : '#f00'
                    })
                ),
                this.g(go.Panel, go.Panel.Horizontal, {
                    alignment: go.Spot.BottomLeft,
                    margin: 5
                },
                    this.makeIconPanel(nodeFontSize)
                ),
                this.g(go.Panel, go.Panel.Horizontal, {
                    alignment: go.Spot.BottomRight,
                    margin: 5
                },
                    this.makeCountPanel(nodeFontSize)
                ),
                this.g(go.Panel, "Table",
                    this.g(go.TextBlock, {
                        row: 0,
                        margin: 3,
                        alignment: go.Spot.Top,
                        editable: false,
                        maxSize: new go.Size(nodeWidth - 20, nodeHeight - 10),
                        font: "bold " + nodeFontSize + "pt sans-serif",
                    },
                        new go.Binding("text", "", d => this.getNodeDisplayName(d)),
                        new go.Binding("stroke", "fore").makeTwoWay()
                    )
                ),
                this.makePort('B', go.Spot.Bottom, true, false),
                this.makePort('T', go.Spot.Top, false, true),
                this.makePort('L', go.Spot.Left, false, true),
                this.makePort('R', go.Spot.Right, true, false)
            )
        );
    }


    private createTerminalNode(isStart: boolean): go.Node {
        let nodeWidth = 80;
        let nodeHeight = 80;
        let nodeBorderColor = 'transparent';
        let nodeFontSize = 8;
        let backColor = isStart ? '#216b23' : '#6b2121';

        return this.g(go.Node, "Auto",
            new go.Binding("location", "pos", s => go.Point.parse(s)).makeTwoWay(v => go.Point.stringify(v)),
            {
                locationSpot: go.Spot.Center,
                mouseEnter: (e, obj) => {
                    this.showPorts(obj.part, true);
                },
                mouseLeave: (e, obj) => {
                    this.showPorts(obj.part, false);
                }
            },
            this.g(go.Panel, "Auto", {
                width: nodeWidth,
                height: nodeHeight,
                margin: 0,
                alignment: go.Spot.Center
            },
                this.g(go.Shape, "Circle", {
                    stroke: nodeBorderColor,
                    strokeWidth: 2,
                    width: 78,
                    height: 78,
                    name: "NodeShape",
                    fill: backColor
                }
                ),
                this.g(go.Panel, go.Panel.Horizontal, {
                    alignment: go.Spot.BottomRight,
                    margin: 0
                },
                    this.makeTerminalCountPanel(nodeFontSize, isStart)
                ),
                this.g(go.TextBlock, {
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    font: "bold " + nodeFontSize + "pt sans-serif",
                    stroke: "#fff",
                },
                    new go.Binding("text", "name").makeTwoWay()
                )
            ),
            this.makePort((isStart) ? 'B' : 'T', (isStart) ? go.Spot.Bottom : go.Spot.Top, isStart, !isStart),
            this.makePort((isStart) ? 'R' : 'L', (isStart) ? go.Spot.Right : go.Spot.Left, isStart, !isStart)
        );
    }

    private createDefaultLink(): go.Link {
        return this.g(
            go.Link, {
                routing: go.Link.AvoidsNodes,
                corner: 10,
                relinkableFrom: false,
                relinkableTo: false
            }, // the whole link panel
            new go.Binding("curve", "curve", go.Binding.parseEnum(go.Link, go.Link.JumpOver)),
            this.g(go.Shape, {
                stroke: "gray", strokeWidth: 2
            },
                new go.Binding("stroke", "valid", v => {
                    return (v || this.isReadOnly) ? "gray" : "red"
                })),
            this.g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" },
                new go.Binding("fill", "valid", v => {
                    return (v || this.isReadOnly) ? "gray" : "red"
                }),
                new go.Binding("stroke", "valid", v => {
                    return (v || this.isReadOnly) ? "gray" : "red"
                })
            ), // the arrowhead
            this.g(go.Panel, "Auto",
                this.g(go.Shape, "Circle", {
                    visible: false,
                    fill: 'gray',//this.g(go.Brush, "Radial", { 0: "rgb(255, 255, 255)", 0.3: "rgb(255, 255, 255)", 1: "rgba(255, 255, 255, 0)" }),
                    stroke: 'gray'
                    //,width: 25,
                    //height: 25
                    //strokeDashArray: [2, 2]
                },
                    //only visible if there's a label
                    new go.Binding("stroke", "valid", v => {
                        return (v || this.isReadOnly) ? "gray" : "red"
                    }),
                    new go.Binding("fill", "valid", v => {
                        return (v || this.isReadOnly) ? "gray" : "red"
                    }),
                    new go.Binding("visible", "icon", function (a) {
                        return (a ? true : false)
                    })
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt FontAwesome", stroke: "#fff", margin: 0.75
                },
                    // the label
                    new go.Binding("text", "icon").makeTwoWay()
                )
            )
        );
    }

    private makeIconPanel(fontSize) {
        fontSize -= 2;
        let iconPanel = this.g(go.Panel,
            "Auto",
            {
                alignment: go.Spot.Center,
                margin: 2
            },
            this.g(go.TextBlock,
                {
                    row: 0,
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    font: (fontSize) + "pt FontAwesome",
                },
                new go.Binding("text", "icon").makeTwoWay(),
                new go.Binding("stroke", "fore").makeTwoWay()
            )
        );

        return iconPanel;
    }

    private makeCountPanel(fontSize) {
        fontSize -= 1;
        let countPanel = this.g(go.Panel,
            "Auto",
            {
                alignment: go.Spot.Center,
                margin: 1
            },
            this.g(go.Shape, "RoundedRectangle",
                {
                    stroke: null,
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, { fill: "lightyellow" }), this.g(go.Panel, "Vertical", this.g(go.TextBlock, {
                        margin: 3,
                        text: 'Item count'
                    })))
                },
                new go.Binding("fill", "fore")
            ),
            this.g(go.TextBlock,
                {
                    row: 0,
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    font: (fontSize) + "pt sans-serif",
                },
                new go.Binding("text", "runCount").makeTwoWay(),
                new go.Binding("stroke", "back").makeTwoWay()
            )
            , new go.Binding("visible", "runCount", (k) => {
                return this.isReadOnly && k > 0;
            })
        );

        return countPanel;
    }

    private makeTerminalCountPanel(fontSize, isStart: boolean) {
        //fontSize -= 1;
        let backColor = isStart ? '#216b23' : '#6b2121';
        let foreColor = '#fff';
        let countPanel = this.g(go.Panel,
            "Auto",
            {
                alignment: go.Spot.Center,
                margin: 1
            },
            this.g(go.Shape, "RoundedRectangle",
                {
                    stroke: null,
                    fill: foreColor,
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, { fill: "lightyellow" }), this.g(go.Panel, "Vertical", this.g(go.TextBlock, {
                        margin: 3,
                        text: 'Item count'
                    })))
                }
            ),
            this.g(go.TextBlock,
                {
                    row: 0,
                    margin: 0,
                    alignment: go.Spot.Center,
                    editable: false,
                    stroke: backColor,
                    font: (fontSize) + "pt sans-serif",
                },
                new go.Binding("text", "runCount").makeTwoWay(),
            )
            , new go.Binding("visible", "runCount", (k) => {
                return this.isReadOnly && k > 0;
            })
        );

        return countPanel;
    }

    private makePort(name, spot, output, input) {
        return this.g(go.Shape, "Circle",
            {
                fill: "transparent",
                stroke: null,
                desiredSize: new go.Size(8, 8),
                alignment: spot, alignmentFocus: spot,
                portId: name,
                fromSpot: spot, toSpot: spot,
                fromLinkable: output, toLinkable: input,
                cursor: "pointer"
            });
    }

    private showPorts(node, show) {
        let diagram = node.diagram;
        if (!diagram || diagram.isReadOnly || !diagram.allowLink) return;
        node.ports.each((port) => {
            port.stroke = (show ? "white" : null);
        });
    }

    //#endregion

    private clearExecuted() {
        this.workflowService.clearLastExecutionDate(this.id, this.uid).subscribe(r => {
            //Only clear if we get a positive response
            if (r != undefined)
                this.model.Event.LastExecuted = null;
        });
    }

}

