import { Component } from '@angular/core';
import { RelationshipRole } from '../../../models/relationship.model';
import { RelationshipsService  } from '../../../services/relationships.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-relationship-roles',
    providers: [RelationshipsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Relationship Roles
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="roles" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','Description']" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th>Name</th>
                                <th>Description</th>
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
                            <tr (dblclick)="selected=item;this.showEditor = true;" [pSelectableRow]="item">
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
                                    <div class="RowTools" *ngIf="!role.IsUsed">
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
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'RelationshipRole'" [title]="'Relationship Role'" [selection]="selected" (saveClick)="saveRole($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the relationship role [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>              
                `
})

export class AdminRelationshipRolesComponent extends BaseComponent {    
    roles: RelationshipRole[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;
    selected: RelationshipRole = null;
    theDeleteCallback: Function;

    constructor(private relationshipsService: RelationshipsService, private messagesService: MessagesService) {
        super();
        this.theDeleteCallback = this.deleteRelationshipRole.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.relationshipsService
            .getRelationshipRoles()
            .then(roles => {
                this.roles = roles
                this.isLoading = false;
            });            
    }

    deleteRelationshipRole(id: number) {
        this.relationshipsService.deleteRelationshipRole(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                this.roles = this.roles.filter(x => x.ID != id);                
            });
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.roles.length > 0)
            this.selected = this.roles[0];
    }


    saveRole(event) {
        this.relationshipsService.saveRelationshipRole(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {                    
                    event.item.ID = Number(result.id.split('|')[1]);
                    this.roles[this.roles.length] = event.item;
                }
                else {
                    let index = this.roles.findIndex(x => x.ID == event.item.ID);

                    if (index >= 0 && index < this.roles.length) {
                        this.roles[index] = event.item;
                    }
                    else {
                        console.log("[ERROR] UNABLE TO FIND THE ITEM WE ARE EDITING IN THE LIST OF RELATIONSHIP ROLES.");
                    }
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.roles = _.orderBy(this.roles, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
}


