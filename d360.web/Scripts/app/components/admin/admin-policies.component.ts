///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader, PoliciesService  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { ClaimsTile } from '../tiles/claims.tile';
import { PolicyType } from '../../models/policy.model';


@Component({
    selector: 'd3s-admin-policies-component',
    directives: [DataTable, Column, TileActionsComponent, PeopleResponsibilitiesTile, ClaimsTile],
    providers: [PoliciesService],
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Policy Types
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Rule'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>  
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                          
                            <p-dataTable *ngIf="!isLoading && !showEditor" [value]="policyTypes" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >                                                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                        
                            </p-dataTable>                               
                        </div>
                    </div>                    
                    <div class="col l8 s12">
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

export class AdminPoliciesComponent extends AdminBaseComponent {
    policyTypes: PolicyType[] = [];
    selected: PolicyType;
    showEditor: boolean = false;

    constructor(private policiesService: PoliciesService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Organize various sets of policies across your organization.";
        this.areaName = "Policy Types";
        this.setCommonItems();
    }

    ngOnInit() {

        this.getPolicyTypes();
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
}