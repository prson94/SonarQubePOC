import {
    Component,
    Input,
    OnInit,
    AfterViewInit,
    AfterViewChecked,
    ElementRef,
    OnDestroy,
    ViewChild,
    Renderer,
    HostListener,
    Output,
    EventEmitter,
    OnChanges,
    SimpleChanges
} from '@angular/core';
import { PermissionsService } from '../../../services/permissions.service';
import { BaseComponent } from '../base.component';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../services/workflow-fields.service';
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
    WorkflowEventRegistration,
    WorkflowListItem,
    WorkflowChangeType,
    FormResponseType,
    WorkflowActivityType,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';

import { MenuItem } from 'primeng/primeng';

import * as go from 'gojs';
import * as _ from 'lodash';

declare var window: any;

@Component({
    selector: 'd3s-workflow-diagram',
    templateUrl: './workflow-diagram.component.html',
    providers: [PermissionsService, WorkflowService]
})

export class WorkflowDiagramComponent extends BaseComponent implements OnInit, AfterViewInit, OnChanges {
    @Input() id: number = 0;
    @Input() version: number = null;
    @Input() readonly: boolean = true;
    @Input() hasClose: boolean = false;
    @Input() hasBack: boolean = false;
    @Input() selection: NodeModel | LinkModel;
    @Input() selectedStepId: string;
    @Output() selectedStepIdChange = new EventEmitter();
    @Output() onCloseClick = new EventEmitter();
    @Output() onBackClick = new EventEmitter();
    @Output() selectionChange = new EventEmitter();
    @ViewChild('workflowDiagram') diagramRef;
    @ViewChild('workflowPalette') paletteRef;

    private activityTypes: ActivityTypeInfo[] = [];
    DiagramObjectType = DiagramObjectType;
    StepType = StepType;
    TransitionType = TransitionType;
    WorkflowChangeType = WorkflowChangeType;
    WorkflowActivityType = WorkflowActivityType;
    FormResponseType = FormResponseType;
    model: WorkflowDiagramModel;
    fieldTypes: FieldType[] = [];

    //diagram properties
    private g = go.GraphObject.make;
    private myDiagram: go.Diagram;
    private myPalette: go.Palette;
    private initialLinks: go.Link[] = [];
    private initialNodes: go.Node[] = [];
    private selectedData = null;
    private sel: any;

    private menuItems: MenuItem[] = [];
    private isWindowVisible = false;
    private isReadOnly: boolean = true;
    private tab = 'info';
    private showNodeTabs = false;
    private showLinkTabs = false;
    private overlayHeader = 'Info';
    private overlayMaxHeight = 500;

    private overlayOffset = 391;
    private diagramOffset = 291;

    private hasType = false;

    private formFields: any[] = [];
    private fieldsSub: any;

    private isValid = true;
    private errors: string[]  = [];

    constructor(
        private myElement: ElementRef,
        protected permissionsService: PermissionsService,
        private renderer: Renderer,
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    //#region angular

    public ngOnInit() {
        if (this.readonly.toString().toLowerCase() == 'true')
            this.isReadOnly = true;
        else
            this.isReadOnly = false;

        this.initializeDiagram();
        this.initializeMenuItems();

        this.resizeDiagram();

        this.load();

    }

    public ngOnChanges(changes: SimpleChanges) {
        //TODO: handle on id change
        if (changes['id'].currentValue != changes['id'].previousValue && !changes['id'].isFirstChange) {
            this.myDiagram.div = null;
            this.initializeDiagram();
            this.initializeMenuItems();
        }

        if (changes['selectedStepId'] && changes['selectedStepId'].currentValue != changes['selectedStepId'].previousValue) {
            this.myDiagram.clearSelection();
            this.myDiagram.select(this.myDiagram.findPartForKey(changes['selectedStepId'].currentValue));
        }
    }

    public ngAfterViewInit() {
        //this.resizeDiagram();
    }

    public ngOnDestroy() {
        //garbage collection
        this.myDiagram.div = null;
        if (this.fieldsSub != null)
            this.fieldsSub.unsubscribe();
    }

    //#endregion

    //#region initialization

    private initializeDiagram() {

        this.myDiagram = this.createDiagram();

        this.myDiagram.nodeTemplateMap.add('task', this.createTaskNode());
        this.myDiagram.nodeTemplateMap.add('start', this.createTerminalNode(true));
        this.myDiagram.nodeTemplateMap.add('finish', this.createTerminalNode(false));
        this.myDiagram.linkTemplateMap.add('', this.createDefaultLink());

        this.myDiagram.addDiagramListener('ObjectDoubleClicked', e => this.ObjectDoubleClicked(e));
        this.myDiagram.addDiagramListener('ChangedSelection', e => this.ChangedSelection(e));
        this.myDiagram.addDiagramListener('LinkDrawn', e => this.LinkDrawn(e));
        this.myDiagram.addDiagramListener('PartCreated', () => this.checkHasMultipleInputs());
        this.myDiagram.addDiagramListener('ExternalObjectsDropped', e => this.ExternalObjectsDropped(e));

        this.myDiagram.grid.visible = false;
        this.myDiagram.grid.gridCellSize = new go.Size(24, 24);
        this.myDiagram.toolManager.draggingTool.isGridSnapEnabled = true;
        this.myDiagram.toolManager.resizingTool.isGridSnapEnabled = false;

        this.myDiagram.toolManager.linkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.myDiagram.toolManager.relinkingTool.temporaryLink.routing = go.Link.Orthogonal;
        this.myDiagram.toolManager.linkingTool.isEnabled = !this.isReadOnly;
        this.myDiagram.toolManager.linkingTool.archetypeLinkData = new LinkModel();

        this.myDiagram.commandHandler.deleteSelection = () => this.deleteSelection();


        this.myDiagram.validCycle = go.Diagram.CycleNotDirected; //disallow cycles
        this.myDiagram.maxSelectionCount = 1; //only select 1 item at a time, this makes handling selections a lot easier
    }

    private initializePalette() {
        this.myPalette = this.createPalette();
    }

    private initializeFormFields() {

        if (this.fieldsSub != null) {
            this.fieldsSub.unsubscribe();
            this.fieldsSub = null;
        }

        this.workflowFieldsService.clearUsedFields();

        this.formFields = [];
        //console.log('initializeFormFields', this.myDiagram.model.nodeDataArray);
        this.myDiagram.model.nodeDataArray.forEach(n => {

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
                    ff['@stepId'] = (<NodeModel>n).key;
                    ff['@VersionStepID'] = (<NodeModel>n).key;

                    this.formFields.push(ff);
                });

            }
        });

        this.workflowFieldsService.clearFormFields();
        this.workflowFieldsService.setFormFields(this.formFields);

        (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.forEach(l => {
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
            icon: 'fa-info-circle'
        });
        if (this.hasClose)
            this.menuItems.push({
                icon: 'fa-remove'
            });
    }

    //#endregion

    //#region save/load

    private populateDiagram(): Promise<any> {
        if (this.id < 1) {
            this.model = new WorkflowDiagramModel();
            this.parseData(this.model);
            return Promise.resolve();
        }

        this.isLoading = true;

        return this.workflowService.getWorkflowDiagram(this.id, this.version)
            .then(r => {
                this.model = r;
                if (this.model.Nodes != null)
                    this.model.Nodes.forEach(n => n.ActivityTypeInfo = this.activityTypes.find(a => a.ID == n.ActivityType));
                //console.log(this.model);
                //this.parseData(this.model);
            })
            .then(() => this.workflowService.getWorkflowFieldTypes(this.model.Event.ObjectID, this.model.Event.Object))
            .then(r => this.fieldTypes = r)
            .then(() => this.parseData(this.model))
            .then(() => { this.isLoading = false; this.hasType = true; });


    }

    private parseData(data: WorkflowDiagramModel) {
        this.myDiagram.startTransaction("load_all_data");
        let dm: go.GraphLinksModel = <go.GraphLinksModel>this.myDiagram.model;
        dm.nodeDataArray = [];
        dm.linkDataArray = [];
        this.initialNodes = [];
        this.initialLinks = [];
        var nodeList = [];
        var linkList = [];

        if (data.Nodes)
            data.Nodes.forEach(n => {
                nodeList.push(this.convertToDiagramModel(n, DiagramObjectType.Node))
            });

        if (data.Links)
            data.Links.forEach(l => {
                linkList.push(this.convertToDiagramModel(l, DiagramObjectType.Link))
            });

        nodeList.forEach(n => this.myDiagram.model.addNodeData(n));
        linkList.forEach(l => dm.addLinkData(l));

        dm.linkDataArray.forEach(l => (<LinkModel>l).formInputs = this.getAvailableFormInputs(<LinkModel>l));

        //get deep copy of lists
        this.initialLinks = _.cloneDeep(linkList);
        this.initialNodes = _.cloneDeep(nodeList);

        this.checkHasMultipleInputs();

        this.myDiagram.commitTransaction("load_all_data");
        this.reOrderLayout();
    }

    private save(publish: boolean = false) {

        let links = []; //(<go.GraphLinksModel>this.myDiagram.model).linkDataArray;
        let nodes = []; //this.myDiagram.model.nodeDataArray;


        this.myDiagram.model.nodeDataArray.forEach(n => {
            nodes.push(this.convertToWorkflowModel(<NodeModel>n));
        });

        (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.forEach(l => {
            links.push(this.convertToWorkflowModel(<LinkModel>l));
        });



        let m = new WorkflowDiagramModel();

        this.model.Type.PublishedVersionID = publish ? -1 : null;
        m.Type = this.model.Type;
        m.Event = null; //this.model.Event;
        m.Nodes = nodes;
        m.Links = links;


        console.log('save', m);

        this.isLoading = true;

        this.workflowService.saveWorkflowDiagramModel(m)
            .then(r => {
                //TODO: message and automatically switch to readonly or edit??
                this.onCloseClick.emit();
            });
    }

    private load() {
        this.getActivityTypes()
            .then(() => this.populateDiagram())
            .then(() => this.initializePalette())
            .then(() => this.initializeFormFields())
            .then(() => this.isWindowVisible = !this.isReadOnly);
        //.then(() => { this.resizeDiagram(); this.resizePalette(); });
    }

    //#endregion

    //#region helper methods

    private getAvailableFormInputs(link: LinkModel): string[] {
        let links = [];
        let forms = [];


        let nodes = this.myDiagram.model.nodeDataArray.filter(n => (<any>n).key == link.from);

        while (nodes.length > 0) {
            links = [];
            nodes.forEach(n => {
                if ((<NodeModel>n).activityType == WorkflowActivityType.Form) {
                    forms.push((<NodeModel>n).key);
                }

                links = links.concat((<go.GraphLinksModel>this.myDiagram.model).linkDataArray.filter(l => (<LinkModel>l).to == (<NodeModel>n).key));
                //console.log('links all', (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.filter(l => (<LinkModel>l).to == (<NodeModel>n).key), (<go.GraphLinksModel>this.myDiagram.model).linkDataArray);
            });
            //console.log('nodes: ', nodes);
            nodes = [];
            links.forEach(l => {
                nodes = nodes.concat(this.myDiagram.model.nodeDataArray.filter(n => (<any>n).key == (<any>l).from));
            });
            //console.log('links: ', links);
        }
        //console.log('forms: ', forms);
        return forms;
    }

    private getActivityTypes(): Promise<any> {
        return this.workflowService.getActivityTypes()
            .then(r => {
                let none = r.findIndex(a => a.ID == 0);

                if (none >= 0)
                    r.splice(none, 1);

                this.activityTypes = r;
                //console.log(r);
            });

    }

    private setOverlayHeaderName(p: any) {
        if (p == null) {
            this.overlayHeader = this.tab;
        } else {
            let a = this.activityTypes.find(a => a.ID == p.activityType);
            this.overlayHeader = (a == null) ? ((p.name == null || p.name == '') ? this.tab : p.name) : a.Description + (p.name == null ? '' : ' - ' + p.name);
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

            if (m.ConditionObject != null) {
                n.condition = [];

                if (m.ConditionObject.Condition != null && m.ConditionObject.Condition.length != null) {
                    n.condition = m.ConditionObject.Condition;
                } else if (m.ConditionObject.Condition != null) {
                    n.condition.push(m.ConditionObject.Condition);
                }

                n.condition.forEach(c => {
                    let i = this.fieldTypes.findIndex(f => f.ID == c['@FieldTypeID']);
                    if (i >= 0)
                        c['@FieldName'] = this.fieldTypes[i].FriendlyName;
                });

            } else {
                n.condition = [];
            }

            n.settings = (m.SettingsObject == null) ? {} : m.SettingsObject;
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
            n.runCount = m.RunCount;

            //special case for Form to deal with XML returning an object when field count = 1 instead of an array
            if (n.activityType == WorkflowActivityType.Form) {
                if (n.fields != null && n.fields.form != null && n.fields.form.field != null && n.fields.form.field.length == null) {
                    let f = _.cloneDeep(n.fields.form.field);

                    n.fields.form.field = [];
                    n.fields.form.field.push(f);
                }
            }

            if (m.ActivityTypeInfo != null) {
                n.fore = m.ActivityTypeInfo.ForeColor;
                n.back = m.ActivityTypeInfo.BackColor;
                n.icon = m.ActivityTypeInfo.Icon;
                n.activityName = m.ActivityTypeInfo.Name;
                n.activityDescription = m.ActivityTypeInfo.Description;
            }

            if (m.SettingsObject != null && m.SettingsObject.settings != null)
                n.settings = m.SettingsObject.settings;

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

            //clone conditions so we can remove field name
            let cond = _.cloneDeep(m.condition);
            cond.forEach(c => delete c['@FieldName']);
            n.Condition = JSON.stringify({ Conditions: { Condition: cond } });
            n.Settings = JSON.stringify({ settings: m.settings });

            n.FromPortID = m.frompid;
            n.ToPortID = m.topid;

            return n;

        } else if (model.diagramObjectType == DiagramObjectType.Node) {
            let m: NodeModel = <NodeModel>model;
            let n = new WorkflowDiagramNode();

            n.Key = m.key;
            n.ActivityType = m.activityType;
            n.Name = m.name;
            n.SettingsObject = m.settings;
            n.Settings = JSON.stringify({ settings: m.settings });
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

    private setTransitionIcon(n: LinkModel) {
        switch (+n.transitionType) {
            case TransitionType.Always:
                (<go.GraphLinksModel>this.myDiagram.model).setDataProperty(n, 'icon', '');
                break;
            case TransitionType.Condition:
                (<go.GraphLinksModel>this.myDiagram.model).setDataProperty(n, 'icon', '\uf121');
                break;
            case TransitionType.Timer:
                (<go.GraphLinksModel>this.myDiagram.model).setDataProperty(n, 'icon', '\uf017');
                break;
        }
    }

    private checkHasMultipleInputs() {
        this.myDiagram.model.nodeDataArray.forEach(n => {
            let count = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.filter(l => (<any>l).to == (<any>n).key).length;
            (<any>n).hasMultipleInputs = (count > 1);
            (<any>n).valid = this.validateNode(<NodeModel>n);
        });

        this.validateDiagram();
    }

    private validateNode(n: NodeModel): boolean {
        if (this.isReadOnly)
            return true;

        switch (n.activityType) {
            case WorkflowActivityType.EmailNotification:

                if (n.settings == null || n.settings == {})
                    return false;
                if (n.settings.MessageSubjectTemplate == null || n.settings.MessageSubjectTemplate.length < 1)
                    return false;
                if (n.settings.MessageBodyTemplate == null || n.settings.MessageBodyTemplate.length < 1)
                    return false;
                if (n.settings.MessageRecipientType == null)
                    return false;

                switch (n.settings.MessageRecipientType) {
                    case 'SpecificUser':
                        if (n.settings.MessageToUser == null || n.settings.MessageToUser.length < 1)
                            return false;
                        break;
                    case 'Responsibility':
                        if (n.settings.ResponsibilityTypeID == null || n.settings.ResponsibilityTypeID < 0)
                            return false;
                        break;
                }
                break;
            case WorkflowActivityType.Form:
                if (n.settings == null || n.settings == {})
                    return false;
                if (n.settings.FormResponseType == null)
                    return false;
                if (n.settings.SendFormEmail != null && n.settings.SendFormEmail.toString().toLowerCase() == 'true') {
                    if (n.settings.MessageRecipientType == null)
                        return false;
                    switch (n.settings.MessageRecipientType) {
                        case 'SpecificUser':
                            if (n.settings.MessageToUser == null || n.settings.MessageToUser.length < 1)
                                return false;
                            break;
                        case 'Responsibility':
                            if (n.settings.ResponsibilityTypeID == null || n.settings.ResponsibilityTypeID < 0)
                                return false;
                            break;
                    }
                }

                if (n.fields == null || n.fields == {})
                    return false;
                if (n.fields.form == null)
                    return false;
                if (n.fields.form['@title'] == null || n.fields.form['@title'].length < 1)
                    return false;

                break;
            case WorkflowActivityType.Procedure:
                if (n.settings.ProcedureID == null || n.settings.ProcedureID == '')
                    return false;
                break;
            case WorkflowActivityType.StatusChange:
                if (n.settings.Status == null || n.settings.Status == '')
                    return false;
                break;
        }

        return true;
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
        //console.log('validateDiagram');

        let model = <go.GraphLinksModel>this.myDiagram.model;
        let invalidNodeCount = 0;
        let invalidLinkCount = 0;
        let disconnectedNodeCount = 0;
        let startNodes = 0;
        let finishNodes = 0;

        let startKey = "";
        let finishKey = "";
        let startToFinish = false;

        model.nodeDataArray.forEach(n => {
            let node = <NodeModel>n;

            if (node.valid == false) {
                //console.log('invalid node: ', node);
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

            if (to == null && from == null)
                disconnectedNodeCount++;

        });

        model.linkDataArray.forEach(l => {
            let link = <LinkModel>l;

            if (link.valid == false)
                invalidLinkCount++;
            if (startNodes == 1 && finishNodes == 1 && link.from == startKey && link.to == finishKey)
                startToFinish = true;
        });


        if (invalidNodeCount > 0)
            this.errors.push('There are one or more invalid steps on the diagram (highlighted in red)');

        if (invalidLinkCount > 0)
            this.errors.push('There are one or more invalid transitions on the digram (highlighted in red)');

        if (startNodes != 1)
            this.errors.push('There must be exactly 1 start step on the diagram');

        if (finishNodes != 1)
            this.errors.push('There must be exactly 1 finish step on the diagram');

        if (disconnectedNodeCount > 0)
            this.errors.push('There are steps on the diagram which are not connected');

        if (startToFinish)
            this.errors.push('The start step cannot be connected directly to the finish step');

        if (this.errors.length > 0)
            this.isValid = false;

        return this.isValid;


    }

    //#endregion

    //#region events

    private changeStep(e: NodeModel) {

        this.myDiagram.startTransaction('changeStep');

        let n = this.myDiagram.model.findNodeDataForKey(e.key);
        if (n != null) {
            n.name = e.name;
        }

        //TODO: just set n = e??

        switch (n.activityType) {
            case WorkflowActivityType.EmailNotification: //email
                n.settings.MessageSubjectTemplate = e.settings.MessageSubjectTemplate;
                n.settings.MessageBodyTemplate = e.settings.MessageBodyTemplate;
                n.settings.MessageRecipientType = e.settings.MessageRecipientType;
                n.settings.MessageToUser = e.settings.MessageToUser;
                n.settings.IncludePreviousFormResponses = e.settings.IncludePreviousFormResponses;
                n.settings.ResponsibilityTypeID = e.settings.ResponsibilityTypeID;

                if (e.settings.MessageRecipientType == 'SpecificUser')
                    delete e.settings.ResponsibilityTypeID;
                if (e.settings.MessageRecipientType == 'Responsibility')
                    delete e.settings.MessageToUser;

                break;
            case WorkflowActivityType.StatusChange: //status change
                n.settings.Status = e.settings.Status;
                break;
            case WorkflowActivityType.Form: //form
                n.fields = e.fields;
                n.settings.FormResponseType = e.settings.FormResponseType
                n.settings.SendFormEmail = e.settings.SendFormEmail;
                if (n.settings.SendFormEmail == true) {
                    n.settings.MessageRecipientType = e.settings.MessageRecipientType;
                    n.settings.MessageToUser = e.settings.MessageToUser;
                    n.settings.ResponsibilityTypeID = e.settings.ResponsibilityTypeID;
                } else {
                    delete n.settings.MessageRecipientType;
                    delete n.settings.MessageToUser;
                    delete n.settings.ResponsibilityTypeID;
                }
                break;
            case WorkflowActivityType.Procedure:
                n.settings.ProcedureID = e.settings.ProcedureID;
                break;
        }

        if (e.hasMultipleInputs && e.settings.WaitForAllTransitions != null) {
            n.settings.WaitForAllTransitions = e.settings.WaitForAllTransitions;
        }

        if (!e.hasMultipleInputs && n.settings.WaitForAllTransitions != null)
            delete n.settings.WaitForAllTransitions;
        //console.log('changeStep: ', n, e);

        //n.valid = this.validateNode(n);

        this.myDiagram.model.setDataProperty(n, 'valid', this.validateNode(n));
        this.validateDiagram();

        this.myDiagram.commitTransaction('changeStep');

    }

    private changeTransition(e: LinkModel) {
        this.myDiagram.startTransaction('changeTransition');

        let i = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.findIndex(l => (<any>l).from == e.from && (<any>l).to == e.to);
        let l = null;
        if (i >= 0)
            l = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray[i];
        if (l != null) {

            l.transitionType = e.transitionType;

            this.setTransitionIcon(l);
            l.name = e.name;
            l.condition = e.condition;
            l.settings = e.settings;
            l.icon = e.icon;
            l.formInputs = this.getAvailableFormInputs(e);
            if (this.selectedData != null && this.selectedData.diagramObjectType == DiagramObjectType.Link) {
                this.selectedData.formInputs = l.formInputs;
            }

            this.setTransitionIcon(l);
            (<go.GraphLinksModel>this.myDiagram.model).setDataProperty(l, 'valid', this.validateLink(l));
            this.validateDiagram();
            //console.log('transition change: ', e, l);
        }

        this.myDiagram.commitTransaction('changeTransition');
    }

    private menuClick(e: any) {
        //console.log(e);
        if (e.icon == 'fa-info-circle')
            this.isWindowVisible = !this.isWindowVisible;
        if (e.icon == 'fa-remove')
            this.onCloseClick.emit();
        if (e.icon == 'fa-floppy-o')
            this.save();
        if (e.icon == 'fa-arrow-left')
            this.onBackClick.emit();

        //TODO: debugging remove this
        //this.resizeDiagram();
    }

    @HostListener('window:resize', ['$event'])
    private onResize(event) {
        this.resizeDiagram();
    }

    private resizeDiagram() {
        //set the diagram div to a specific height
        //required for GoJS

        //let offset = this.diagramRef.nativeElement.offsetTop;
        //let height = window.innerHeight;

        //if (this.diagramRef.nativeElement.offsetParent) {
        //    offset += this.diagramRef.nativeElement.offsetParent.offsetTop;
        //}

        //debugging
        //console.log('resizeDiagram :: innerHeight', window.innerHeight);
        //console.log('resizeDiagram :: offsetTop', this.diagramRef.nativeElement.offsetTop);
        //console.log('resizeDiagram :: offsetParent', (this.diagramRef.nativeElement.offsetParent) ? this.diagramRef.nativeElement.offsetParent.offsetTop : 0);
        //console.log('resizeDiagram :: height', height - offset - 35);

        //this.diagramRef.nativeElement.style.height = (height - offset - 35) + 'px';
        //this.paletteRef.nativeElement.style.height = (height - offset - 35) + 'px';

        this.diagramRef.nativeElement.style.height = (window.innerHeight - this.diagramOffset) + 'px';
        this.paletteRef.nativeElement.style.height = (window.innerHeight - this.diagramOffset) + 'px';
        this.overlayMaxHeight = window.innerHeight - this.overlayOffset;

        //this.overlayMaxHeight = height - offset - 35;
        //console.log(this.overlayMaxHeight);
    }

    private reOrderLayout() {
        this.myDiagram.layout.invalidateLayout();
        this.myDiagram.requestUpdate();
    }

    private ChangedSelection(e: any) {
        this.sel = e.diagram.selection;
        if (this.sel.count == 0) {
            this.selectedData = null;
            this.showNodeTabs = false;
            this.showLinkTabs = false;
            this.selectedStepId = null;
            this.selectedStepIdChange.emit(null);
        } else {
            var sel = _.cloneDeep(this.sel.toArray());

            if (sel != null && sel.length != 0) {
                this.selectedData = sel[0].data;
                if (this.selectedData.diagramObjectType == DiagramObjectType.Node) {
                    this.showNodeTabs = true;
                    this.showLinkTabs = false;
                    this.selectedStepId = this.selectedData.key;
                    this.selectedStepIdChange.emit(this.selectedData.key);

                    let i = this.myDiagram.model.nodeDataArray.findIndex(n => (<any>n).key == this.selectedData.key);
                    if (i > -1) {
                        // this.selectedData = this.myDiagram.model.nodeDataArray[i];
                    }
                } else if (this.selectedData.diagramObjectType == DiagramObjectType.Link) {
                    this.showNodeTabs = false; this.showLinkTabs = true;
                    let i = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.findIndex(n => (<any>n).key == this.selectedData.key);
                    if (i > -1) {
                        //this.selectedData = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray[i];
                    }
                }
            }
        }
        this.setOverlayHeaderName(this.selectedData);
        this.selection = this.selectedData;
        this.selectionChange.emit(this.selection);
        //console.log('selection changed: ', e);
        //console.log(this.selection);
    }

    private ObjectDoubleClicked(e: any) {
        //console.log('double clicked', e);
        //var obj = e.diagram.selection.first().data;
    }

    private LinkDrawn(e: any) {
        let link = e.subject;
        let l = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.findIndex(l => (<any>l).from == link.from && (<any>l).to == link.to);
        this.checkHasMultipleInputs();

        if (l > -1) {
            let k = (<LinkModel>(<go.GraphLinksModel>this.myDiagram.model).linkDataArray[l]);
            k.formInputs = this.getAvailableFormInputs(k);
        }
        //console.log(link, l);
    }

    private deleteSelection() {

        if (this.isReadOnly)
            return;

        let links: LinkModel[] = [];
        let nodes: NodeModel[] = [];
        let coll: go.Part[] = [];

        this.myDiagram.selection.each(x => {
            if (x.data.diagramObjectType == DiagramObjectType.Node) {
                nodes.push(x.data);
            } else if (x.data.diagramObjectType == DiagramObjectType.Link) {
                links.push(x.data);
            }
        });

        //get links attached to node if they weren't selected. They will be deleted automagically
        nodes.forEach(n => {
            let to = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.filter(l => (<any>l).to == n.key);
            let from = (<go.GraphLinksModel>this.myDiagram.model).linkDataArray.filter(l => (<any>l).from == n.key);

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
            coll.push(this.myDiagram.findPartForData(l));
        });

        nodes.forEach(n => {
            if (n.activityType == WorkflowActivityType.Form) {
                let canDelete = true;
                if (n.fields.form != null) {
                    n.fields.form.field.forEach(f => {
                        if (this.workflowFieldsService.getUsedFields().findIndex(u => u.stepId == n.key) > -1) {
                            canDelete = false;

                            //need to remove pending delete on link
                            let parts = coll.filter(c => c.data.diagramObjectType == DiagramObjectType.Link && c.data.from == n.key);
                            parts.forEach(p => {
                                let i = coll.findIndex(c => c.data.diagramObjectType == DiagramObjectType.Link && c.data.from == p.data.from);
                                let l = this.myDiagram.findPartForData(p);
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
                        coll.push(this.myDiagram.findPartForData(n));
                    }
                } else {
                    coll.push(this.myDiagram.findPartForData(n));
                }
            } else {
                coll.push(this.myDiagram.findPartForData(n));
            }
        });

        //console.log(nodes, links, coll, this.workflowFieldsService.getFields(), this.workflowFieldsService.getUsedFields());
        this.myDiagram.removeParts(coll, false);
        this.myDiagram.clearSelection();
        this.selectedStepId = null;
        this.selectedStepIdChange.emit(null);
        this.selectedData = null;
        this.validateDiagram();
    }

    private ExternalObjectsDropped(e: any) {
        this.myDiagram.model.nodeDataArray.forEach(n => {
            this.myDiagram.model.setDataProperty(n, 'valid', this.validateNode(<NodeModel>n));
        });

        this.validateDiagram();
    }

    //#endregion

    //#region templates

    private createPalette(): go.Palette {

        //console.log('reached created palette');


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

        paletteModel.push(start);

        let finish = new NodeModel();
        finish.category = 'finish';
        finish.name = 'Finish';
        finish.diagramObjectType = DiagramObjectType.Node;
        finish.stepType = StepType.Finish;
        finish.activityType = 0;
        finish.pos = "0 0";
        finish.valid = true;

        paletteModel.push(finish);

        let terminate = new NodeModel();
        terminate.category = 'finish';
        terminate.name = 'Terminate';
        terminate.diagramObjectType = DiagramObjectType.Node;
        terminate.stepType = StepType.Terminate;
        terminate.activityType = 0;
        terminate.pos = "0 0";
        terminate.valid = true;

        paletteModel.push(terminate);

        this.activityTypes.forEach(a => {

            let m = new NodeModel();

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
            m.settings = {};
            m.fields = {};
            m.valid = true;

            paletteModel.push(m);

        });

        let pt = this.g(go.Palette, "WorkflowPalette",
            {
                "animationManager.duration": 800,
                nodeTemplateMap: this.myDiagram.nodeTemplateMap,
                model: new go.GraphLinksModel(paletteModel),
                layout: this.g(go.GridLayout, { alignment: go.GridLayout.Location })
            });

        return pt;
    }

    private createDiagram(): go.Diagram {

        let dg = this.g(go.Diagram, 'WorkflowDiagram', {
            initialContentAlignment: go.Spot.TopLeft,
            allowDrop: true,
            "undoManager.isEnabled": !this.isReadOnly
        });

        dg.model.class = go.GraphLinksModel;
        dg.model.nodeCategoryProperty = "category";
        dg.model.linkFromPortIdProperty = "frompid";
        dg.model.linkToPortIdProperty = "topid";
        dg.model.nodeDataArray = [];
        dg.model.linkDataArray = [];
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
                mouseEnter: (e, obj) => { this.showPorts(obj.part, true); },
                mouseLeave: (e, obj) => { this.showPorts(obj.part, false); }
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
                    new go.Binding("stroke", "valid", v => { return (v || this.isReadOnly) ? nodeBorderColor : '#f00' })
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
                        new go.Binding("text", "activityName").makeTwoWay(),
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
                mouseEnter: (e, obj) => { this.showPorts(obj.part, true); },
                mouseLeave: (e, obj) => { this.showPorts(obj.part, false); }
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
                new go.Binding("stroke", "valid", v => { return (v || this.isReadOnly) ? "gray" : "red" })),
            this.g(go.Shape, { toArrow: "standard", fill: "gray", stroke: "gray" },
                new go.Binding("fill", "valid", v => { return (v || this.isReadOnly) ? "gray" : "red" }),
                new go.Binding("stroke", "valid", v => { return (v || this.isReadOnly) ? "gray" : "red" })
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
                    new go.Binding("stroke", "valid", v => { return (v || this.isReadOnly) ? "gray" : "red" }),
                    new go.Binding("fill", "valid", v => { return (v || this.isReadOnly) ? "gray" : "red" }),
                    new go.Binding("visible", "icon", function (a) { return (a ? true : false) })
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

    private createEditorLink(): go.Link {
        return this.g(go.Link,  // the whole link panel
            {
                routing: go.Link.AvoidsNodes,
                curve: go.Link.JumpOver,
                corner: 5, toShortLength: 4,
                relinkableFrom: true,
                relinkableTo: true,
                reshapable: true,
                resegmentable: true,
                // mouse-overs subtly highlight links:
                mouseEnter: function (e, link) { link.findObject("HIGHLIGHT").stroke = "rgba(30,144,255,0.2)"; },
                mouseLeave: function (e, link) { link.findObject("HIGHLIGHT").stroke = "transparent"; }
            },
            new go.Binding("points").makeTwoWay(),
            this.g(go.Shape,  // the link path shape
                { isPanelMain: true, stroke: "gray", strokeWidth: 2 }),
            this.g(go.Shape,  // the arrowhead
                { toArrow: "standard", stroke: null, fill: "gray" }),
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
                    new go.Binding("visible", "icon", function (a) { return (a ? true : false) })
                ), // the link shape
                this.g(go.TextBlock, {
                    textAlign: "center", font: "9pt FontAwesome", stroke: "#fff", margin: 0.75
                },
                    // the label
                    new go.Binding("text", "icon").makeTwoWay()
                )
            ),
            this.g(go.Shape,
                { isPanelMain: true, strokeWidth: 8, stroke: "transparent", name: "HIGHLIGHT" })
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
                    toolTip: this.g(go.Adornment, "Auto", this.g(go.Shape, { fill: "lightyellow" }), this.g(go.Panel, "Vertical", this.g(go.TextBlock, { margin: 3, text: 'Item count' })))
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
            , new go.Binding("visible", "runCount", (k) => { return this.isReadOnly && k > 0; })
        );

        return countPanel;
    }

    private makePort2(name: string, leftside: boolean) {
        var port = this.g(go.Shape, "Circle", {
            fill: "white",
            stroke: "gray",
            strokeWidth: 3,
            desiredSize: new go.Size(9, 9),
            portId: name, // declare this object to be a "port"
            cursor: "pointer" // show a different cursor to indicate potential link point
        });

        var panel = this.g(go.Panel, "Horizontal", {
            margin: new go.Margin(2, 0)
        });

        if (leftside) {
            port.toSpot = go.Spot.Left;
            port.toLinkable = true;
            panel.alignment = go.Spot.TopLeft;
            panel.add(port);
        } else {
            port.fromSpot = go.Spot.Right;
            port.fromLinkable = true;
            panel.alignment = go.Spot.TopRight;
            panel.add(port);
        }
        return panel;
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

}

