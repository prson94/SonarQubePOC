
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService, MessagesService } from '../../services/index';
import {
    AttributeNode,
    FusionRule,
    FusionRuleStep,
    FusionRuleItem,
    FusionRuleMapping,
    FusionRuleEditorModel,
    FusionAttributeType,
    FusionRuleItemEditorModel,
    FusionRuleMappingEditorModel,
    FusionRuleStepEditorModel } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-rules',
    template: ` 
<div>
    <div [ngSwitch]="formMode">
        <div *ngSwitchDefault >
            <div class="row">  
                <div class="col l8 m6 s12 tile tile-detail">
                    <header>Rules<d3s-tile-actions hasAdd="true" (addClick)="addRule();" style="float:right;"></d3s-tile-actions></header>
                    <p-dataTable [value]="fusionRules" selectionMode="single" [(selection)]="selectedFusionRule" (onRowSelect)="loadSteps();">
                        <p-column header="Enabled" field="Enabled" [sortable]="true" [style]="{width:'15%'}">
                            <template let-item="rowData" pTemplate type="body">
                                <i *ngIf="item.Enabled" class="fa fa-check enabled" title="Enabled"></i>
                                <i *ngIf="!item.Enabled" class="fa fa-times disabled" title="Disabled"></i>
                            </template>
                        </p-column>
                        <p-column header="Name" field="ObjectName"></p-column>
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
                <div class="col l4 m6 s12">
                    <div class="tile tile-detail">
                        <header>Items for selected rule<d3s-tile-actions hasAdd="true" (addClick)="addItem();" style="float:right;"></d3s-tile-actions></header>
                        <p-dataTable #dt [value]="fusionRuleItems" selectionMode="single" [(selection)]="selectedFusionRuleItem" [rows]="rowsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="[5,10,20]">
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column header="Limiting Attribute" field="FusionAttributeName"></p-column>
                            <p-column header="">
                                <template pTemplate type="body" let-row="rowData">
                                    <div class="RowTools">
                                        <a (click)="deleteItem(row);"><i class="fa fa-trash-o"></i></a>
                                    </div>
                                </template>
                            </p-column>
                        </p-dataTable>
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col s12 tile tile-detail">
                    <header>Steps for selected rule <d3s-tile-actions hasAdd="true" (addClick)="addStep();" style="float:right;"></d3s-tile-actions></header>
                    <p-dataTable [value]="fusionRuleSteps" selectionMode="single" [(selection)]="selectedFusionRuleStep" (onRowSelect)="loadMappings();">
                        <p-column header="Step" field="Step" [style]="{width:'10%'}"></p-column>
                        <p-column header="Action" field="Action" [style]="{width:'15%'}"></p-column>
                        <p-column header="Description" field="Description"></p-column>
                        <p-column header="" [style]="{width:'15%'}">
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
                    <header>Mappings for selected step<d3s-tile-actions hasAdd="true" (addClick)="addMapping();" style="float:right;"></d3s-tile-actions></header>
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
                    <input type="text" pInput  [(ngModel)]="addFusionRule.Description" style="width:80%" />
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
            <form #mappingForm="ngForm" (ngSubmit)="saveEditMapping()">
                <div class="row">
                    <div class="col s12">
                        <input type="checkbox" [(ngModel)]="fusionRuleMappingEditorModel.Item.IsConstantValue" name="isConstant" /> Store a fixed source value?
                    </div>
                </div>
                <div class="row">
                    <div class="col s6" *ngIf="fusionRuleMappingEditorModel.Item.IsConstantValue">
                        <div class="FieldName" style="display:block;">Source</div>
                        <input type="text" [(ngModel)]="fusionRuleMappingEditorModel.Item.ConstantValue" style="width:95%" name="constantValue" required/>
                    </div>
                    <div class="col s6" *ngIf="!fusionRuleMappingEditorModel.Item.IsConstantValue">
                        <div class="FieldName" style="display:block;">Source</div>
                        <select [(ngModel)]="fusionRuleMappingEditorModel.sourceValue" style="width:95%" name="source" required>
                            <option *ngFor="let i of fusionRuleMappingEditorModel.SourceFields" [value]="i.Value">{{i.Text}}</option>
                        </select>
                    </div>
                    <div class="col s6">
                        <div class="FieldName" style="display:block;">Target</div>
                        <select [(ngModel)]="fusionRuleMappingEditorModel.targetValue" style="width:95%" name="target" required>
                            <option *ngFor="let i of fusionRuleMappingEditorModel.TargetFields" [value]="i.Value">{{i.Text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12" style="padding-top:10px">
                        <button pButton type="submit" label="Save" [disabled]="!mappingForm.form.valid"></button>
                        <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                    </div>
                </div>
            </form>
        </div>
        
        <div *ngSwitchCase="FormMode.AddMapping" class="tile tile-detail">
            <header>Add Fusion Rule Mapping</header>
            <form #mappingForm="ngForm" (ngSubmit)="saveAddMapping()">
                <div class="row">
                    <div class="col s12">
                        <input type="checkbox" [(ngModel)]="fusionRuleMappingEditorModel.Item.IsConstantValue" name="isConstant" /> Store a fixed source value?
                    </div>
                </div>
                <div class="row">
                    <div class="col s6" *ngIf="fusionRuleMappingEditorModel.Item.IsConstantValue">
                        <div class="FieldName" style="display:block;">Source</div>
                        <input type="text" [(ngModel)]="fusionRuleMappingEditorModel.Item.ConstantValue" style="width:95%" name="constant" required />
                    </div>
                    <div class="col s6" *ngIf="!fusionRuleMappingEditorModel.Item.IsConstantValue">
                        <div class="FieldName" style="display:block;">Source</div>
                        <select [(ngModel)]="fusionRuleMappingEditorModel.sourceValue" style="width:95%" name="source" required>
                            <option *ngFor="let i of fusionRuleMappingEditorModel.SourceFields" [value]="i.Value">{{i.Text}}</option>
                        </select>
                    </div>
                    <div class="col s6">
                        <div class="FieldName" style="display:block;">Target</div>
                        <select [(ngModel)]="fusionRuleMappingEditorModel.targetValue" style="width:95%" name="target" required>
                            <option *ngFor="let i of fusionRuleMappingEditorModel.TargetFields" [value]="i.Value">{{i.Text}}</option>
                        </select>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12" style="padding-top:10px">
                        <button pButton type="submit" label="Save" [disabled]="!mappingForm.form.valid"></button>
                        <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
                    </div>
                </div>
            </form>
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
            <d3s-fusion-rule-step [ruleID]="selectedFusionRule.ID" (onClose)="formMode = FormMode.Default;" (onSave)="saveAddEditStep($event)"></d3s-fusion-rule-step>
        </div>

        <div *ngSwitchCase="FormMode.EditStep">
            <d3s-fusion-rule-step [ruleID]="selectedFusionRuleStep.RuleID" [ruleStepID]="selectedFusionRuleStep.ID" (onClose)="formMode = FormMode.Default" (onSave)="saveAddEditStep($event)"></d3s-fusion-rule-step>
        </div>

        <div *ngSwitchCase="FormMode.DeleteMapping">
            <div class="tile tile-detail">
                   <delete-form
                        [uri]="'form/DeleteFusionRuleStepMappingByID?id=' + selectedFusionRuleMapping?.ID"
                        [method]="'delete'"
                        [prompt]="'Are you sure you want to delete this fusion rule step mapping?'"                                         
                        (onCancel)="formMode = FormMode.Default;"
                        (onDeleteComplete)="confirmDeleteMapping()"
                    ></delete-form>   
            </div>
        </div>

        <div *ngSwitchCase="FormMode.AddItem">
            <div class="tile tile-detail">
                <header>Add Promotion Target Item</header>  
                <div class="row">
                    <div class="col s4 offset-s4">
                        <d3s-loading [isLoading]="addItemLoading"></d3s-loading>
                        <div *ngIf="!addItemLoading">                        
                            <div style="max-height:500px;overflow-y:scroll;position:relative;">
                            <div *ngIf="selectAllItems" style="position:absolute;left:0;top:0;width:100%;height:100%;background-color:rgba(1,1,1,.2);z-index:1"></div>
                                <input type="text" style="width:100%;margin-bottom:10px;" [(ngModel)]="addItemSearch" placeholder="Search..." />
                                <p-treeTable [value]="attributeNodes | treeSearch: addItemSearch:'Name'" (onNodeExpand)="loadSubItems($event)" >
                                    <p-column header="Name" field="Name"></p-column>
                                    <p-column [style]="{ 'width' : '30px' }">
                                        <template pTemplate type="body" let-row="rowData">
                                            <input type="checkbox" [ngModel]="row?.data?.selected" (ngModelChange)="row.data.selected = $event;selectInOriginalTree(row.data.ID,$event);" />
                                        </template>
                                    </p-column>
                                </p-treeTable>
                            </div>
                        </div>
                    </div>
                    <div class="col s2">
                        <input type="checkbox" [(ngModel)]="selectAllItems" /> Select All
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <button type="button" label="Save" (click)="saveAddItem()" pButton ></button>
                        <button type="button" label="Close" (click)="formMode = FormMode.Default;addItemSearch = '';" pButton ></button>
                    </div>
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
    @Input() rowsPerPage: number = 10;

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
    fusionAttributeNodeItems: AttributeNode[] = [];
    attributeNodes: TreeNode[] = [];
    addItemLoading = false;
    selectAllItems = false;
    addItemSearch = "";

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.loadRules();
    }

    loadRules() {
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
        this.addFusionRule.Description = "";
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
                this.showMessageForResult(this.messagesService, r);
                this.loadRules();
            });
    }

    editRule(row: FusionRule) {
        this.selectedFusionRule = row;
        this.fusionService.getEditFusionRule(this.selectedFusionRule.ID)
            .then(r => {
                this.fusionRuleEditorModel = r;
                //console.log(this.fusionRuleEditorModel);
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
                this.showMessageForResult(this.messagesService, r);
                this.loadRules();
            });
    }

    saveRule() {
        this.fusionService.postEditFusionRule(this.fusionRuleEditorModel.Rule)
            .then(r => {
                this.formMode = FormMode.Default;
                this.showMessageForResult(this.messagesService, r);
                this.loadRules();
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
                this.showMessageForResult(this.messagesService, r);
                this.formMode = FormMode.Default;
            });
    }

    addStep() {
        if (this.selectedFusionRule == null || this.selectedFusionRule.ID == null)
            return;
        this.formMode = FormMode.AddStep;
    }

    addItem() {
        if (this.selectedFusionRule == null || this.selectedFusionRule.ID == null)
            return;
        this.formMode = FormMode.AddItem;
        this.addItemLoading = true;
        this.fusionService.getAddFusionRuleItem(this.selectedFusionRule.ID)
            .then(r => {
                this.fusionRuleItemEditorModel = r;
                //console.log(r);
            }).then(() => this.fusionService.getPromotionChildAttributeNodes(this.fusionRuleItemEditorModel.FusionID, this.fusionRuleItemEditorModel.TargetFusionAttributeTypeID, this.selectedFusionRule.ID))
            .then(r => {
                this.fusionAttributeNodeItems = r;
                this.attributeNodes = [];

                this.fusionAttributeNodeItems.forEach(i => {
                    i.parentType = this.fusionRuleItemEditorModel.TargetFusionAttributeTypeID;
                    i.selected = false;
                    this.attributeNodes.push({
                        data: i,
                        expanded: false,
                        leaf: false
                    });
                });
                //console.log(r);
                this.addItemLoading = false;
            });
    }

    loadSubItems(e: any) {
        let data = e.node.data;
        data.isLoadingChildren = true;
        this.fusionService.getPromotionChildAttributeNodes(this.fusionID, (data.parentType == 0) ? this.fusionRuleItemEditorModel.TargetFusionAttributeTypeID : data.parentType, this.selectedFusionRule.ID, data.FusionAttributeTypeID, data.ID)
            .then(r => {
                if (r.length == 0) {
                    e.node.leaf = true;
                }
                else {
                    e.node.children = [];
                    r.forEach(i => {
                        i.parentType = data.FusionAttributeTypeID;
                        e.node.children.push({
                            data: i,
                            expanded: false,
                            leaf: false
                        });
                    });
                }
                data.isLoadingChildren = false;
            });
    }

    saveAddItem() {
        let form: any = {};

        //this.attributeNodes.forEach

        form.RuleID = this.selectedFusionRule.ID;
        form.AllSelected = this.selectAllItems;
        form.FusionAttributeID = this.getSelectedAttributeNodeIDs().join(',');

        //console.log(this.attributeNodes);
        //console.log(this.attributeNodes.filter(a => a.data.selected));
        //console.log(form);

        this.fusionService.postAddFusionRuleItem(form)
            .then(r => {
                //console.log(r);
                this.showMessageForResult(this.messagesService, r);
                this.formMode = FormMode.Default;
                this.selectAllItems = false;
                this.addItemSearch = '';
                this.attributeNodes = [];
                this.loadRules();
            });
        //console.log(form);
    }

    getSelectedAttributeNodeIDs(nodes: TreeNode[] = null, values: number[]  = []) {
        if (nodes == null)
            nodes = this.attributeNodes;
        nodes.forEach(n => {
            //console.log(n);
            if (n.data.selected) {
                values.push(n.data.ID);
                //console.log(n.data.ID);
            }
            if (n.children) {
                let v = this.getSelectedAttributeNodeIDs(n.children);
                v.forEach(i => { values.push(i) });
            }
                
        });
        return values;
    }

    deleteItem(row: FusionRuleItem) {
        this.selectedFusionRuleItem = row;
        //console.log(row);
        this.formMode = FormMode.DeleteItem;
    }

    confirmDeleteItem() {
        this.fusionService.deleteFusionRuleItem(this.selectedFusionRuleItem.ID)
            .then(r => {
                this.loadSteps();
                this.showMessageForResult(this.messagesService, r);
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
                //console.log(r);
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
                this.showMessageForResult(this.messagesService, r);
                this.formMode = FormMode.Default;
            });

    }

    addMapping() {
        if (this.selectedFusionRuleStep == null || this.selectedFusionRuleStep.ID == null)
            return;
        this.fusionService.getAddFusionRuleStepMapping(this.selectedFusionRuleStep.ID)
            .then(r => {
                this.fusionRuleMappingEditorModel = r;
                this.formMode = FormMode.AddMapping;
                //console.log(r);
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
                this.showMessageForResult(this.messagesService, r);
                this.formMode = FormMode.Default;
            }).then(() => {
                this.loadMappings();
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
                this.showMessageForResult(this.messagesService, r);
                this.formMode = FormMode.Default;
            });
    }

    saveAddEditStep(e: any) {
        this.showMessageForResult(this.messagesService, e);
        this.formMode = FormMode.Default;
        this.loadSteps();
    }

    selectInOriginalTree(id: number, event) {
        let node = this.attributeNodes.find(x => x.data.ID == id);

        if (node) {
            node.data.selected = event;
        }
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