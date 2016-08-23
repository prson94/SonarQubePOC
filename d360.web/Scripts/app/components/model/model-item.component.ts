///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ModelsService, RightSidebarService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model, ModelHierarchy } from '../../models/model.model';

@Component({
    selector: 'd3s-model-item',
    providers: [ModelsService],
    template: ` <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Taxonomy'"></d3s-audit>
                <d3s-ownership-tab *ngIf="!isLoading && isOwnershipVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Taxonomy'"></d3s-ownership-tab>
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible" class="row">
                    <div class="col s12">
                        <div class="row">
                            <div class="col s12">
                                 <div class="tile tile-detail">
                                    <d3s-object-governance-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID"></d3s-object-governance-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-object-definition-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID"></d3s-object-definition-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-object-relationships-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID"></d3s-object-relationships-tile>
                                </div>
                            </div>
                        </div>
                    </div>                   
                </div>
                `
})

export class ModelItemComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
    model: Model;
    modelHierarchy: ModelHierarchy[] = [];
    selected: ModelHierarchy;

    constructor(private route: ActivatedRoute,
            private router: Router,
            rightSidebarService: RightSidebarService,
            protected modelsService: ModelsService,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super(rightSidebarService);

        this.setCommonRightSideBar(true, true);
    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {
            let modelId = +params['modelId'];

            
            this.isLoading = true;
            this.loadModelHierarchy(modelId);
            this.modelsService.getModel(modelId)
                .then(result => {
                    this.isLoading = false;
                    this.model = result;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Information Models'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name, undefined, true, 'TaxonomyType', this.model.ID));

                    this.setBrowserTitle(this.titleService, this.model.Name);

                });           
            
        });

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Model'));
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private loadModelHierarchy(modelId: number) {
        this.modelsService.getModelHierarchy(modelId)
            .then(result => {
                this.modelHierarchy = result;
                this.selected = (this.modelHierarchy.length && this.modelHierarchy.length > 0) ? this.modelHierarchy[0] : null;
            });
    }
};