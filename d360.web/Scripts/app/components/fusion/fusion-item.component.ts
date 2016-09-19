import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { HeaderBreadcrumbService, FusionService, RightSidebarService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails  } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-item',
    template: ` <div class="row" *ngIf="isLoading">
                    <div class="col s12">
                        <div>
                            <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                        </div>
                    </div>
                </div>
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="fusion?.ID" [objectType]="'Fusion'" [title]="'Ownership of ' + fusion?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>        
                <div class="row"*ngIf="!isLoading && !isOwnershipVisible">
                    <div class="col l2 m12 s12">
                        <d3s-fusion-structure-tree [fusion]="fusion" [fusionAttributeTypeId]="selectedFusionAttributeTypeId" (fusionAttributeTypeIdChange)="changeFusionAttributeTypeId($event)"></d3s-fusion-structure-tree>
                    </div>
                    <div class="col l10 m12 s12">
                        <d3s-fusion-attribute-summary [fusionId]="fusionId" [fusionAttributeTypeId]="selectedFusionAttributeTypeId" [fusionAttribute]="selectedFusionAttribute" (fusionAttributeChange)="selectedFusionAttribute=$event;"></d3s-fusion-attribute-summary>
                        <div class="tile tile-detail" *ngIf="selectedFusionAttribute">
                            <d3s-object-relationships [objectType]="'FusionAttribute'" [objectID]="selectedFusionAttribute?.ID" objectName=""></d3s-object-relationships>
                        </div>
                    </div>
                </div>
                `,
    providers: [FusionService],
})

export class FusionItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private fusionId: number;
    private fusion: FusionConfigurationDetails;
    private selectedFusionAttributeTypeId: number;
    private selectedFusionAttribute: any;

    constructor(private headerBreadcrumbService: HeaderBreadcrumbService,
            private route: ActivatedRoute,
            private router: Router,
            rightSidebarService: RightSidebarService,
            private fusionService: FusionService,
            protected titleService: Title) {
        super(rightSidebarService);
        this.setCommonRightSideBar(false, true);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');

        this.sub = this.route.params.subscribe(params => {

            this.fusionId = +params['fusionId'];
            this.selectedFusionAttributeTypeId = +params['fusionAttributeTypeId'];

            this.headerBreadcrumbService.setCurrentObjectInfo('Fusion', this.fusionId);

            if (!this.fusion || this.fusion.ID != this.fusionId) {
                this.fusionService.getFusionConfiguration(this.fusionId)
                    .then(result => {
                        this.isLoading = false;
                        this.fusion = result;

                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion', '/a/fusion'));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.fusion.Name));

                        this.setBrowserTitle(this.titleService, `Fusion - ${this.fusion.Name}`);

                    });
            }

        });
    }   

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }

    private changeFusionAttributeTypeId(event) {
        this.selectedFusionAttribute = null;
        this.router.navigateByUrl(`/a/fusion/${this.fusionId};fusionAttributeTypeId=${event}`);
    }    
};