///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ModelsService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model } from '../../models/model.model';

@Component({
    selector: 'd3s-model-item',
    providers: [ModelsService],
    template: ` Model Item
                `
})

export class ModelItemComponent extends BaseComponent implements OnInit {
    sub: any;
    model: Model;

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
};