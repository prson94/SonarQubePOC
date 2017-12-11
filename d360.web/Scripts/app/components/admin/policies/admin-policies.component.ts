import { Component, OnInit, OnDestroy} from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { PoliciesService } from '../../../services/policies.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { PolicyType } from '../../../models/policy.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { AssetTypeService } from "../../../services/asset-type.services";
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-policies-component',
    providers: [PoliciesService, AssetTypeService],
    template: ` <div class="tile tile-detail" *ngIf="showEditor || showDelete">
                    <d3s-asset-type-editor-form  *ngIf="showEditor" [assetTypeClass]="'P'" [id]="selected?.AssetTypeID" [title]="'Edit Policy Type'" (onCancel)="closeEditor()" (onComplete)="savePolicyType($event)"></d3s-asset-type-editor-form>
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.AssetTypeID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the policy type [' + [selected?.Name] + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form>
                </div>
                <div class="row" *ngIf="!showEditor && !showDelete">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header>Policy Types
                                <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showEditor && !showDelete">
                                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="policyTypes" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="Name" header="Name" sortable="true"  [filter]="!showSimpleFilter"></p-column>     
                                    <p-column field="MaximumDepth" header="Max Depth" sortable="true"  [filter]="!showSimpleFilter" [style]="{width:'100px'}"></p-column>                                                        
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-policy="rowData"  pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selected=policy;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </ng-template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <ng-template let-policy="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selected=policy;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </ng-template>
                                    </p-column>    
                                </p-dataTable>      
                            </span>    
                        </div>
                    </div>               
                    <div class="col l8 s12" *ngIf="selected">
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
                                    <d3s-admin-level-grid objectType="PolicyType" [maxDepth]="selected?.MaximumDepth" [objectId]="selected?.ID"></d3s-admin-level-grid>
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
                                    <d3s-admin-allocation [objectType]="'PolicyType'" [objectID]="selected?.ID"></d3s-admin-allocation>
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

    protected assetTypeService: AssetTypeService = null;

    constructor(
        private stateService: StateService,
        rightSidebarService: RightSidebarService,
        private policiesService: PoliciesService,
        protected messagesService: MessagesService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        assetTypeService: AssetTypeService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        

        this.assetTypeService = assetTypeService;

        this.areaName = "Policy Types";
        this.setCommonItems();
        this.theDeleteCallback = this.deletePolicyType.bind(this);
        this.setCommonRightSideBar(true);

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/PolicyType/${this.selected.ID}`
            });
        }
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
        
    deletePolicyType(id: number) {
        this.assetTypeService.deleteAssetType(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;                
                if (result.type != 'error') {                    
                    this.policyTypes = this.policyTypes.filter(x => x.AssetTypeID != id);                    
                    this.selected = this.policyTypes.length > 0 ? this.policyTypes[0] : null;
                }
                this.stateService.reloadLeftNavMenu();
            });
    }

    savePolicyType(event) {
        this.showEditor = false;
        this.getPolicyTypes();
        this.stateService.reloadLeftNavMenu();
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