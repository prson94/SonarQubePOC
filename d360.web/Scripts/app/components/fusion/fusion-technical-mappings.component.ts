
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { MapRuleItemDetail } from '../../models/fusion.model';
import { TreeNode, Column } from 'primeng/primeng';
import { FormMode } from '../../models/form.model';

@Component({
    selector: 'd3s-fusion-technical-mappings',
    template: ` 
        <div class="tile tile-detail">
            <header>
                Technical Mappings
                <d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions>
            </header>
            <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;" *ngIf="formMode == FormMode.Default">   
            <p-treeTable *ngIf="formMode == FormMode.Default" [value]="technicalMappingsTree | treeSearch: searchValue" selectionMode="single" [(selection)]="selected">
            <p-column header="Group">
                <template let-row="rowData" pTemplate type="body">
                    <div *ngIf="row.data.ID != 0">{{row.data.ID}}</div>
                    <div *ngIf="row.data.ID == 0">NONE</div>
                </template>
            </p-column>
                <p-column header="Transformation" field="Transformation">
                    <template pTemplate type="body" let-item="rowData">
                        <div [innerHtml]="item.data.Transformation"></div> 
                    </template>
                </p-column>
                <p-column header="Source Object" field="SourceObjectName"></p-column>
                <p-column header="Source Configuration" field="SourceFusion"></p-column>
                <p-column header="Source Attribute" field="SourceFusionAttributeTextPath"></p-column>
                <p-column header="Target Object" field="TargetObjectName"></p-column>
                <p-column header="Target Configuration" field="TargetFusion"></p-column>
                <p-column header="Target Attribute" field="TargetFusionAttributeTextPath"></p-column>
                <p-column header="">
                    <template pTemplate type="body" let-row="rowData">
                        <div class="RowTools">
                            <a *ngIf="row.data.ParentTextID == null" style="cursor: pointer" (click)="add(row)"><i class="fa fa-plus"></i></a>
                            <a style="cursor: pointer"><i class="fa fa-pencil" (click)="edit(row)"></i></a>
                            <a style="cursor: pointer"><i class="fa fa-trash-o" (click)="delete(row)"></i></a>
                        </div>
                    </template>
                </p-column>
            </p-treeTable>
            <div *ngIf="formMode == FormMode.Editing" class="row">
                <div class="col s12">
                    <d3s-dynamic-editor
                        [selection]="selection.data"
                        [title]="selection.data.Type == 'MapRule' ? 'Rule' : 'RuleItem'"
                        [objectType]="selection.data.Type"
                        [objectID]="selection.data.ID"
                        [editUri]="'form/dynamicedit/edit/' + selection.data.Type"
                        (closeClick)="formMode = FormMode.Default"
                        (saveClick)="formMode = FormMode.Default" >
                    </d3s-dynamic-editor>
                </div>
            </div>
            <div *ngIf="formMode == FormMode.Adding" class="row">
                <div class="col s12" *ngIf="selectedParentID != null">
                    <d3s-dynamic-editor
                        [selection]="null"
                        [title]="'Rule Item'"
                        objectType="MapRuleItem"
                        objectID="selectedParentID"
                        createUri="form/dynamicedit/create/mapruleitem"
                        (closeClick)="formMode = FormMode.Default"
                        (saveClick)="formMode = FormMode.Default" >
                    </d3s-dynamic-editor>
                </div>
                <div class="col s12" *ngIf="selectedParentID == null">
                    <d3s-dynamic-editor
                        [selection]="null"
                        [title]="'Rule'"
                        objectType="MapRule"
                        objectID="0"
                        createUri="form/dynamicedit/create/maprule"
                        (closeClick)="formMode = FormMode.Default"
                        (saveClick)="formMode = FormMode.Default" >
                    </d3s-dynamic-editor>
                </div>
            </div>
        </div>
                `,
    providers: [FusionService]
})

export class FusionTechnicalMappingsComponent extends BaseComponent implements OnInit {
    technicalMappings: MapRuleItemDetail[];
    technicalMappingsTree: TreeNode[];

    selection: TreeNode;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    searchValue: string;
    selectedParentID: number = null;

    constructor( private fusionService: FusionService) {   
        super();
    }

    ngOnInit() {
        this.fusionService.getFusionTechnicalMappings().then(m => {
            console.log(m);
            this.technicalMappingsTree = [];
            this.technicalMappings = m.filter(i => i.Type == "MapRule");
            
            for (let t of this.technicalMappings) {
                let n: TreeNode = {};
                n.data = t;
                n.children = [];
                
                t.children = m.filter(i => i.Type == "MapRuleItem" && i.ParentTextID == t.TextID);
                for (let c of t.children) {
                    let cn: TreeNode = {};
                    cn.data = c;
                    n.children.push(cn);
                }

                this.technicalMappingsTree.push(n);
            }
            console.log(this.technicalMappingsTree);

        });
    }

    add(item) {
        this.selection = item;
        console.log(this.selection);
        this.selectedParentID = this.selection ? this.selection.data.ID : null;
        this.selection = null;
        this.formMode = FormMode.Adding;
    }

    edit(item) {
        this.selection = item;
        console.log(this.selection);
        this.formMode = FormMode.Editing;
    }

    delete(item) {
        this.selection = item;
        this.formMode = FormMode.Deleting;
    }
};