///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { DataTable, Column} from 'primeng/primeng';
import { RuleDimension } from '../../models/rule.model';
import { MessagesService, RulesService  } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { DeleteForm } from '../forms/delete.form';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';


@Component({
    selector: 'd3s-rule-dimensions-tile',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm, DynamicEditorComponent],
    providers: [RulesService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Dimensions
                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Rule Dimension'" (addClick)="add()"></d3s-tile-actions>                            
               </header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="dimensions" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                            
                <p-column field="Description" header="Description" [sortable]="true" [filter]="true">
                    <template let-col let-dimension="rowData">
                        <div [innerHtml]="dimension?.Description"></div>
                    </template>                                                        
                </p-column>    
                    <p-column [style]="{width:'40px'}">
                        <template let-dimension="rowData">
                            <div class="RowTools">
                                <a style="cursor:pointer;" (click)="selected=dimension;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                            </div>
                        </template>
                    </p-column>                            
                    <p-column  [style]="{width:'40px'}">
                        <template let-dimension="rowData">
                            <div class="RowTools">                                
                                <a style="cursor:pointer;" (click)="selected=dimension;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                            </div>
                        </template>
                    </p-column>                            
                </p-dataTable> 
                <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'RuleDimension'" [title]="'Rule Dimension'" [selection]="selected" (saveClick)="saveDimension($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the dimension [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>              
                `
})

export class RuleDimensionsTile {    
    error: any;
    dimensions: RuleDimension[] = [];

    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;
    selected: RuleDimension = null;
    theDeleteCallback: Function;

    constructor(private rulesService: RulesService) {
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
        this.rulesService.deleteDimension(id);
        this.showDelete = false;
        this.dimensions.splice(this.findDimensionIndex(id), 1);
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


