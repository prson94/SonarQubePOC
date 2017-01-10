import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionService, MessagesService } from '../../../services/index';
import { FusionRuleItem, FusionRuleItemEditorModel, FusionRule, AttributeNode } from '../../../models/fusion.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rule-item-editor',
    template: `
    <header>Add Promotion Target Item</header>
    <div class="row">
        <div class="col s4 offset-s4">
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div *ngIf="!isLoading">
                <div style="max-height:500px;overflow-y:scroll;position:relative;">
                    <input type="text" style="width:100%;margin-bottom:10px;" [(ngModel)]="addItemSearch" placeholder="Search..." [disabled]="selectAllItems"/>
                    <p-treeTable [value]="values | treeSearch: addItemSearch:'Name'" (onNodeExpand)="loadSubItems($event)">
                        <p-column header="Name" field="Name"></p-column>
                        <p-column [style]="{ 'width' : '30px' }">
                            <template pTemplate type="body" let-row="rowData">
                                <input type="checkbox" [ngModel]="row?.data?.selected" (ngModelChange)="row.data.selected = $event;selectInOriginalTree(row.data.ID,$event);" [disabled]="selectAllItems" />
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
            <button type="button" label="Save" (click)="save()" [disabled]="isLoading" pButton></button>
            <button type="button" label="Close" (click)="onClose.emit()" pButton></button>
        </div>
    </div>

`,
    providers: [FusionService]
})

export class FusionRuleItemEditorComponent extends BaseComponent implements OnInit {
    @Input() fusionRule: FusionRule;
    @Input() fusionID: number;
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();
    @Output() onError = new EventEmitter();

    values: TreeNode[] = [];
    attributes: AttributeNode[] = [];
    selectAllItems = false;
    addItemSearch = "";

    model: FusionRuleItemEditorModel;

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        if (this.fusionRule == null || this.fusionRule.ID == null)
            return;
        this.isLoading = true;
        this.fusionService.getAddFusionRuleItem(this.fusionRule.ID)
            .then(r => this.model = r)
            .then(() => this.fusionService.getPromotionChildAttributeNodes(this.model.FusionID, this.model.TargetFusionAttributeTypeID, this.fusionRule.ID))
            .then(r => {
                this.attributes = r;
                this.values = [];

                this.attributes.forEach(i => {
                    i.parentType = this.model.TargetFusionAttributeTypeID;
                    i.selected = false;
                    this.values.push({
                        data: i,
                        expanded: false,
                        leaf: false
                    });
                });
            })
            .then(() => this.isLoading = false);
        //if (this.selectedFusionRule == null || this.selectedFusionRule.ID == null)
        //    return;
        //this.formMode = FormMode.AddItem;
        //this.addItemLoading = true;
        //this.fusionService.getAddFusionRuleItem(this.selectedFusionRule.ID)
        //    .then(r => {
        //        this.fusionRuleItemEditorModel = r;
        //        //console.log(r);
        //    }).then(() => this.fusionService.getPromotionChildAttributeNodes(this.fusionRuleItemEditorModel.FusionID, this.fusionRuleItemEditorModel.TargetFusionAttributeTypeID, this.selectedFusionRule.ID))
        //    .then(r => {
        //        this.fusionAttributeNodeItems = r;
        //        this.attributeNodes = [];

        //        this.fusionAttributeNodeItems.forEach(i => {
        //            i.parentType = this.fusionRuleItemEditorModel.TargetFusionAttributeTypeID;
        //            i.selected = false;
        //            this.attributeNodes.push({
        //                data: i,
        //                expanded: false,
        //                leaf: false
        //            });
        //        });
        //        this.addItemLoading = false;
        //    });
    }

    save() {
        if (this.isLoading)
            return;
        let form: any = {};
        this.isLoading = true;

        form.RuleID = this.fusionRule.ID;
        form.AllSelected = this.selectAllItems;
        form.FusionAttributeID = this.getSelectedAttributeNodeIDs().join(',');

        this.fusionService.postAddFusionRuleItem(form)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.onSave.emit();
                this.selectAllItems = false;
                this.addItemSearch = "";
                this.values = [];
                this.isLoading = false;
            })
            .catch(() => this.onError.emit());        
    }

    //#region helpers

    loadSubItems(e: any) {
        let data = e.node.data;
        data.isLoadingChildren = true;
        this.fusionService.getPromotionChildAttributeNodes(this.fusionID, (data.parentType == 0) ? this.model.TargetFusionAttributeTypeID : data.parentType, this.fusionRule.ID, data.FusionAttributeTypeID, data.ID)
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

    getSelectedAttributeNodeIDs(nodes: TreeNode[] = null, values: number[] = []) {
        if (nodes == null)
            nodes = this.values;
        nodes.forEach(n => {
            if (n.data.selected) {
                values.push(n.data.ID);
            }
            if (n.children) {
                let v = this.getSelectedAttributeNodeIDs(n.children);
                v.forEach(i => { values.push(i) });
            }
        });
        return values;
    }

    selectInOriginalTree(id: number, event) {
        let node = this.values.find(x => x.data.ID == id);

        if (node) {
            node.data.selected = event;
        }
    }

    //#endregion
}