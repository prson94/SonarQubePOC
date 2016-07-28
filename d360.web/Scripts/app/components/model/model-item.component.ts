///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ModelsService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model, ModelHierarchy } from '../../models/model.model';
import { ObjectDefinitionTile } from '../tiles/object-definition.tile';

@Component({
    selector: 'd3s-model-item',
    providers: [ModelsService],
    directives: [ObjectDefinitionTile],
    template: ` 
                <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail">
                                <d3s-object-definition-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID"></d3s-object-definition-tile>
                            </div>
                        </div>
                </div>
                `
})

export class ModelItemComponent extends BaseComponent implements OnInit {
    sub: any;
    model: Model;
    modelHierarchy: ModelHierarchy[] = [];
    selected: ModelHierarchy;

    constructor(private route: ActivatedRoute,
            private router: Router,
            protected modelsService: ModelsService,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
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
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Model'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.TaxonomyTypeClass));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name));

                    this.setBrowserTitle(this.titleService, this.model.Name);

                });           
            
        });

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Model'));
    }

    private loadModelHierarchy(modelId: number) {
        this.modelsService.getModelHierarchy(modelId)
            .then(result => {
                this.modelHierarchy = result;
                this.selected = (this.modelHierarchy.length && this.modelHierarchy.length > 0) ? this.modelHierarchy[0] : null;
            });
    }
};