
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionRule, FusionRuleStep, FusionRuleItem, FusionRuleMapping, FusionRuleEditorModel, FusionAttributeType, FusionRuleItemEditorModel, FusionRuleMappingEditorModel, FusionRuleStepEditorModel } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rules',
    template: ` 
<div>
    <div [ngSwitch]="formMode">
        <div *ngSwitchDefault >
            <div class="row">  
                <div class="col s8 tile tile-detail">
                    <header>Rules</header>
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
                    <div style="margin-left: 25px;">
                        <div class="tile tile-detail">
                            <header>Items for selected rule</header>
                            <div style="text-align: right">
                                <d3s-tile-actions hasAdd="true" (addClick)="addItem();" style="float:right;"></d3s-tile-actions>
                            </div>
                            <p-dataTable [value]="fusionRuleItems" selectionMode="single" [(selection)]="selectedFusionRuleItem">
                                <p-column header="Limiting Attribute" field="FusionAttributeName"></p-column>
                                <p-column header="">
                                    <template pTemplate type="body">
                                        <div class="RowTools">
                                            <a (click)="deleteItem(row);"><i class="fa fa-trash-o"></i></a>
                                        </div>
                                    </template>
                                </p-column>
                            </p-dataTable>
                        </div>
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col s12 tile tile-detail">
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
                <div class="col s12 tile tile-detail">
                    <header>Mappings for selected step</header>
                    <div style="text-align: right">
                        <d3s-tile-actions hasAdd="true" (addClick)="addMapping();" style="float:right;"></d3s-tile-actions>
                    </div>
                    <p-dataTable [value]="fusionRuleMappings" selectionMode="single" [(selection)]="selectedFusionRuleMapping">
                        <p-column header="Source" field="SourceFieldName"></p-column>
                        <p-column header="Target" field="TargetFieldName"></p-column>
                        <p-column header="">
                            <template pTemplate type="body" let-row="rowData">
                                <div class="RowTools">
                                    <a (click)="editMapping(row);"><i class="fa fa-pencil"></i></a>
                                    <a (click)="deleteMapping(row);"><i class="fa fa-trash-o"></i></a>
                                </div>
                            </template>
                        </p-column>
                    </p-dataTable>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.EditRule" class="tile tile-detail">
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
        <div *ngSwitchCase="FormMode.AddRule" class="tile tile-detail">
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
        <div *ngSwitchCase="FormMode.DeleteRule" class="tile tile-detail">
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
        <div *ngSwitchCase="FormMode.DeleteItem" class="tile tile-detail">
            <header>Delete Fusion Rule Item</header>
            <div class="row">
                <div class="col s12">
                    Are you sure you want to delete this fusion rule item?
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Delete" (click)="confirmDeleteItem();"></button>
                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.EditMapping" class="tile tile-detail">
            <header>Edit Fusion Rule Mapping</header>
            <div class="row">
                <div class="col s12">
                    <input type="checkbox" [(ngModel)]="fusionRuleMappingEditorModel.Item.IsConstantValue" /> Store a fixed source value?
                </div>
            </div>
            <div class="row">
                <div class="col s6" *ngIf="fusionRuleMappingEditorModel.Item.IsConstantValue">
                    <div class="FieldName" style="display:block;">Source</div>
                    <input type="text" [(ngModel)]="fusionRuleMappingEditorModel.Item.ConstantValue" style="width:95%" />
                </div>
                <div class="col s6" *ngIf="!fusionRuleMappingEditorModel.Item.IsConstantValue">
                    <div class="FieldName" style="display:block;">Source</div>
                    <select [(ngModel)]="fusionRuleMappingEditorModel.sourceValue" style="width:95%">
                        <option *ngFor="let i of fusionRuleMappingEditorModel.SourceFields" [value]="i.Value">{{i.Text}}</option>
                    </select>
                </div>
                <div class="col s6">
                    <div class="FieldName" style="display:block;">Target</div>
                    <select [(ngModel)]="fusionRuleMappingEditorModel.targetValue" style="width:95%">
                        <option *ngFor="let i of fusionRuleMappingEditorModel.TargetFields" [value]="i.Value">{{i.Text}}</option>
                    </select>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Save" (click)="saveEditMapping();"></button>
                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.AddMapping" class="tile tile-detail">
            <header>Add Fusion Rule Mapping</header>
            <div class="row">
                <div class="col s12">
                    <input type="checkbox" [(ngModel)]="fusionRuleMappingEditorModel.Item.IsConstantValue" /> Store a fixed source value?
                </div>
            </div>
            <div class="row">
                <div class="col s6" *ngIf="fusionRuleMappingEditorModel.Item.IsConstantValue">
                    <div class="FieldName" style="display:block;">Source</div>
                    <input type="text" [(ngModel)]="fusionRuleMappingEditorModel.Item.ConstantValue" style="width:95%" />
                </div>
                <div class="col s6" *ngIf="!fusionRuleMappingEditorModel.Item.IsConstantValue">
                    <div class="FieldName" style="display:block;">Source</div>
                    <select [(ngModel)]="fusionRuleMappingEditorModel.sourceValue" style="width:95%">
                        <option *ngFor="let i of fusionRuleMappingEditorModel.SourceFields" [value]="i.Value">{{i.Text}}</option>
                    </select>
                </div>
                <div class="col s6">
                    <div class="FieldName" style="display:block;">Target</div>
                    <select [(ngModel)]="fusionRuleMappingEditorModel.targetValue" style="width:95%">
                        <option *ngFor="let i of fusionRuleMappingEditorModel.TargetFields" [value]="i.Value">{{i.Text}}</option>
                    </select>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Save" (click)="saveAddMapping();"></button>
                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.DeleteStep" class="tile tile-detail">
            <header>Delete Fusion Rule Step</header>
            <div class="row">
                <div class="col s12">
                    Are you sure you want to delete this fusion rule step?
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Delete" (click)="confirmDeleteStep();"></button>
                    <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.AddStep">
            <d3s-fusion-rule-step [ruleID]="selectedFusionRule.ID" (onClose)="formMode = FormMode.Default;" (onSave)="formMode.Default; loadSteps();"></d3s-fusion-rule-step>
        </div>
        <div *ngSwitchCase="FormMode.EditStep">
            <d3s-fusion-rule-step [ruleID]="selectedFusionRuleStep.RuleID" [ruleStepID]="selectedFusionRuleStep.ID" (onClose)="formMode = FormMode.Default" (onSave)="formMode = FormMode.Default; loadSteps();"></d3s-fusion-rule-step>
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
    selectedFusionRuleMapping: FusionRuleMapping;
    fusionRuleItems: FusionRuleItem[] = [];
    selectedFusionRuleItem: FusionRuleItem;
    formMode = FormMode.Default;
    FormMode = FormMode;

    fusionRuleEditorModel: FusionRuleEditorModel;
    addFusionRule: FusionRule;
    addFusionAttributeTypes: FusionAttributeType[] = [];

    fusionRuleItemEditorModel: FusionRuleItemEditorModel;
    fusionRuleMappingEditorModel: FusionRuleMappingEditorModel;
    fusionRuleStepEditorModel: FusionRuleStepEditorModel;


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
        this.selectedFusionRuleStep = row;
        this.formMode = FormMode.EditStep;
    }

    deleteStep(row: FusionRuleStep) {
        this.selectedFusionRuleStep = row;
        this.formMode = FormMode.DeleteStep;
    }

    confirmDeleteStep() {
        this.fusionService.deleteFusionRuleStep(this.selectedFusionRuleStep.RuleID, this.selectedFusionRuleStep.ID)
            .then(r => {
                this.loadSteps();
                this.formMode = FormMode.Default;
            });
    }

    saveStep() {

    }

    addStep() {
        this.formMode = FormMode.AddStep;
    }

    saveAddStep() {

    }

    addItem() {
        this.fusionService.getAddFusionRuleItem(this.selectedFusionRule.ID)
            .then(r => {
                this.fusionRuleItemEditorModel = r;
                this.formMode = FormMode.AddItem;
                console.log(r);
            });
    }

    saveAddItem() {

    }

    deleteItem(row: FusionRuleItem) {
        this.selectedFusionRuleItem = row;
        this.formMode = FormMode.DeleteItem;
    }

    confirmDeleteItem() {
        this.fusionService.deleteFusionRuleItem(this.selectedFusionRuleItem.ID)
            .then(r => {
                this.loadSteps();
                this.formMode = FormMode.Default;
            });
    }

    editMapping(row: FusionRuleMapping) {
        this.fusionService.getEditFusionRuleStepMapping(row.ID)
            .then(r => {
                this.fusionRuleMappingEditorModel = r;
                this.fusionRuleMappingEditorModel.sourceValue = this.fusionRuleMappingEditorModel.Item.SourceFieldName + '|' + this.fusionRuleMappingEditorModel.Item.SourceFieldTypeID.toString();
                this.fusionRuleMappingEditorModel.targetValue = this.fusionRuleMappingEditorModel.Item.TargetFieldName + '|' + this.fusionRuleMappingEditorModel.Item.TargetFieldTypeID.toString();

                this.formMode = FormMode.EditMapping;
                console.log(r);
            });
    }

    saveEditMapping() {
        let m = this.fusionRuleMappingEditorModel.Item;
        m.SourceFieldName = this.fusionRuleMappingEditorModel.sourceValue.split('|')[0];
        m.SourceFieldTypeID = parseInt(this.fusionRuleMappingEditorModel.sourceValue.split('|')[1]);
        m.TargetFieldName = this.fusionRuleMappingEditorModel.targetValue.split('|')[0];
        m.TargetFieldTypeID = parseInt(this.fusionRuleMappingEditorModel.targetValue.split('|')[1]);

        this.fusionService.putEditFusionRuleStepMapping(m)
            .then(r => {
                this.loadSteps();
                this.formMode = FormMode.Default;
            });

    }

    addMapping() {
        this.fusionService.getAddFusionRuleStepMapping(this.selectedFusionRuleStep.ID)
            .then(r => {
                this.fusionRuleMappingEditorModel = r;
                this.formMode = FormMode.AddMapping;
                console.log(r);
            });
    }

    saveAddMapping() {
        let m = this.fusionRuleMappingEditorModel.Item;
        m.SourceFieldName = this.fusionRuleMappingEditorModel.sourceValue.split('|')[0];
        m.SourceFieldTypeID = parseInt(this.fusionRuleMappingEditorModel.sourceValue.split('|')[1]);
        m.TargetFieldName = this.fusionRuleMappingEditorModel.targetValue.split('|')[0];
        m.TargetFieldTypeID = parseInt(this.fusionRuleMappingEditorModel.targetValue.split('|')[1]);

        this.fusionService.postAddFusionRuleStepMapping(m)
            .then(r => {
                this.formMode = FormMode.Default;
            });
    }

    deleteMapping(row: FusionRuleMapping) {
        this.selectedFusionRuleMapping = row;
        this.formMode = FormMode.DeleteMapping;
    }

    confirmDeleteMapping() {
        this.fusionService.deleteFusionRuleStepMapping(this.selectedFusionRuleMapping.ID)
            .then(r => {
                this.loadSteps();
                this.formMode = FormMode.Default;
            });
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
    DeleteItem,
    AddItem,
    EditMapping,
    AddMapping,
    DeleteMapping,
}