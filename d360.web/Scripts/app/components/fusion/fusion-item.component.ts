import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { HeaderBreadcrumbService, FusionService, RightSidebarService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails, FusionAttributeType  } from '../../models/fusion.model';
import { FusionStructureTreeComponent} from './fusion-structure-tree.component';
import { FusionAttributeFilter } from '../../models/fusion-attribute.model';

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
                        <d3s-fusion-attribute-summary [initialFusionAttributeId]="initialFusionAttributeId" [fusionId]="fusionId" [fusionAttributeTypeId]="selectedFusionAttributeTypeId" [fusionAttribute]="selectedFusionAttribute" (fusionAttributeChange)="selectedFusionAttribute=$event;"></d3s-fusion-attribute-summary>
                        <div class="tile tile-detail" *ngIf="selectedFusionAttribute">                            
                            <d3s-fusion-attribute-item-details [fusionAttributeId]="selectedFusionAttribute.ID" [name]="selectedFusionAttribute.Name"></d3s-fusion-attribute-item-details>
                        </div>
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
    private initialFusionAttributeId: number;

    @ViewChild(FusionStructureTreeComponent) private fusionTreeComponent: FusionStructureTreeComponent;
    

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
            this.initialFusionAttributeId = +params['fusionAttributeId'];
            

            if (!this.fusion || this.fusion.ID != this.fusionId) {
                this.fusionService.getFusionConfiguration(this.fusionId)
                    .then(result => {
                        this.isLoading = false;
                        this.fusion = result;
                        
                        this.buildBreadcrumb();

                        this.setBrowserTitle(this.titleService, `Fusion - ${this.fusion.Name}`);

                        this.headerBreadcrumbService.setCurrentObjectInfo('Fusion', this.fusionId);

                    });
            }
            else {
                this.buildBreadcrumb();
            }

        });
    }   

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.clearSidebar();
    }

    private loadFusionTypeStructure() {
        
    }
    

    private buildBreadcrumb() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion', '/a/fusion'));
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.fusion.Name));

        if (this.selectedFusionAttributeTypeId && this.fusionTreeComponent.fusionAttributeTypes) {
            this.addFusionAttributeTypeBreadcrumb(this.selectedFusionAttributeTypeId);
        }

    }

    private addFusionAttributeTypeBreadcrumb(id: number) {
        var items = this.fusionTreeComponent.fusionAttributeTypes.filter(x => x.ID == id);

        if (items.length > 0) {
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(items[0].Name, `/a/fusion/${this.fusionId};fusionAttributeTypeId=${items[0].ID}`));

      //      if (items[0].ParentID)
        //        this.addFusionAttributeTypeBreadcrumb(items[0].ParentID);
        }

        
    }


    private changeFusionAttributeTypeId(event) {
        this.selectedFusionAttribute = null;
        this.router.navigateByUrl(`/a/fusion/${this.fusionId};fusionAttributeTypeId=${event}`);
    }    
};