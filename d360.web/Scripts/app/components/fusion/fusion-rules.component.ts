
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
    templateUrl: './fusion-rules.component.html',
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
                var saItem = r.find(i => i.TargetFieldName == "TaxonomyTypeID");
                if (saItem != undefined) {
                    saItem.TargetFieldName = "Subject Area";
                }
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

        if (!m.IsConstantValue) {
            let sv = this.fusionRuleMappingEditorModel.sourceValue.split('|');
            m.SourceFieldName = sv[0];
            m.SourceFieldTypeID = parseInt(sv[1]);
        }
        else {
            m.SourceFieldName = null;
            m.SourceFieldTypeID = 0;
        }
        let tv = this.fusionRuleMappingEditorModel.targetValue.split('|');
        m.TargetFieldName = tv[0];
        m.TargetFieldTypeID = parseInt(tv[1]);

        this.fusionService.putEditFusionRuleStepMapping(m)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.formMode = FormMode.Default;
            })
            .then(r => {
                this.loadMappings();
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

        if (!m.IsConstantValue) {
            let sv = this.fusionRuleMappingEditorModel.sourceValue.split('|');
            m.SourceFieldName = sv[0];
            m.SourceFieldTypeID = parseInt(sv[1]);
        }
        let tv = this.fusionRuleMappingEditorModel.targetValue.split('|');
        m.TargetFieldName = tv[0];
        m.TargetFieldTypeID = parseInt(tv[1]);

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