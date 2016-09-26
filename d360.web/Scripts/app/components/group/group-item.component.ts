import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';


@Component({
    selector: 'd3s-group-item',

    template: ` 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" >    
                            <div class="row" *ngIf="!isLoading && !showDelete && !showEditor">                        
                                <div class="col s12">
                                    <header>{{modelGroup}} Groups                                
                                        <d3s-tile-actions [hasAdd]="false" ></d3s-tile-actions>                                                     
                                    </header>      
                                    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                                                                     
                                    <p-dataTable [globalFilter]="gb" [value]="rules" selectionMode="single" [rows]="20" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showRule();" >
                                        <p-column field="ID" header="ID" [sortable]="true" [style]="{width:'10%'}"></p-column>                                                                                                                        
                                        <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'45%'}"></p-column>                                                                                                                                                                
                                    </p-dataTable>      
                                </div>
                            </div>
                            
                        </div>                        
                    </div>
                </div>
                `
})

export class GroupItemComponent extends BaseComponent implements OnInit {

    constructor(private route: ActivatedRoute,
        private router: Router,
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();


    }

    ngOnInit() {
     /*   this.setBrowserTitle(this.titleService, 'Groups');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Groups'));

        this.load();*/
    }

    private load() {

    }

};