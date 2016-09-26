///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionRule, FusionRuleStep, FusionRuleItem, FusionRuleMapping, FusionRuleEditorModel, FusionAttributeType } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rules',
    template: ` 
<div class="tile tile-detail">
    <div [ngSwitch]="formMode">
        <div *ngSwitchDefault>
             <header>Rules</header>
            <div class="row">  
                <div class="col s8">
                    <div style="text-align: right">
                        <d3s-tile-actions hasAdd="true" (addClick)="addRule();" style="float:right;"></d3s-tile-actions>
                    </div>
                    <p-dataTable [value]="fusionRules" selectionMode="single" [(selection)]="selectedFusionRule" (onRowSelect)="loadSteps();">
                        <p-column header="Enabled" field="Enabled"></p-column>
                        <p-column header="Name" field="Name"></p-column>
                        <p-column header="Description" field="Description"></p-column>
                        <p-column header="">
                            <template pTemplate type="body" let-row="rowData">
                                <div class="RowTools">
                                    <a (click)="editRule(row);"><i class="fa fa-pencil"></i></a>
                                    <a (click)="deleteRule(row);"><i class="fa fa-trash-o"></i></a>
                                </div>
                            </template>
                        </p-column>
                    </p-dataTable>
                </div>
                <div class="col s4">
                    <header>Items for selected rule</header>
                    <p-dataTable [value]="fusionRuleItems" selectionMode="single">
                        <p-column header="Limiting Attribute" field="FusionAttributeName"></p-column>
                        <p-column header="">
                            <template pTemplate type="body">
                                <div class="RowTools"></div>
                            </template>
                        </p-column>
                    </p-dataTable>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <header>Steps for selected rule</header>
                    <div style="text-align: right">
                        <d3s-tile-actions hasAdd="true" (addClick)="addStep();" style="float:right;"></d3s-tile-actions>
                    </div>
                    <p-dataTable [value]="fusionRuleSteps" selectionMode="single" [(selection)]="selectedFusionRuleStep" (onRowSelect)="loadMappings();">
                        <p-column header="Action" field="Action"></p-column>
                        <p-column header="Step" field="Step"></p-column>
                        <p-column header="Description" field="Description"></p-column>
                        <p-column header="">
                            <template pTemplate type="body" let-row="rowData">
                                <div class="RowTools">
                                    <a (click)="editStep(row);"><i class="fa fa-pencil"></i></a>
                                    <a (click)="deleteStep(row);"><i class="fa fa-trash-o"></i></a>
                                </div>
                            </template>
                        </p-column>
                    </p-dataTable>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <header>Mappings for selected step</header>
                    <p-dataTable [value]="fusionRuleMappings" selectionMode="single">
                        <p-column header="Source" field="SourceFieldName"></p-column>
                        <p-column header="Target" field="TargetFieldName"></p-column>
                        <p-column header="">
                            <template pTemplate type="body">
                                <div class="RowTools"></div>
                            </template>
                        </p-column>
                    </p-dataTable>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.EditRule">
            <header>Edit Fusion Rule</header>
            <div class="row">
                <div class="col s12">
                    <div class="FieldName" style="display:block;">Promote</div>
                    <select [(ngModel)]="fusionRuleEditorModel.Rule.ObjectID">
                        <option *ngFor="let i of fusionRuleEditorModel.AttributeTypes" [value]="i.ID">{{i.Name}}</option>
                    </select>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <div class="FieldName" style="display:block;">Description</div>
                    <input type="text" pInput  [(value)]="fusionRuleEditorModel.Rule.Description" style="width:80%" />
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <input type="checkbox" [(ngModel)]="fusionRuleEditorModel.Rule.Enabled" /> Enabled?
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Save" (click)="saveRule();"></button>
                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.AddRule">
            <header>Add Fusion Rule</header>
            <div class="row">
                <div class="col s12">
                    <div class="FieldName" style="display:block;">Promote</div>
                    <select [(ngModel)]="addFusionRule.ObjectID">
                        <option *ngFor="let i of addFusionAttributeTypes" [value]="i.ID">{{i.Name}}</option>
                    </select>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <div class="FieldName" style="display:block;">Description</div>
                    <input type="text" pInput  [(value)]="addFusionRule.Description" style="width:80%" />
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <input type="checkbox" [(ngModel)]="addFusionRule.Enabled" /> Enabled?
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Save" (click)="saveAddRule();"></button>
                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.DeleteRule">
            <header>Delete Fusion Rule</header>
            <div class="row">
                <div class="col s12">
                    Are you sure you want to delete this fusion rule?
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Delete" (click)="confirmDeleteRule();"></button>
                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                </div>
            </div>
        </div>
    </div>   
</div>
                `,
    providers: [FusionService]
})

export class FusionRulesComponent extends BaseComponent implements OnInit {
    @Input() fusionID: number;
    @Input() fusionTypeID: number;

    fusionRules: FusionRule[] = [];
    selectedFusionRule: FusionRule;
    fusionRuleSteps: FusionRuleStep[] = [];
    selectedFusionRuleStep: FusionRuleStep;
    fusionRuleMappings: FusionRuleMapping[] = [];
    fusionRuleItems: FusionRuleItem[] = [];
    formMode = FormMode.Default;
    FormMode = FormMode;

    fusionRuleEditorModel: FusionRuleEditorModel;
    addFusionRule: FusionRule;
    addFusionAttributeTypes: FusionAttributeType[] = [];


    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.fusionService.getFusionRules(this.fusionID)
            .then(r => {
                this.fusionRules = r;
                if (this.fusionRules.length > 0) {
                    this.selectedFusionRule = this.fusionRules[0];
                    this.loadSteps();
                }
            });
    }

    loadSteps() {
        if (this.selectedFusionRule == null) {
            this.fusionRuleSteps = [];
            return;
        }
            
        this.fusionService.getFusionRuleSteps(this.selectedFusionRule.ID)
            .then(r => {
                this.fusionRuleSteps = r;
                if (this.fusionRuleSteps.length > 0) {
                    this.selectedFusionRuleStep = this.fusionRuleSteps[0];
                    this.loadMappings();
                }
            });
        this.fusionService.getFusionRuleItems(this.selectedFusionRule.ID)
            .then(r => {
                this.fusionRuleItems = r;
            });


    }

    loadMappings() {
        if (this.selectedFusionRuleStep == null) {
            this.fusionRuleMappings = [];
            return;
        }
        this.fusionService.getFusionRuleStepMappings(this.selectedFusionRuleStep.ID)
            .then(r => {
                this.fusionRuleMappings = r;
            });
    }

    loadItems() {

    }

    addRule() {
        this.addFusionRule = new FusionRule();
        this.addFusionRule.FusionID = this.fusionID;
        this.fusionService.getAddFusionRule(this.fusionTypeID)
            .then(r => {
                this.addFusionAttributeTypes = r;
                this.formMode = FormMode.AddRule;
            });
    }

    saveAddRule() {
        this.fusionService.postAddFusionRule(this.addFusionRule)
            .then(r => {
                this.formMode = FormMode.Default;
                this.loadSteps();
            });
    }

    editRule(row: FusionRule) {
        this.selectedFusionRule = row;
        this.fusionService.getEditFusionRule(this.selectedFusionRule.ID)
            .then(r => {
                this.fusionRuleEditorModel = r;
                console.log(this.fusionRuleEditorModel);
                this.formMode = FormMode.EditRule;
            });
    }

    deleteRule(row: FusionRule) {
        this.selectedFusionRule = row;
        this.formMode = FormMode.DeleteRule;
    }

    confirmDeleteRule() {
        this.fusionService.deleteFusionRuleById(this.selectedFusionRule.ID)
            .then(r => {
                this.formMode = FormMode.Default;
                this.loadSteps();
            });
    }

    saveRule() {
        this.fusionService.postEditFusionRule(this.fusionRuleEditorModel.Rule)
            .then(r => {
                this.formMode = FormMode.Default;
                this.loadSteps();
            });
    }

    editStep(row: FusionRuleStep) {

    }

    deleteStep(row: FusionRuleStep) {

    }

    confirmDeleteStep() {

    }

    saveStep() {

    }

    addStep() {

    }

    saveAddStep() {

    }

};

enum FormMode {
    Default,
    EditRule,
    DeleteRule,
    AddRule,
    EditStep,
    DeleteStep,
    AddStep,
    EditItem,
    DeleteItem,
    AddItem,
}