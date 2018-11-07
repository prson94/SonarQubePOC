import { Component } from '@angular/core';
import { RuleDimension } from '../../../models/rule.model';
import { MessagesService } from '../../../services/messages.service';
import { RulesService  } from '../../../services/rules.service';
import { BaseComponent } from '../../shared/base.component';

@Component({
    selector: 'd3s-admin-rule-dimensions',
    providers: [RulesService],
    template: `
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                           <header *ngIf="!showEditor && !showDelete">Dimensions
                            <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                           </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showDelete && !showEditor">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="dimensions" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','Description']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'Description'">
                                                Description
                                                <d3s-sortIcon [field]="'Description'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'Description'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                            <td>{{item.Name}}</td>
                                            <td>
                                                <div [innerHtml]="item?.Description"></div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>
                            </span>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'RuleDimension'" [title]="'Rule Dimension'" [selection]="selected" (saveClick)="saveDimension($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the dimension [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></d3s-delete-form>              
                        </div>
                    </div>
                </div>
                `
})

export class AdminRuleDimensionsComponent extends BaseComponent {    
    error: any;
    dimensions: RuleDimension[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;
    
    selected: RuleDimension = null;
    theDeleteCallback: Function;

    constructor(
        private rulesService: RulesService,
        private messagesService: MessagesService,        
    ) {
        super();
        this.theDeleteCallback = this.deleteDimension.bind(this);
    }

    ngOnInit() {
        this.getDimensions();
    }
    
    getDimensions() {
        this.isLoading = true;
        this.rulesService
            .getRuleDimensions()
            .then(dimensions => {
                this.dimensions = dimensions
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    deleteDimension(id: number) {
        this.rulesService.deleteDimension(id).
            then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.dimensions.splice(this.findDimensionIndex(id), 1);
            });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.dimensions.length > 0)
            this.selected = this.dimensions[0];
    }

    findDimensionIndex(id: number) {
        var index: number = -1;
        for (var dimension of this.dimensions) {
            index++;
            if (dimension.ID == id) return index;
        }
    }

    saveDimension(event) {
        this.rulesService.saveDimension(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.dimensions[this.dimensions.length] = event.item;
                }
                else {
                    this.dimensions[this.findDimensionIndex(event.item.ID)] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }
}


