///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, OnDestroy} from '@angular/core';
import { MessagesService, HeaderBreadcrumbService, PageHeader, PoliciesService, RightSidebarService  } from '../../services/index';
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
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <p-dataTable *ngIf="!isLoading && !showEditor && !showDelete" [value]="policyTypes" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                        
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
                    <div class="col l8 s12">
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

    constructor(rightSidebarService: RightSidebarService, private policiesService: PoliciesService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = "Organize various sets of policies across your organization.";
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
        this.policiesService.getPolicyTypes()
            .then(result => {
                this.policyTypes = result;
                this.isLoading = false;
                if (this.policyTypes.length > 0) this.selected = this.policyTypes[0];
            });
    }

    findPolicyTypeIndex(id: number) {
        var index: number = -1;
        for (var policyType of this.policyTypes) {
            index++;
            if (policyType.ID == id) return index;
        }
    }

    deletePolicyType(id: number) {
        this.policiesService.deletePolicy(id);
        this.showDelete = false;
        this.selected = this.policyTypes.length > 0 ? this.policyTypes[0] : null;
        this.policyTypes.splice(this.findPolicyTypeIndex(id), 1);
    }

    savePolicyType(event) {
        this.policiesService.saveDimension(event.item)
            .then(result => {
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.policyTypes[this.policyTypes.length] = event.item;
                }
                else {
                    this.policyTypes[this.findPolicyTypeIndex(event.item.ID)] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
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