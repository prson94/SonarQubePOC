import { Component } from '@angular/core';
import { RelationshipRole } from '../../models/relationship.model';
import { MessagesService, RelationshipsService  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
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
                    <input  [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                    <p-dataTable #dt [globalFilter]="gb" [value]="roles" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;this.showEditor = true;" [(selection)]="selected" >                                                                        
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="Name" header="Name" sortable="custom" (sortFunction)="columnSort($event)" [filter]="!showSimpleFilter"></p-column>                                                                                    
                        <p-column field="Description" header="Description" [sortable]="false" [filter]="!showSimpleFilter">
                            <template let-col let-role="rowData" pTemplate type="body">
                                <div [innerHtml]="role?.Description"></div>
                            </template>                                                        
                        </p-column>    
                        <p-column [style]="{width:'40px'}">
                            <template let-role="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="selected=role;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}">
                            <template let-role="rowData" pTemplate type="body">
                                <div class="RowTools" *ngIf="!role.IsUsed">                                
                                    <a style="cursor:pointer;" (click)="selected=role;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>                            
                    </p-dataTable> 
                </span>
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'RelationshipRole'" [title]="'Relationship Role'" [selection]="selected" (saveClick)="saveRole($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the relationship role [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>              
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


