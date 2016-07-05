///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { MessagesService, HeaderBreadcrumbService, PageHeader  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';


@Component({
    selector: 'd3s-admin-dashboards-component',
    directives: [DataTable, Column, TileActionsComponent],   
    template: `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Dashboards</header>  
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                          
                            <p-dataTable *ngIf="!isLoading && !showEditor" [value]="analytics" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >                                                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                        
                            </p-dataTable>                                
                        </div>
                    </div>                                        
                </div>  
                `
})

export class AdminDashboardsComponent extends AdminBaseComponent {    
    showEditor: boolean = false;

    constructor(protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, pageHeader: PageHeader) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Manage your dashboard overlays and tiles.";
        this.areaName = "Dashboards";
        this.setCommonItems();
    }

    ngOnInit() {
                
    }

}