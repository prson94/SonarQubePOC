import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionService } from '../../../services/fusion.service';
import { MessagesService } from '../../../services/messages.service';
import { FusionRuleItem, FusionRuleItemEditorModel, FusionRule, AttributeNode } from '../../../models/fusion.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rule-item-editor',
    template: `
    <header>Add Promotion Target Item</header>
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div class="row" *ngIf="!isQueryEditor">
        <div class="col s4 offset-s4">
            <div *ngIf="!isLoading">
                <div style="max-height:500px;overflow-y:scroll;position:relative;">
                    <input type="text" style="width:100%;margin-bottom:10px;" [(ngModel)]="addItemSearch" placeholder="Search..." [disabled]="selectAllItems"/>
                    <p-treeTable [value]="values | treeSearch: addItemSearch:'Name'" (onNodeExpand)="loadSubItems($event)">
                        <p-column header="Name" field="Name"></p-column>
                        <p-column [style]="{ 'width' : '30px' }">
                            <ng-template pTemplate type="body" let-row="rowData">
                                <input type="checkbox" [ngModel]="row?.data?.selected" (ngModelChange)="row.data.selected = $event;selectInOriginalTree(row.data.ID,$event);" [disabled]="selectAllItems" />
                            </ng-template>
                        </p-column>
                    </p-treeTable>
                </div>
            </div>
        </div>
        <div class="col s2">
            <input type="checkbox" [(ngModel)]="selectAllItems" /> Select All
        </div>
    </div>

<div class="row" *ngIf="isQueryEditor">
    <div class="col s4 offset-s4">
        <p-dataTable #dtItems [value]="queryValues" paginator="true" pageLinks="3" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
            <footer *ngIf="dtItems.totalRecords"><d3s-grid-paging-info [totalRecords]="dtItems.totalRecords" [first]="dtItems.first" [rows]="dtItems.rows"></d3s-grid-paging-info></footer>
            <p-column header="" field="selected" sortable="false" [style]="{width:'10%'}">
                <ng-template let-item="rowData" pTemplate type="body">
                    <input type="checkbox" [(ngModel)]="item.selected" [disabled]="selectAllItems" />
                </ng-template>
            </p-column>
            <p-column header="Name" field="friendlyName" sortable="true" [style]="{width:'90%'}"></p-column>
        </p-dataTable>
        <div class="col s2">
            <input type="checkbox" [(ngModel)]="selectAllItems" /> Select All
        </div>
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
    queryValues: any[] = [];
    selectAllItems = false;
    addItemSearch = "";

    isQueryEditor = false;

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
        this.isQueryEditor = (this.fusionRule.ObjectType == 'FusionQueryAttributeType');

        if (this.isQueryEditor) {
            this.fusionService.getAddFusionRuleItem(this.fusionRule.ID)
                .then(r => {
                    this.model = r;
                })
                .then(() => this.fusionService.getPromotionQueryAttributes(this.fusionRule.ID))
                .then(r => {
                    //filter out values which have already been selected
                    this.queryValues = r.filter(i => this.model.Items.findIndex(j => j.ObjectType == 'FusionQueryAttribute' && j.ObjectID == i.id) < 0);
                })
                .then(() => this.isLoading = false);
        } else {
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
        }
    }

    save() {
        if (this.isLoading)
            return;
        let form: any = {};
        this.isLoading = true;

        form.RuleID = this.fusionRule.ID;
        form.AllSelected = this.selectAllItems;
        form.ObjectType = this.isQueryEditor ? 'FusionQueryAttribute' : 'FusionAttribute';

        if (this.isQueryEditor)
            form.attributeIDs = this.getSelectedQueryAttributeIDs(this.queryValues).join(',');
        else
            form.attributeIDs = this.getSelectedAttributeNodeIDs().join(',');



        this.fusionService.postAddFusionRuleItem(form)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.onSave.emit();
                this.selectAllItems = false;
                this.addItemSearch = "";
                this.values = [];
                this.queryValues = [];
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

    getSelectedQueryAttributeIDs(records: any[]) {
        return records.filter(r => r.selected == true).map(r => r.id);
    }

    selectInOriginalTree(id: number, event) {
        let node = this.values.find(x => x.data.ID == id);

        if (node) {
            node.data.selected = event;
        }
    }

    //#endregion
}