import { Component, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { RightSidebarService, HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ReferenceItemType } from '../../models/reference.model';

@Component({
    selector: 'd3s-reference-list',   
   
    template: ` 
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="selectedReferenceItemType?.ID" [objectName]="selectedReferenceItemType?.Name" [objectType]="'ReferenceItemType'"></d3s-audit>                
                <d3s-lineage *ngIf="!isLoading && isLineageVisible" [objectID]="selectedReferenceItemType?.ID" [objectName]="selectedReferenceItemType?.Name" [objectType]="'ReferenceItemType'"></d3s-lineage>
                <d3s-impact *ngIf="!isLoading && isImpactVisible" [objectID]="selectedReferenceItemType?.ID" [objectName]="selectedReferenceItemType?.Name" [objectType]="'ReferenceItemType'"></d3s-impact>
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="selectedReferenceItemType?.ID" [objectType]="'ReferenceItemType'" [title]="'Ownership of ' + selectedReferenceItemType?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading && isRelationshipsVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-relationships [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID" [objectName]="selectedReferenceItemType?.Name"></d3s-object-relationships>
                        </div>
                    </div>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible && !isRelationshipsVisible && !isLineageVisible && !isImpactVisible">                                      
                    <div class="col s12 l3">
                        <d3s-reference-item-type-list [(selected)]="selectedReferenceItemType"></d3s-reference-item-type-list>
                    </div>
                    <div class="col s12 l9" *ngIf="selectedReferenceItemType">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <object-detail [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID"></object-detail>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID" ></d3s-field-definition-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">           
                                    <d3s-dynamic-grid [title]="'Items'" [itemName]="'Lookup'" [objectType]="'ReferenceItemType'" [objectID]="selectedReferenceItemType?.ID" [createUri]="'form/dynamicedit/create/referenceitem/'" [editUri]="'form/dynamicedit/edit/referenceitem/'" [dataUri]="referenceItemUri()" [deleteUri]="'form/dynamicedit/delete/referenceitem/'"></d3s-dynamic-grid>                                                                       
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
               `
})

export class ReferenceListComponent extends BaseComponent implements OnInit, OnDestroy {    

    private selectedReferenceItemType: ReferenceItemType;

    constructor(rightSidebarService: RightSidebarService, protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super(rightSidebarService);

        this.setCommonRightSideBar(true, true, false, true, true, true);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Reference');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Reference'));
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    referenceItemUri() {
        if (this.selectedReferenceItemType == null) return "";

        return `resources/referenceItems/${this.selectedReferenceItemType.ID}/items.json`;
    }    
};