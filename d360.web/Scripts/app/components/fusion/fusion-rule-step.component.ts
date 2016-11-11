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
        <div class="row" style="margin-bottom: 20px">
            <div class="col l6 m6 s12">
                <div class="FieldName" style="display:block">Description</div>
                <input type="text" style="width:100%" [(ngModel)]="model.RuleStep.Description"/>
            </div>
            <div class="col l6 m6 s12">
                <div class="FieldName" style="display:block">Action</div>
                <select [(ngModel)]="model.RuleStep.Action" style="width:100%">
                    <option *ngFor="let i of actionTypes" [value]="i.value">{{i.text}}</option>
                </select>
            </div>
        </div>
        <div [ngSwitch]="model.RuleStep.Action">
            <div *ngSwitchCase="'promote'">
                <d3s-fusion-rule-step-promote [ruleID]="ruleID" [ruleStepID]="ruleStepID" [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"></d3s-fusion-rule-step-promote>
            </div>
            <div *ngSwitchCase="'find'">
                <d3s-fusion-rule-step-find [ruleID]="ruleID" [ruleStepID]="ruleStepID" [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"></d3s-fusion-rule-step-find>
            </div>
            <div *ngSwitchCase="'lineage'">
                <d3s-fusion-rule-step-lineage [ruleID]="ruleID" [ruleStepID]="ruleStepID" [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"></d3s-fusion-rule-step-lineage>
            </div>
            <div *ngSwitchCase="'relate'">
                <d3s-fusion-rule-step-relate [ruleID]="ruleID" [ruleStepID]="ruleStepID" [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"></d3s-fusion-rule-step-relate>
            </div>
            <div *ngSwitchCase="'findrelation'"> 
                <d3s-fusion-rule-step-findviarelation [ruleID]="ruleID" [ruleStepID]="ruleStepID" [fusionID]="model.FusionID" [(settings)]="model.RuleStep.Settings"></d3s-fusion-rule-step-findviarelation>
            </div>
        </div> 
        <div class="row" style="margin-top: 20px">
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

    actionTypes: any[] = [
        { text: 'Promote', value: 'promote' },
        { text: 'Find', value: 'find' },
        { text: 'Find via Relationship', value: 'findrelation' },
        { text: 'Lineage', value: 'lineage' },
        { text: 'Relate', value: 'relate' },
    ];

    model: FusionRuleStepEditorModel; 
    rule: FusionRule;
    settings: any;

    constructor(private fusionService: FusionService) {
        super();
    } 

    ngOnInit() {
        this.isLoading = true;
        if (this.ruleStepID == 0) {
            this.fusionService.getAddFusionRuleStep(this.ruleID)
                .then(r => {
                    this.model = r;
                    this.model.RuleStep.Action = 'Promote';
                })
                .then(() => this.isLoading = false);
        } else {
            this.fusionService.getEditFusionRuleStep(this.ruleID, this.ruleStepID)
                .then(r => {
                    this.model = r;
                    this.model.RuleStep.Action = this.model.RuleStep.Action.toLowerCase();
                })
                //.then(() => this.loadSettings())
                .then(() => this.isLoading = false);
        }
    }
    
    //loadSettings(): Promise<any> {
    //    let s = this.model.RuleStep.Settings;
    //    switch (this.model.RuleStep.Action) {
    //        case 'promote':
    //            this.selectedPromotionItem = this.promotionItems.find(i => i.PromotionObjectType == s.Object && i.PromotionObjectID == s.ObjectID).id;
    //            if (s.ParentObjectSearch && s.ParentObjectSearch != "") {
    //                this.selectedPromotionSearchType = s.ParentObjectSearch.toLowerCase();
    //            }
    //            if (this.selectedPromotionSearchType == 'resultfromstep') {
    //                this.selectedPromotionStep = s.ParentObjectID;
    //                return this.changePromotionItem()
    //                    .then(() => this.changePromotionSearchType());
    //            } else if (this.selectedPromotionSearchType == 'direct') {
    //                this.selectedPromotionParent = s.ParentObjectID;
    //                return this.changePromotionItem()
    //                    .then(() => this.changePromotionSearchType());
    //            } else if (this.selectedPromotionSearchType == 'fusionowner') {
    //                this.selectedPromotionFusionItem = s.ParentObjectID;
    //                return this.changePromotionItem()
    //                    .then(() => this.changePromotionSearchType());
    //            }
    //            break;
    //        case 'relate':
    //            this.selectedRelateIntersectType = s.IntersectType;

    //            if (s.Subject == 'Step') {
    //                this.selectedRelateSubjectSearchType = 'resultfromstep';
    //                this.selectedRelateSubjectStep = s.SubjectID;
    //            } else if (s.Subject == 'Owner') {
    //                this.selectedRelateSubjectSearchType = 'fusionowner';
    //                this.selectedRelateSubjectFusionOwnerItem = s.SubjectID;
    //            } else {
    //                this.selectedRelateSubjectSearchType = 'self';
    //            }

    //            if (s.Object == 'Step') {
    //                this.selectedRelateObjectSearchType = 'resultfromstep';
    //                this.selectedRelateObjectStep = s.ObjectID;
    //            } else if (s.Object == 'Owner') {
    //                this.selectedRelateObjectSearchType = 'fusionowner';
    //                this.selectedRelateObjectFusionOwnerItem = s.ObjectID;
    //            } else {
    //                this.selectedRelateObjectSearchType = 'self';
    //            }

    //            return this.changeRelateSearchType(false)
    //                .then(() => this.changeRelateSearchType(true));
    //    }

    //    return Promise.resolve();
    //}


    //saveSettings() {
    //    let action = this.model.RuleStep.Action;
    //    let s = this.model.RuleStep.Settings;
    //    switch (action) {
    //        case 'promote':
    //            let promotionItem = this.promotionItems.find(i => i.id == this.selectedPromotionItem);
    //            s.Object = promotionItem.PromotionObjectType;
    //            s.ObjectID = promotionItem.PromotionObjectID;
    //            s.ParentObjectTypeID = promotionItem.ParentObjectTypeID;

    //            s.ParentObjectSearch = this.selectedPromotionSearchType;
    //            if (this.selectedPromotionSearchType == 'resultfromstep') {
    //                s.ParentObjectID = this.selectedPromotionStep;
    //                s.FindObjectStep = this.selectedPromotionStep;
    //            } else if (this.selectedPromotionSearchType == 'direct') {
    //                s.ParentObjectID = this.selectedPromotionParent;
    //            } else if (this.selectedPromotionSearchType == 'fusionowner') {
    //                s.ParentObjectID = this.selectedPromotionFusionItem;
    //            }
    //            break;
    //        case 'find':
    //            s.FindSearchType = this.selectedFindSearchType.toString();

    //            if (this.selectedFindSearchType == 'glossary') {
    //                s.FilterField = this.selectedFindSourceField.toString();
    //                s.Object = this.selectedFindObjectType;
    //                s.ObjectID = this.selectedFindObject;
    //                s.TargetField = this.selectedFindTargetField;
    //            } else if (this.selectedFindSearchType == 'resultfromstep') {
    //                s.Object = 'Step';
    //                s.ObjectID = this.selectedFindStep;
    //                s.FindParent = this.findByParent;
    //                s.FindObjectStep = this.selectedFindStep;
    //            } else if (this.selectedFindSearchType == 'fusion') {
    //                s.FilterField = this.selectedFindSourceField.toString();
    //                s.Object = 'FusionAttributeType';
    //                s.ObjectID = this.selectedFindFusionItem;
    //            } else if (this.selectedFindSearchType == 'fusionowner') {
    //                s.Object = 'Owner';
    //                s.ObjectID = this.selectedFindFusionItem;
    //            }
    //            break;
    //        case 'relate':
    //            s.IntersectType = this.selectedRelateIntersectType;
    //            s.RelateSubjectSearchType = this.selectedRelateSubjectSearchType;
    //            s.RelateObjectSearchType = this.selectedRelateObjectSearchType;

    //            if (this.selectedRelateSubjectSearchType = 'resultfromstep') {
    //                s.Subject = 'Step';
    //                s.SubjectID = this.selectedRelateSubjectStep;
    //                s.RelateSubjectStep = this.selectedRelateSubjectStep;
    //            } else if (this.selectedRelateSubjectSearchType == 'fusionowner') {
    //                s.Subject = 'Owner';
    //                s.SubjectID = this.selectedRelateSubjectFusionOwnerItem;
    //            } else if (this.selectedRelateSubjectSearchType == 'self') {
    //                s.Subject = 'Self';
    //                s.SubjectID = 0;
    //            }

    //            if (this.selectedRelateObjectSearchType = 'resultfromstep') {
    //                s.Object = 'Step';
    //                s.ObjectID = this.selectedRelateObjectStep;
    //                s.RelateObjectStep = this.selectedRelateObjectStep;
    //            } else if (this.selectedRelateObjectSearchType == 'fusionowner') {
    //                s.Object = 'Owner';
    //                s.ObjectID = this.selectedRelateObjectFusionOwnerItem;
    //            } else if (this.selectedRelateObjectSearchType == 'self') {
    //                s.Object = 'Self';
    //                s.ObjectID = 0;
    //            }
                
    //            break;
    //        case 'lineage':
    //            s.Role = this.selectedLineageRole;
    //            s.SubjectID = this.selectedBusinessSource;
    //            s.ObjectID = this.selectedBusinessTarget;
    //            s.TechnicalSubjectID = this.selectedTechnicalSource;
    //            s.TechnicalObjectID = this.selectedTechnicalTarget;
    //            s.LineageSubjectStep = this.selectedBusinessSource;
    //            s.LineageObjectStep = this.selectedBusinessTarget;
    //            s.LineageTechnicalSubjectStep = this.selectedTechnicalSource;
    //            s.LineageTechnicalObjectStep = this.selectedTechnicalTarget;
    //            break;

    //        case 'findrelation':
    //            s.FindIntersectType = this.selectedFindRelationIntersectType;
    //            s.FindSearchType = this.selectedFindRelationSearchType;
    //            if (this.selectedFindRelationSearchType == 'resultfromstep') {
    //                s.Object = 'Step';
    //                s.ObjectID = this.selectedFindRelationStep;
    //                s.FindObjectStep = this.selectedFindRelationStep;
    //            }
    //            break;
    //    }

    //    this.model.RuleStep.Settings = s;
    //}

    save() {
        //console.log(this.model.RuleStep.Settings);
        if (this.ruleStepID && this.ruleStepID != 0) {
            //edit
            //this.saveSettings();
            this.fusionService.putEditFusionRuleStep(this.model.RuleStep)
                .then(r => {
                    this.onSave.emit(r);
                });
        } else {
            //add
            //this.saveSettings();
            this.fusionService.postAddFusionRuleStep(this.model.RuleStep)
                .then(r => {
                    this.onSave.emit(r);
                });
        }
    }

};

