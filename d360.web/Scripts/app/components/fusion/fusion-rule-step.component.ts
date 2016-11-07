import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionRuleStep, FusionRuleStepEditorModel, PromotionObject, FusionRule } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rule-step',
    template: ` 
<div class="tile tile-detail">
    <header *ngIf="ruleStepID != 0">Edit Rule Step</header>
    <header *ngIf="ruleStepID == 0">Add Rule Step</header>
    <div *ngIf="isLoading">

    </div>
    <div *ngIf="!isLoading">
        <div class="row">
            <div class="col s8 offset-s2">
                <div class="FieldName" style="display:block">Description</div>
                <input type="text" style="width:95%" [(ngModel)]="model.RuleStep.Description"/>
            </div>
        </div>
        <div class="row">
            <div class="col s8 offset-s2">
                <div class="FieldName" style="display:block">Action</div>
                <select [(ngModel)]="model.RuleStep.Action" style="width:95%" (ngModelChange)="loadAction()">
                    <option *ngFor="let i of actionTypes" [value]="i.value">{{i.text}}</option>
                </select>
            </div>
        </div>
        <div [ngSwitch]="model.RuleStep.Action">
            <div *ngSwitchCase="'promote'">
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Promote To</div>
                        <select [(ngModel)]="selectedPromotionItem" style="width:95%" (ngModelChange)="changePromotionItem()">
                            <option *ngFor="let i of promotionItems" [value]="i.id">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="showPromotionParent">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Parent Search</div>
                        <select [(ngModel)]="selectedPromotionSearchType" style="width:95%" (ngModelChange)="changePromotionSearchType()">
                            <option *ngFor="let i of promotionSearchTypes" [value]="i.value">{{i.text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="showPromotionParent && selectedPromotionSearchType == 'direct'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Promote Under</div>
                        <select [(ngModel)]="selectedPromotionParent" style="width:95%" >
                            <option *ngFor="let i of promotionParents" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="showPromotionParent && selectedPromotionSearchType == 'resultfromstep'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Result From Step</div>
                        <select [(ngModel)]="selectedPromotionStep" style="width:95%" >
                            <option *ngFor="let i of promotionSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="showPromotionParent && selectedPromotionSearchType == 'fusionowner'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Fusion Owner</div>
                        <select [(ngModel)]="selectedPromotionFusionItem" style="width:95%" >
                            <option *ngFor="let i of promotionFusionItems" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
            </div>
            <div *ngSwitchCase="'find'">
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display: block">Search Type</div>
                        <select [(ngModel)]="selectedFindSearchType" style="width:95%" (ngModelChange)="changeFindSearchType()">
                            <option *ngFor="let i of findSearchTypes" [value]="i.value">{{i.text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType != 'fusionowner' && selectedFindSearchType != 'resultfromstep'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display: block">Source Matching Field</div>
                        <select [(ngModel)]="selectedFindSourceField" style="width:95%" >
                            <option *ngFor="let i of findSourceFields" [value]="i.ID">{{i.FriendlyName}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'glossary'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display: block">Type</div>
                        <select [(ngModel)]="selectedFindObjectType" style="width:95%" (ngModelChange)="changeFindObjectType()">
                            <option *ngFor="let i of findObjectTypes" [value]="i.value">{{i.text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'glossary' && selectedFindObjectType && selectedFindObjectType != ''">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display: block">Item</div>
                        <select [(ngModel)]="selectedFindObject" style="width:95%" (ngModelChange)="changeFindObject()">
                            <option *ngFor="let i of findObjects" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'glossary' && selectedFindObjectType && selectedFindObjectType != '' && showFindTargetField">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display: block">Target Matching Field</div>
                        <select [(ngModel)]="selectedFindTargetField" style="width:95%">
                            <option *ngFor="let i of findTargetFields" [value]="i.ID">{{i.FriendlyName}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'resultfromstep'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display: block">Result From Step</div>
                        <select [(ngModel)]="selectedFindStep" style="width:95%" (ngModelChange)="changeFindObjectType()">
                            <option *ngFor="let i of findSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'resultfromstep'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display: block">Find items parent?</div>
                        <input type="checkbox" [(ngModel)]="findByParent" /> Enabled?
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'fusionowner'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Fusion Owner</div>
                        <select [(ngModel)]="selectedFindFusionItem" style="width:95%" >
                            <option *ngFor="let i of findFusionOwnerItems" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'fusion'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Fusion Attribute Type</div>
                        <select [(ngModel)]="selectedFindFusionItem" style="width:95%" >
                            <option *ngFor="let i of findFusionItems" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'promotion'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Fusion Attribute Type</div>
                        <select [(ngModel)]="selectedFindFusionItem" style="width:95%" (ngModelChange)="changeFindFusionItem()">
                            <option *ngFor="let i of findFusionItems" [value]="i.ID">{{i.TextPath}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindSearchType == 'promotion' && selectedFindFusionItem && selectedFindFusionItem != ''">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Promotion Step</div>
                        <select [(ngModel)]="selectedFindPromotionItem" style="width:95%">
                            <option *ngFor="let i of findPromotionItems" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
            </div>
            <div *ngSwitchCase="'lineage'">
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Role</div>
                        <select [(ngModel)]="selectedLineageRole" style="width:95%">
                            <option *ngFor="let i of lineageRoles" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Source From Step (Business Relation)</div>
                        <select [(ngModel)]="selectedBusinessSource" style="width:95%">
                            <option *ngFor="let i of lineageSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Target From Step (Business Relation)</div>
                        <select [(ngModel)]="selectedBusinessTarget" style="width:95%">
                            <option *ngFor="let i of lineageSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Source From Step (Technical Relation)</div>
                        <select [(ngModel)]="selectedTechnicalSource" style="width:95%">
                            <option *ngFor="let i of lineageSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Target From Step (Technical Relation)</div>
                        <select [(ngModel)]="selectedTechnicalTarget" style="width:95%">
                            <option *ngFor="let i of lineageSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
            </div>
            <div *ngSwitchCase="'relate'">
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Intersect Type</div>
                        <select [(ngModel)]="selectedRelateIntersectType" style="width:95%">
                            <option *ngFor="let i of relateIntersectTypes" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Subject Search Type</div>
                        <select [(ngModel)]="selectedRelateSubjectSearchType" style="width:95%" (ngModelChange)="changeRelateSearchType(true)">
                            <option *ngFor="let i of relateSearchTypes" [value]="i.value">{{i.text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedRelateSubjectSearchType && selectedRelateSubjectSearchType == 'resultfromstep'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Subject Step</div>
                        <select [(ngModel)]="selectedRelateSubjectStep" style="width:95%" >
                            <option *ngFor="let i of relateSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedRelateSubjectSearchType && selectedRelateSubjectSearchType == 'fusionowner'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Subject Fusion Owner Rule</div>
                        <select [(ngModel)]="selectedRelateSubjectFusionOwnerItem" style="width:95%" >
                            <option *ngFor="let i of relateFusionOwnerItems" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Object Search Type</div>
                        <select [(ngModel)]="selectedRelateObjectSearchType" style="width:95%" (ngModelChange)="changeRelateSearchType(false)">
                            <option *ngFor="let i of relateSearchTypes" [value]="i.value">{{i.text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedRelateObjectSearchType && selectedRelateObjectSearchType == 'resultfromstep'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Object Step</div>
                        <select [(ngModel)]="selectedRelateObjectStep" style="width:95%" >
                            <option *ngFor="let i of relateSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedRelateObjectSearchType && selectedRelateObjectSearchType == 'fusionowner'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Object Fusion Owner Rule</div>
                        <select [(ngModel)]="selectedRelateObjectFusionOwnerItem" style="width:95%" >
                            <option *ngFor="let i of relateFusionOwnerItems" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div> 
            </div>
            <div *ngSwitchCase="'findrelation'"> 
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Intersect Type</div>
                        <select [(ngModel)]="selectedFindRelationIntersectType" style="width:95%" > 
                            <option *ngFor="let i of findRelationIntersectTypes" [value]="i.ID">{{i.Name}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Search Type</div>
                        <select [(ngModel)]="selectedFindRelationSearchType" style="width:95%"  (ngModelChange)="changeFindRelationSearchType()">
                            <option *ngFor="let i of findRelationSearchTypes" [value]="i.value">{{i.text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row" *ngIf="selectedFindRelationSearchType && selectedFindRelationSearchType == 'resultfromstep'">
                    <div class="col s8 offset-s2">
                        <div class="FieldName" style="display:block">Result From Step</div>
                        <select [(ngModel)]="selectedFindRelationStep" style="width:95%" >
                            <option *ngFor="let i of findRelationSteps" [value]="i.ID">{{i.Description}}</option>
                        </select>
                    </div>
                </div>
            </div>
        </div> 
        <div class="row">
            <div class="col s12">
                <button type="button" label="Save" pButton (click)="save();"></button>
                <button type="button" label="Cancel" pButton (click)="onClose.emit()"></button>
            </div>
        </div>
    </div>
</div>`,
    providers: [FusionService] 
})

export class FusionRuleStepComponent extends BaseComponent implements OnInit {
    @Input() ruleID: number;
    @Input() ruleStepID: number = 0;
     
    @Output() onClose = new EventEmitter();
    @Output() onSave = new EventEmitter();

    model: FusionRuleStepEditorModel; 
    rule: FusionRule;


    actionTypes: any[] = [
        { text: 'Promote', value: 'promote' },
        { text: 'Find', value: 'find' },
        { text: 'Find via Relationship', value: 'findrelation' },
        { text: 'Lineage', value: 'lineage' },
        { text: 'Relate', value: 'relate' },
    ];


    //promote
    promotionSearchTypes: any[] = [
        { value: "direct", text: "Direct" },
        { value: "resultfromstep", text: "Result From Step" },
        { value: "fusionowner", text: "Fusion Owner" },
    ];

    promotionItems: PromotionObject[] = [];
    showPromotionParent = false;
    selectedPromotionItem: string;
    selectedPromotionSearchType: any;
    promotionParents: any[] = [];
    selectedPromotionParent;
    promotionSteps: any[] = [];
    selectedPromotionStep;
    promotionFusionItems: any[] = [];
    selectedPromotionFusionItem;


    //find
    showFindTargetField = false;
    findSearchTypes: any[] = [
        { value: "glossary", text: "Glossary" },
        { value: "resultfromstep", text: "Result From Step" },
        { value: "fusionowner", text: "Fusion Owner" },
        { value: "fusion", text: "Fusion" },
        { value: "promotion", text: "Previous Promotion" }
    ];

    findObjectTypes: any[] = [
        { value: "ArtifactType", text: "Artifact" },
        { value: "TaxonomyType", text: "Model" }
    ];

    selectedFindSearchType;
    selectedFindObjectType;
    findSourceFields: any[] = [];
    selectedFindSourceField;
    findObjects: any[] = [];
    selectedFindObject;
    findTargetFields: any[] = [];
    selectedFindTargetField;
    findSteps: any[] = [];
    selectedFindStep;
    findByParent = false;
    findFusionOwnerItems: any[] = [];
    selectedFindFusionOwnerItem;
    findFusionItems: any[] = [];
    selectedFindFusionItem;
    findPromotionItems: any[] = [];
    selectedFindPromotionItem;

    //lineage
    lineageRoles: any[] = [];
    selectedLineageRole;
    lineageSteps: any[] = [];
    selectedBusinessSource;
    selectedBusinessTarget;
    selectedTechnicalSource;
    selectedTechnicalTarget;

    //relate
    relateIntersectTypes: any[] = [];
    selectedRelateIntersectType;
    relateSearchTypes: any[] = [
        { value: "resultfromstep", text: "Result From Step" },
        { value: "self", text: "Self" },
        { value: "fusionowner", text: "Fusion Owner Rule" },
    ];
    selectedRelateSubjectSearchType;
    selectedRelateObjectSearchType;
    relateSteps: any[] = [];
    selectedRelateSubjectStep;
    selectedRelateObjectStep;
    relateFusionOwnerItems: any[] = [];
    selectedRelateSubjectFusionOwnerItem;
    selectedRelateObjectFusionOwnerItem;


    //find relation
    findRelationSearchTypes: any[] = [
        { value: "self", text: "Self" },
        { value: "resultfromstep", text: "Result From Step" }
    ];
    selectedFindRelationSearchType;
    findRelationIntersectTypes: any[] = [];
    selectedFindRelationIntersectType;
    findRelationSteps: any[] = [];
    selectedFindRelationStep;

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        if (this.ruleStepID == 0) {
            this.fusionService.getAddFusionRuleStep(this.ruleID)
                .then(r => {
                    this.model = r;
                    this.model.RuleStep.Action = this.actionTypes[0].value;
                })
                .then(() => this.loadAction())
                .then(() => this.isLoading = false);
        } else {
            this.fusionService.getEditFusionRuleStep(this.ruleID, this.ruleStepID)
                .then(r => {
                    this.model = r;
                    this.model.RuleStep.Action = this.model.RuleStep.Action.toLowerCase();
                    console.log(this.model);
                })
                .then(() => this.loadAction())
                .then(() => this.loadSettings())
                .then(() => this.isLoading = false);
        }
    }
    
    loadSettings(): Promise<any> {
        let s = this.model.RuleStep.Settings;
        switch (this.model.RuleStep.Action) {
            case 'promote':
                this.selectedPromotionItem = this.promotionItems.find(i => i.PromotionObjectType == s.Object && i.PromotionObjectID == s.ObjectID).id;
                if (s.ParentObjectSearch && s.ParentObjectSearch != "") {
                    this.selectedPromotionSearchType = s.ParentObjectSearch.toLowerCase();
                }
                if (this.selectedPromotionSearchType == 'resultfromstep') {
                    this.selectedPromotionStep = s.ParentObjectID;
                    return this.changePromotionItem()
                        .then(() => this.changePromotionSearchType());
                } else if (this.selectedPromotionSearchType == 'direct') {
                    this.selectedPromotionParent = s.ParentObjectID;
                    return this.changePromotionItem()
                        .then(() => this.changePromotionSearchType());
                } else if (this.selectedPromotionSearchType == 'fusionowner') {
                    this.selectedPromotionFusionItem = s.ParentObjectID;
                    return this.changePromotionItem()
                        .then(() => this.changePromotionSearchType());
                }
                break;
            case 'find':
                this.selectedFindSourceField = s.FilterField;
                if (s.Object == 'TaxonomyType' || s.Object == 'ArtifactType') {
                    this.selectedFindSearchType = 'glossary';
                    this.selectedFindObjectType = s.Object;
                    this.selectedFindObject = s.ObjectID;
                    this.selectedFindTargetField = s.TargetField;
                    return this.changeFindSearchType()
                        .then(() => this.changeFindObjectType())
                        .then(() => this.changeFindObject());
                } else if (s.Object == 'Step') {
                    this.selectedFindSearchType = 'resultfromstep';
                    this.selectedFindStep = s.ObjectID;
                    this.findByParent = (s.FindParent == '1');
                    return this.changeFindSearchType();
                } else if (s.Object == 'FusionAttributeType') {
                    this.selectedFindSearchType = 'fusion';
                    this.selectedFindFusionItem = s.ObjectID;
                    return this.changeFindSearchType();
                } else if (s.Object == 'Owner') {
                    this.selectedFindSearchType = 'fusionowner';
                    this.selectedFindFusionItem = s.ObjectID;
                    return this.changeFindSearchType();
                }
                break;
            case 'lineage':
                this.selectedLineageRole = s.Role;
                this.selectedBusinessSource = s.SubjectID;
                this.selectedBusinessTarget = s.ObjectID;
                this.selectedTechnicalSource = s.TechnicalSubjectID;
                this.selectedTechnicalTarget = s.TechnicalObjectID;
                break;
            case 'relate':
                this.selectedRelateIntersectType = s.IntersectType;

                if (s.Subject == 'Step') {
                    this.selectedRelateSubjectSearchType = 'resultfromstep';
                    this.selectedRelateSubjectStep = s.SubjectID;
                } else if (s.Subject == 'Owner') {
                    this.selectedRelateSubjectSearchType = 'fusionowner';
                    this.selectedRelateSubjectFusionOwnerItem = s.SubjectID;
                } else {
                    this.selectedRelateSubjectSearchType = 'self';
                }

                if (s.Object == 'Step') {
                    this.selectedRelateObjectSearchType = 'resultfromstep';
                    this.selectedRelateObjectStep = s.ObjectID;
                } else if (s.Object == 'Owner') {
                    this.selectedRelateObjectSearchType = 'fusionowner';
                    this.selectedRelateObjectFusionOwnerItem = s.ObjectID;
                } else {
                    this.selectedRelateObjectSearchType = 'self';
                }

                return this.changeRelateSearchType(false)
                    .then(() => this.changeRelateSearchType(true));
        }

        return Promise.resolve();
    }

    loadAction(): Promise<any> {
        

        switch (this.model.RuleStep.Action) {
            case 'promote':
                this.promotionItems = [];
                return this.fusionService.getFusionPromotionItems(this.model.FusionID, this.model.FusionTypeID)
                    .then(r => {
                        this.promotionItems = r;
                        this.promotionItems.forEach(i => {
                            i.id = (i.ParentObjectTypeID || 0).toString() + '|' + i.PromotionObjectID.toString() + '|' + i.PromotionObjectType;
                        });
                    });
            case 'find':
                this.findSourceFields = [];
                return this.fusionService.getFusionRules(this.model.FusionID)
                    .then(r => {
                        this.rule = r.find(i => i.ID == this.ruleID);
                    })
                    .then(() => this.fusionService.getFindSourceFields(this.rule.ObjectType, this.rule.ObjectID))
                    .then(r => {
                        this.findSourceFields = r;
                        this.findSourceFields.push({
                            ID: 0,
                            FriendlyName: 'Name'
                        });
                        this.findSourceFields.push({
                            ID: -2,
                            FriendlyName: 'ParentID'
                        });
                        console.log(this.findSourceFields);
                    });
            case 'lineage':
                this.lineageRoles = [];
                this.lineageSteps = [];
                return this.fusionService.getLineageRoles()
                    .then(r => {
                        this.lineageRoles = r;
                    })
                    .then(() => this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID))
                    .then(r => {
                        this.lineageSteps = r;
                    });
            case 'relate':
                this.relateIntersectTypes = [];
                return this.fusionService.getRelateIntersectTypes()
                    .then(r => {
                        this.relateIntersectTypes = r;
                    });
            case 'findrelation':
                this.findRelationIntersectTypes = [];
                return this.fusionService.getFusionRelationIntersectTypes()
                    .then(r => {
                        this.findRelationIntersectTypes = r;
                    });
            default:
                return Promise.resolve();
        }
    }

    saveSettings() {
        let action = this.model.RuleStep.Action;
        let s = this.model.RuleStep.Settings;
        console.log(s);
        switch (action) {
            case 'promote':
                let promotionItem = this.promotionItems.find(i => i.id == this.selectedPromotionItem);
                s.Object = promotionItem.PromotionObjectType;
                s.ObjectID = promotionItem.PromotionObjectID;
                s.ParentObjectTypeID = promotionItem.ParentObjectTypeID;

                s.ParentObjectSearch = this.selectedPromotionSearchType;
                if (this.selectedPromotionSearchType == 'resultfromstep') {
                    s.ParentObjectID = this.selectedPromotionStep;
                    s.FindObjectStep = this.selectedPromotionStep;
                } else if (this.selectedPromotionSearchType == 'direct') {
                    s.ParentObjectID = this.selectedPromotionParent;
                } else if (this.selectedPromotionSearchType == 'fusionowner') {
                    s.ParentObjectID = this.selectedPromotionFusionItem;
                }
                break;
            case 'find':
                s.FilterField = this.selectedFindSourceField.toString();
                s.FindSearchType = this.selectedFindSearchType.toString();
                if (this.selectedFindSearchType == 'glossary') {
                    s.Object = this.selectedFindObjectType;
                    s.ObjectID = this.selectedFindObject;
                    s.TargetField = this.selectedFindTargetField;
                } else if (this.selectedFindSearchType == 'resultfromstep') {
                    s.Object = 'Step';
                    s.ObjectID = this.selectedFindStep;
                    s.FindParent = this.findByParent;
                    s.FindObjectStep = this.selectedFindStep;
                } else if (this.selectedFindSearchType == 'fusion') {
                    s.Object = 'FusionAttributeType';
                    s.ObjectID = this.selectedFindFusionItem;
                } else if (this.selectedFindSearchType == 'fusionowner') {
                    s.Object = 'Owner';
                    s.ObjectID = this.selectedFindFusionItem;
                }
                break;
            case 'relate':
                s.IntersectType = this.selectedRelateIntersectType;
                s.RelateSubjectSearchType = this.selectedRelateSubjectSearchType;
                s.RelateObjectSearchType = this.selectedRelateObjectSearchType;

                if (this.selectedRelateSubjectSearchType = 'resultfromstep') {
                    s.Subject = 'Step';
                    s.SubjectID = this.selectedRelateSubjectStep;
                    s.RelateSubjectStep = this.selectedRelateSubjectStep;
                } else if (this.selectedRelateSubjectSearchType == 'fusionowner') {
                    s.Subject = 'Owner';
                    s.SubjectID = this.selectedRelateSubjectFusionOwnerItem;
                } else if (this.selectedRelateSubjectSearchType == 'self') {
                    s.Subject = 'Self';
                    s.SubjectID = 0;
                }

                if (this.selectedRelateObjectSearchType = 'resultfromstep') {
                    s.Object = 'Step';
                    s.ObjectID = this.selectedRelateObjectStep;
                    s.RelateObjectStep = this.selectedRelateObjectStep;
                } else if (this.selectedRelateObjectSearchType == 'fusionowner') {
                    s.Object = 'Owner';
                    s.ObjectID = this.selectedRelateObjectFusionOwnerItem;
                } else if (this.selectedRelateObjectSearchType == 'self') {
                    s.Object = 'Self';
                    s.ObjectID = 0;
                }
                
                break;
            case 'lineage':
                s.Role = this.selectedLineageRole;
                s.SubjectID = this.selectedBusinessSource;
                s.ObjectID = this.selectedBusinessTarget;
                s.TechnicalSubjectID = this.selectedTechnicalSource;
                s.TechnicalObjectID = this.selectedTechnicalTarget;
                s.LineageSubjectStep = this.selectedBusinessSource;
                s.LineageObjectStep = this.selectedBusinessTarget;
                s.LineageTechnicalSubjectStep = this.selectedTechnicalSource;
                s.LineageTechnicalObjectStep = this.selectedTechnicalTarget;
                break;

            case 'findrelation':
                s.FindIntersectType = this.selectedFindRelationIntersectType;
                s.FindSearchType = this.selectedFindRelationSearchType;
                if (this.selectedFindRelationSearchType == 'resultfromstep') {
                    s.Object = 'Step';
                    s.ObjectID = this.selectedFindRelationStep;
                    s.FindObjectStep = this.selectedFindRelationStep;
                }
                break;
        }

        this.model.RuleStep.Settings = s;
    }

    changePromotionItem(): Promise<any> {
        let item = this.promotionItems.find(i => i.id == this.selectedPromotionItem);
        console.log(item);
        if (item.ParentObjectTypeID && item.ParentObjectTypeID != 0) {
            this.showPromotionParent = true;
            return this.changePromotionSearchType();
        }
        else
            this.showPromotionParent = false;
        return Promise.resolve();
    }

    changePromotionSearchType(): Promise<any> {
        switch (this.selectedPromotionSearchType) {
            case 'direct':
                this.promotionParents = [];
                let item = this.promotionItems.find(i => i.id == this.selectedPromotionItem);
                let ot = item.PromotionObjectType == 'ArtifactType' ? 'Artifact' : item.PromotionObjectType;
                return this.fusionService.getPromotionParents(item.ParentObjectTypeID, ot)
                    .then(r => {
                        this.promotionParents = r;
                    });
            case 'resultfromstep':
                this.promotionSteps = [];
                return this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.promotionSteps = r;
                    });
            case 'fusionowner':
                this.promotionFusionItems = [];
                return this.fusionService.getPromotionFusionOwnerRules(this.model.FusionID)
                    .then(r => {
                        this.promotionFusionItems = r;
                    });
            default:
                return Promise.resolve();
        }
    }

    changeFindSearchType(): Promise<any> {
        switch (this.selectedFindSearchType) {
            case 'glossary':
                return Promise.resolve();
            case 'resultfromstep':
                this.findSteps = [];
                return this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                    .then(r => {
                        this.findSteps = r;
                    });
            case 'fusionowner':
                this.findFusionOwnerItems = [];
                return this.fusionService.getPromotionFusionOwnerRules(this.model.FusionID)
                    .then(r => {
                        this.findFusionOwnerItems = r;
                        this.findFusionOwnerItems.forEach(i => {
                            i.text = i.FusionAttributeName + ' Owned By:' + i.OwnerObject;
                        });
                    });
            case 'fusion':
                this.findFusionItems = [];
                return this.fusionService.getFindFusionAttributeTypes()
                    .then(r => {
                        this.findFusionItems = r;
                    });
            case 'promotion':
                this.findFusionItems = [];
                return this.fusionService.getFindAttributeTypes()
                    .then(r => {
                        this.findFusionItems = r;
                    });
            default:
                return Promise.resolve();
        }
    }

    changeFindObjectType(): Promise<any> {
        this.findObjects = [];
        if (this.selectedFindObjectType == 'ArtifactType')
            return this.fusionService.getFindArtifactTypes()
                .then(r => {
                    this.findObjects = r;
                    console.log(r);
                });
        if (this.selectedFindObjectType == 'TaxonomyType')
           return  this.fusionService.getFindModels()
                .then(r => {
                    this.findObjects = r;
                    console.log(r);
                });
        return Promise.resolve();
    }

    changeFindObject(): Promise<any> {
        let item = this.findObjects.find(i => i.ID == this.selectedFindObject);

        this.findTargetFields = [];
        if (this.selectedFindObjectType == 'ArtifactType' && item.ParentID != null && item.ParentID != 0) {
            return this.fusionService.getFindSourceFields('ArtifactType', item.ID)
                .then(r => {
                    this.findTargetFields = r;
                    this.findTargetFields.push({
                        ID: 0,
                        FriendlyName: 'Name'
                    });
                    this.showFindTargetField = true;
                });
        } else if (this.selectedFindObjectType == 'TaxonomyType' && item.MaximumDepth > 1) {
            return this.fusionService.getFindSourceFields('TaxonomyType', item.ID)
                .then(r => {
                    this.findTargetFields = r;
                    this.findTargetFields.push({
                        ID: 0,
                        FriendlyName: 'Name'
                    });
                    this.showFindTargetField = true;
                });
        } else
            this.showFindTargetField = false;
        return Promise.resolve();

    }

    changeFindFusionItem(): Promise<any> {
        //console.log(this.findFusionItems);
        let item = this.findFusionItems.find(i => i.ID == this.selectedFindFusionItem);
        this.findPromotionItems = [];
        return this.fusionService.getFindPromotions(item.ID)
            .then(r => {
                this.findPromotionItems = r;
            });
    }

    changeRelateSearchType(subject: boolean): Promise<any> {
        let searchType = subject ? this.selectedRelateSubjectSearchType : this.selectedRelateObjectSearchType;

        switch (searchType) {
            case 'resultfromstep':
                if (this.relateSteps.length == 0) {
                    return this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                        .then(r => {
                            this.relateSteps = r;
                        });
                }
                break;
            case 'fusionowner':
                if (this.relateFusionOwnerItems.length == 0) {
                    return this.fusionService.getPromotionFusionOwnerRules(this.model.FusionID)
                        .then(r => {
                            this.relateFusionOwnerItems = r;
                            this.relateFusionOwnerItems.forEach(i => {
                                i.text = i.FusionAttributeName + ' Owned By:' + i.OwnerObject;
                            });
                        });
                }
                break;
            default:
                return Promise.resolve();
        }
    }

    changeFindRelationSearchType(): Promise<any> {
        if (this.selectedFindRelationSearchType && this.selectedFindRelationSearchType == 'resultfromstep') {
            this.findRelationSteps = [];
            return this.fusionService.getPromotionRuleSteps(this.ruleID, this.ruleStepID)
                .then(r => {
                    this.findRelationSteps = r;
                });
        }

        return Promise.resolve();

    }

    save() {
        if (this.ruleStepID && this.ruleStepID != 0) {
            //edit
            this.saveSettings();
            console.log(this.model.RuleStep);
            this.fusionService.putEditFusionRuleStep(this.model.RuleStep)
                .then(r => {
                    this.onSave.emit(r);
                });
        } else {
            //add
            this.saveSettings();
            console.log(this.model.RuleStep);
            this.fusionService.postAddFusionRuleStep(this.model.RuleStep)
                .then(r => {
                    this.onSave.emit(r);
                });
        }
    }

};

