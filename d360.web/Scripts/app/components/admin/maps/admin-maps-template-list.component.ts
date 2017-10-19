import { Component, NgZone, OnInit, OnChanges, Input, Output, SimpleChanges } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { MessagesService } from '../../../services/messages.service';
import { MapsService } from '../../../services/maps.service';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { FormMode } from '../../../models/form.model';
import { MapTypeTemplate, MapTypeTemplateItem } from '../../../models/map.model';

@Component({
    selector: 'd3s-admin-maps-template-list',
    providers: [MapsService],
    template: `
<div>
    <header>
        Map Type Templates <d3s-tile-actions [hasAdd]="!isLoading" (addClick)="add()"></d3s-tile-actions>
    </header>
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
        <p-dataTable #dt [globalFilter]="gb" [value]="mapTypeTemplates" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="edit($event.data.ID)" [(selection)]="selection" >                                                        
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>                                                        
            <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                               
            <p-column [style]="{width:'40px'}">
                <ng-template let-item="rowData" pTemplate type="body">
                    <div class="RowTools">
                        <a style="cursor:pointer;" (click)="selection = item; edit(item.ID);"><i class="fa fa-pencil"></i></a>                                        
                    </div>
                </ng-template>
            </p-column>                            
            <p-column  [style]="{width:'40px'}">
                <ng-template let-item="rowData" pTemplate type="body">
                    <div class="RowTools">                                
                        <a style="cursor:pointer;" (click)="selection = item; delete(item.ID);"><i class="fa fa-trash-o"></i></a>                                    
                    </div>
                </ng-template>
            </p-column>  
        </p-dataTable>      
    </div>
</div>
`
})

export class AdminMapsTemplateListComponent extends BaseComponent {
    @Input() mapTypeId: number = null;
    mapTypeTemplate: MapTypeTemplate;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    mapTypeTemplates: MapTypeTemplate[] = [];

    constructor(private mapsService: MapsService) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.mapTypeId == null)
            return;
        if (changes.mapTypeId.previousValue != changes.mapTypeId.currentValue || changes.mapTypeId.isFirstChange) {
            this.load();
        }
    }

    load() {
        if (this.mapTypeId == null)
            return;
        this.isLoading = true;
        this.mapsService.getMapTypeTemplates(this.mapTypeId)
            .then(r => {
                this.mapTypeTemplates = r;
                this.isLoading = false;
            });
    }
}


