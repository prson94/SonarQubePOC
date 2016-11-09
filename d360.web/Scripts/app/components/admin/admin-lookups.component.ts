import { Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, LookupService, RightSidebarService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { Lookup } from '../../models/lookup.model';
import { Title } from '@angular/platform-browser';


@Component({
    selector: 'd3s-admin-lookups-component',
    providers: [LookupService],
    template: ` <d3s-audit *ngIf="isAuditVisible" [objectID]="selectedLookup?.ID" [objectName]="selectedLookup?.Name" [objectType]="'LookupType'"></d3s-audit>
                <div class="row" *ngIf="!isAuditVisible">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Lookup Types
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                            </header>   
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!showEditor && !showDelete && !isLoading">       
                                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                      
                                <p-dataTable #dt [globalFilter]="gb" [value]="lookups" selectionMode="single" [rows]="20" paginator="true" pageLinks="3" [(selection)]="selectedLookup"  (onRowDblclick)="selectedLookup=$event.data;showEditor=true;" >                                                        
                                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                    <p-column field="ID" header="ID" [sortable]="true"></p-column>                                                            
                                    <p-column field="Name" header="Name" [sortable]="true"></p-column>                            
                                    <p-column [style]="{width:'40px'}">
                                        <template let-lookup="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selectedLookup=lookup;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <template let-lookup="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selectedLookup=lookup;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </template>
                                    </p-column>                            
                                </p-dataTable>  
                            </span> 
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selectedLookup?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the lookup [' + [selectedLookup?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>  
                            <d3s-admin-lookup-type-editor *ngIf="showEditor" [lookup]="selectedLookup" (saveClick)="saveLookup($event)" (closeClick)="closeEditor()"></d3s-admin-lookup-type-editor>                       
                        </div>
                    </div>                    
                    <div class="col l8 s12" *ngIf="!showDelete && !showEditor && selectedLookup">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'LookupType'" [objectID]="selectedLookup?.ID" ></d3s-field-definition-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">           
                                    <d3s-dynamic-grid [title]="'Items'" [itemName]="'Lookup'" [objectType]="'LookupType'" [objectID]="selectedLookup?.ID" [createUri]="'form/dynamicedit/create/lookup/'" [editUri]="'form/dynamicedit/edit/lookup/'" [dataUri]="lookupUri()" [deleteUri]="'form/DeleteLookupByIdRaw?id='"></d3s-dynamic-grid>                                                                       
                                </div>
                            </div>
                        </div>
                    <div>
                </div>  
                `
})

export class AdminLookupsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    lookups: Lookup[] = [];
    selectedLookup: Lookup;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    constructor(rightSidebarService: RightSidebarService, private lookupService: LookupService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);                
        this.areaName = "Lookup Types";
        this.setCommonItems();
        this.setCommonRightSideBar(true);
    }

    ngOnInit() {
        this.theDeleteCallback = this.deleteLookup.bind(this);
        this.getLookups();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getLookups() {
        this.isLoading = true;
        this.lookupService.getLookups()
            .then(result => {
                this.lookups = result;
                this.isLoading = false;
                if (this.lookups.length > 0) this.selectedLookup = this.lookups[0];
            });            
    }

    deleteLookup(id: number) {
        this.isLoading = true;
        this.lookupService.deleteLookup(id).
            then(result => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;

                if (result.type != 'error') {
                    this.selectedLookup = this.lookups.length > 0 ? this.lookups[0] : null;
                    this.lookups = this.lookups.filter(x => x.ID != id);
                }
            });
    }

    lookupUri() {
        if (this.selectedLookup == null) return "";

        return `resources/lookups/${this.selectedLookup.ID}/items.json`;
    }    

    add() {
        this.showEditor = true;
        this.selectedLookup = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selectedLookup == null) {
            this.selectedLookup = this.lookups.length > 0 ? this.lookups[0] : null;
        }
    }

    saveLookup(event) {
        this.isLoading = true;
        this.lookupService.saveLookup(event.lookup)
            .then(result => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
                if (event.lookup.ID == undefined) {
                    event.lookup.ID = Number(result.id);
                    this.lookups[this.lookups.length] = event.lookup;
                }
                else {
                    let index = this.lookups.findIndex(x => x.ID == event.lookup.ID);

                    if (index >= 0 && index < this.lookups.length) {
                        this.lookups[index] = event.lookup;
                    }
                }
                this.selectedLookup = event.lookup;
                this.showEditor = false;
            });
        
    }
    
}