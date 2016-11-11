import { Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, PoliciesService, RightSidebarService, StateService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { PolicyType } from '../../models/policy.model';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-policies-component',
    providers: [PoliciesService],
    template: `<d3s-audit *ngIf="isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'PolicyType'"></d3s-audit>
                <div *ngIf="!isAuditVisible" class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Policy Types
                                <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showEditor && !showDelete">
                                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="policyTypes" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >
                                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                    <p-column field="Name" header="Name" sortable="true"  [filter]="!showSimpleFilter"></p-column>     
                                    <p-column field="PolicyTypeClass" header="Classification" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                                               
                                    <p-column field="MaximumDepth" header="Max Depth" sortable="true"  [filter]="!showSimpleFilter"></p-column>                                                        
                                    <p-column [style]="{width:'40px'}">
                                        <template let-policy="rowData"  pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=policy;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <template let-policy="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selected=policy;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </template>
                                    </p-column>    
                                </p-dataTable>      
                            </span>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'PolicyType'" [title]="'Policy Type'" [selection]="selected" (saveClick)="savePolicyType($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                            <delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the policy type [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></delete-form>        
                        </div>
                    </div>               
                    <div class="col l8 s12" *ngIf="!showEditor && !showDelete && selected">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <object-detail [objectType]="'PolicyType'" [objectID]="selected?.ID"></object-detail>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'PolicyType'" [objectID]="selected?.ID" ></d3s-field-definition-tile>     
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-claims-tile [objectType]="'PolicyType'" [objectID]="selected?.ID" [readonly]="false"></d3s-claims-tile>                 
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">           
                                    <d3s-people-responsibilities-tile [objectType]="'RuleType'" [objectID]="selected?.ID" [showHidden]="true"></d3s-people-responsibilities-tile>                        
                                </div>
                            </div>
                        </div>
                    <div>
                </div>  
                `
})

export class AdminPoliciesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    policyTypes: PolicyType[] = [];
    selected: PolicyType;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    constructor(private stateService: StateService, rightSidebarService: RightSidebarService, private policiesService: PoliciesService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Policy Types";
        this.setCommonItems();
        this.theDeleteCallback = this.deletePolicyType.bind(this);
        this.setCommonRightSideBar(true);
    }

    ngOnInit() {
        this.getPolicyTypes();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getPolicyTypes() {
        this.isLoading = true;
        this.policiesService.getPolicyTypesWithClassification()
            .then(result => {
                this.policyTypes = result;
                this.isLoading = false;
                if (this.policyTypes.length > 0) this.selected = this.policyTypes[0];
            });
    }
        
    deletePolicyType(id: number) {
        this.policiesService.deletePolicy(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.selected = this.policyTypes.length > 0 ? this.policyTypes[0] : null;
                    this.policyTypes = this.policyTypes.filter(x => x.ID != id);
                }
                this.stateService.reloadLeftNavMenu();
            });
    }

    savePolicyType(event) {
        this.policiesService.saveDimension(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.policyTypes[this.policyTypes.length] = event.item;
                }
                else {
                    let index = this.policyTypes.findIndex(x => x.ID == event.item.ID);
                    if (index >= 0 && index < this.policyTypes.length)
                        this.policyTypes[index] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
                this.stateService.reloadLeftNavMenu();
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.policyTypes.length > 0 ? this.policyTypes[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

}