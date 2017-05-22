import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/fusion.service';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails, FusionAttributeType  } from '../../models/fusion.model';
import { FusionStructureTreeComponent} from './fusion-structure-tree.component';
import { FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-fusion-item',
    template: ` <d3s-loading [isLoading]="isLoading"></d3s-loading>                
                <div class="row" *ngIf="!isLoading && isHistoryVisible">
                    <div class="col s12">
                        <d3s-fusion-execution-history [fusion]="fusion"></d3s-fusion-execution-history>
                        <d3s-fusion-agent-history [fusion]="fusion"></d3s-fusion-agent-history>
                    </div>
                </div>      
                <div class="row" *ngIf="!isLoading && isManualLoadVisible">
                    <div class="col s12">
                        <d3s-fusion-manual-load [fusion]="fusion"></d3s-fusion-manual-load>
                    </div>
                </div>   
                <div class="row" *ngIf="!isLoading && showFusionRules">
                    <div class="col s12">
                        <d3s-fusion-rules [fusionID]="fusionId" [fusionTypeID]="fusion.FusionTypeID"></d3s-fusion-rules>
                    </div>
                </div>   
                <div class="row" *ngIf="!isLoading && !isHistoryVisible && !isManualLoadVisible && !showFusionRules">
                    <div class="col l2 m12 s12">
                        <div class="tile tile-detail">
                            <header>Structure</header>
                            <d3s-fusion-structure-tree [fusion]="fusion" [fusionAttributeTypeId]="selectedFusionAttributeTypeId" (fusionAttributeTypeIdChange)="changeFusionAttributeTypeId($event)"></d3s-fusion-structure-tree>
                        </div>
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

export class FusionAttributeItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        private fusionService: FusionService,
        private route: ActivatedRoute,
        private router: Router) {
        super();        
    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {
                        
            var fusionAttributeTypeId = +params['fusionAttributeTypeId'];
            var fusionAttributeId = +params['fusionAttributeId'];

            this.fusionService.getFusionConfigurationFromAttributeId(fusionAttributeId)
                .then(res => {
                    this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${res.ID};fusionAttributeTypeId=${fusionAttributeTypeId};fusionAttributeId=${fusionAttributeId}`);                    
                });

        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }    
};