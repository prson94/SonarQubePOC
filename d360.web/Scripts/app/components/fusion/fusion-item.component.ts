import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { HeaderBreadcrumbService, FusionService, RightSidebarService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails  } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-item',
    template: ` Fusion Item
                `,
    providers: [FusionService],
})

export class FusionItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private fusionId: number;
    private fusion: FusionConfigurationDetails ;

    constructor(private headerBreadcrumbService: HeaderBreadcrumbService,
            private route: ActivatedRoute,
            private router: Router,
            private fusionService: FusionService,
            protected titleService: Title) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');

        this.sub = this.route.params.subscribe(params => {

            this.fusionId = +params['fusionId'];

            this.headerBreadcrumbService.setCurrentObjectInfo('Fusion', this.fusionId);

            this.fusionService.getFusionConfiguration(this.fusionId)
                .then(result => {
                    this.isLoading = false;
                    this.fusion = result;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion', '/a/fusion'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.fusion.Name));

                    this.setBrowserTitle(this.titleService, `Fusion - ${this.fusion.Name}`);

                });


        });
    }   

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
};