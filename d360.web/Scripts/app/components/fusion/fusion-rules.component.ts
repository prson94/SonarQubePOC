///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionRule, FusionRuleStep, FusionRuleItem, FusionRuleMapping } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rules',
    template: ` 
<div class="tile tile-detail">
    <header>Rules</header>
    <div class="row">  
        <div class="col s8">
            <p-dataTable [value]="fusionRules" selectionMode="single" [(selection)]="selectedFusionRule" (onRowSelect)="loadSteps();">
                <p-column header="Enabled" field="Enabled"></p-column>
                <p-column header="Name" field="Name"></p-column>
                <p-column header="Description" field="Description"></p-column>
                <p-column header="">
                    <template pTemplate type="body">
                        <div class="RowTools"></div>
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
            <p-dataTable [value]="fusionRuleSteps" selectionMode="single" [(selection)]="selectedFusionRuleStep" (onRowSelect)="loadMappings();">
                <p-column header="Action" field="Action"></p-column>
                <p-column header="Step" field="Step"></p-column>
                <p-column header="Description" field="Description"></p-column>
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

};