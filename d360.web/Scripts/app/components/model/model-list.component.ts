///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ModelsService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model } from '../../models/model.model';

@Component({
    selector: 'd3s-model-list',
    providers: [ModelsService],    
    template: `                 
                <div class="row">
                    <div class="col s12">
                        <div *ngIf="isLoading">
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                        <div class="tile tile-detail" *ngIf="!isLoading">                            
                            <header>{{modelGroup}} Models
                                <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                            </header>                              
                            <p-dataTable #dt [value]="models | modelType: modelGroup" scrollable="true" scrollWidth="100%" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showModel();" >
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true" [style]="{width:'200px'}"></p-column>                                                                                                                        
                                <p-column field="TaxonomyTypeClass" [hidden]="modelGroup" header="Classification" [sortable]="true" [filter]="true" [style]="{width:'200px'}"></p-column>
                                <p-column field="Description" header="Description" [sortable]="true" [filter]="true" [style]="{width:'500px'}">
                                    <template let-col let-data="rowData" pTemplate type="body">
                                        <div [innerHtml]="data?.Description"></div>
                                    </template>                                                        
                                </p-column>
                                <p-column field="MaximumDepth" header="Max Depth" [sortable]="true" [filter]="true" [style]="{width:'100px'}"></p-column>                                
                            </p-dataTable>      
                        </div>
                    </div>
                </div>
                `
})

export class ModelListComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private modelGroup: string;
    private models: Model[] = [];
    private selected: Model;
    
    constructor(
                private route: ActivatedRoute,
                private router: Router,
                protected titleService: Title,
                protected headerBreadcrumbService: HeaderBreadcrumbService,
                protected modelsService: ModelsService) {
        super();
        
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.modelGroup = params['group'];

                                    
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Models', this.modelGroup ? '/a/model/classification' : undefined));

            if (this.modelGroup) {
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.modelGroup));
            }

            this.setBrowserTitle(this.titleService, `${this.modelGroup ? this.modelGroup + ' ' : ''}Models`);

            this.loadModels();
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();        
    }

    loadModels() {
        this.isLoading = true;
        this.modelsService.getModels()
            .then(result => {
                this.isLoading = false;
                this.models = result;                
                if (this.models.length && this.models.length > 0) this.selected = this.models[0];
            });
    }

    showModel() {
        this.router.navigateByUrl(`/a/model/${this.selected.ID}`)
    }

};