import { Component, OnInit } from '@angular/core';
import { takeUntil } from "rxjs/operators";
import { Subject } from "rxjs";
import { TreeNode } from 'primeng/api';
import { MapRuleItemDetail } from '../../models/fusion.model';
import { FormMode } from '../../models/form.model';
import { FusionService } from '../../services/fusion.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-technical-mappings',
    template: `
        <div class="tile tile-detail">
            <header>
                Technical Mappings
                <d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions>
            </header>
            <select [(ngModel)]="searchField" style="width:150px;display:inline-block">
                <option *ngFor="let f of searchFields" [value]="f.value">{{f.label}}</option>
            </select>
            <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search"
                   style="width: 300px;display:inline-block;" *ngIf="formMode == FormMode.Default">
            <p-treeTable *ngIf="formMode == FormMode.Default"
                         [value]="technicalMappingsTree|treeSearch: searchValue: searchField" selectionMode="single"
                         [(selection)]="selected">
                <ng-template pTemplate="header">
                    <tr>
                        <th>Group</th>
                        <th>Transformation</th>
                        <th>Source Object</th>
                        <th>Source Configuration</th>
                        <th>Source Attribute</th>
                        <th>Target Object</th>
                        <th>Target Configuration</th>
                        <th>Target Attribute</th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-rowNode let-row="rowData">
                    <tr [ttSelectableRow]="rowNode">
                        <td>
                            <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
                            <div *ngIf="row.ID != 0">{{row.ID}}</div>
                            <div *ngIf="row.ID == 0">NONE</div>
                        </td>
                        <td>{{row.SourceObjectName}}</td>
                        <td>{{row.SourceFusion}}</td>
                        <td>{{row.SourceFusionAttributeTextPath}}</td>
                        <td>{{row.TargetObjectName}}</td>
                        <td>{{row.TargetFusion}}</td>
                        <td>{{row.TargetFusionAttributeTextPath}}</td>
                        <td>
                            <div class="RowTools">
                                <a *ngIf="row.ParentTextID == null" style="cursor: pointer" (click)="add(rowNode.node)"><i
                                        class="fa fa-plus"></i></a>
                                <a style="cursor: pointer"><i class="fa fa-pencil" (click)="edit(nowNode.node)"></i></a>
                                <a style="cursor: pointer"><i class="fa fa-trash-o" (click)="delete(nowNode.node)"></i></a>
                            </div>
                        </td>
                    </tr>
                </ng-template>
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
                            (saveClick)="formMode = FormMode.Default">
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
                            (saveClick)="formMode = FormMode.Default">
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
                            (saveClick)="formMode = FormMode.Default">
                    </d3s-dynamic-editor>
                </div>
            </div>
        </div>
    `,
    providers: [FusionService]
})

export class FusionTechnicalMappingsComponent extends BaseComponent implements OnInit {
    technicalMappings: MapRuleItemDetail[];
    technicalMappingsTree: TreeNode[] = [];

    selection: TreeNode;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    searchValue: string = '';
    searchFields = [];
    searchField: string = 'group';
    selectedParentID: number = null;

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.searchFields = [
            {value: 'Transformation', label: 'Transformation'},
            {value: 'SourceObjectName', label: 'Source Object'},
            {value: 'SourceFusion', label: 'Source Configuration'},
            {value: 'SourceFusionAttributeTextPath', label: 'Source Attribute'},
            {value: 'TargetObjectName', label: 'Target Object'},
            {value: 'TargetFusion', label: 'Target Configuration'},
            {value: 'TargetFusionAttributeTextPath', label: 'Target Attribute'}
        ];

        this.searchField = this.searchFields[0].value;

        this.fusionService
            .getFusionTechnicalMappings()
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(m => {
                this.technicalMappingsTree = [];
                this.technicalMappings = m.filter(i => i.Type == "MapRule");

                for (let t of this.technicalMappings) {
                    let len = this.technicalMappingsTree.push({
                        data: t,
                        label: '',
                        children: []
                    });

                    t.children = m.filter(i => i.Type == "MapRuleItem" && i.ParentTextID == t.TextID);

                    for (let c of t.children) {
                        this.technicalMappingsTree[len - 1].children.push({
                            data: c,
                            label: '',
                            children: [],
                            leaf: true
                        });
                    }
                }
            });
    }

    add(item) {
        this.selection = item;
        this.selectedParentID = this.selection ? this.selection.data.ID : null;
        this.selection = null;
        this.formMode = FormMode.Adding;
    }

    edit(item) {
        this.selection = item;
        this.formMode = FormMode.Editing;
    }

    delete(item) {
        this.selection = item;
        this.formMode = FormMode.Deleting;
    }
}
