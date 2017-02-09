import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/fusion.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails, FusionAttributeType  } from '../../models/fusion.model';
import { FusionStructureTreeComponent} from './fusion-structure-tree.component';
import { FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-fusion-item',
    template: ` <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="fusion?.ID" [objectType]="'Fusion'" [title]="'Ownership of ' + fusion?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>  
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
                <d3s-dashboard-tab *ngIf="!isLoading && isDashboardVisible" [objectID]="fusion.FusionTypeID" [objectName]="fusion.Name" [objectType]="'FusionType'"></d3s-dashboard-tab>
                <div class="row" *ngIf="!isLoading && showFusionRules">
                    <div class="col s12">
                        <d3s-fusion-rules [fusionID]="fusionId" [fusionTypeID]="fusion.FusionTypeID"></d3s-fusion-rules>
                    </div>
                </div>   
                <div class="row" *ngIf="!isLoading && !isOwnershipVisible && !isHistoryVisible && !isManualLoadVisible && !showFusionRules && !isDashboardVisible">
                    <div class="col l3 m12 s12">
                        <div class="tile tile-detail">
                            <header>Structure</header>
                            <d3s-fusion-structure-tree #tree [fusion]="fusion" [showFusionQueryConfig]="isQueryConfigVisible" (showFusionQueryConfigChange)="showQueryConfig($event)" [fusionAttributeTypeId]="selectedFusionAttributeTypeId" (fusionAttributeTypeIdChange)="changeFusionAttributeTypeId($event)" [fusionQueryAttributeTypeId]="selectedFusionQueryAttributeTypeId" (fusionQueryAttributeTypeIdChange)="changeFusionQueryAttributeTypeId($event)"></d3s-fusion-structure-tree>
                        </div>
                    </div>
                    <div class="col l9 m12 s12" *ngIf="!isQueryConfigVisible">
                        <d3s-fusion-attribute-summary [initialFusionAttributeId]="initialFusionAttributeId" [initialFusionQueryAttributeId]="initialFusionQueryAttributeId" [fusionId]="fusionId" [fusionAttributeTypeId]="selectedFusionAttributeTypeId" [fusionQueryAttributeTypeId]="selectedFusionQueryAttributeTypeId" [fusionQueryAttribute]="selectedFusionQueryAttribute" [fusionAttribute]="selectedFusionAttribute" (fusionAttributeChange)="selectedFusionAttribute=$event;" (fusionQueryAttributeChange)="selectedFusionQueryAttribute=$event;"></d3s-fusion-attribute-summary>                        
                        <div class="tile tile-detail" *ngIf="selectedFusionAttribute">
                            <d3s-fusion-attribute-item-details [fusionAttributeId]="selectedFusionAttribute.ID" [name]="selectedFusionAttribute.Name" [objectType]="selectedFusionQueryAttributeTypeId ? 'FusionQueryAttribute':'FusionAttribute'"></d3s-fusion-attribute-item-details>
                        </div>
                        <div class="tile tile-detail" *ngIf="selectedFusionAttribute">
                            <d3s-object-relationships [objectPermissions]="permissions" [objectType]="'FusionAttribute'" [objectID]="selectedFusionAttribute?.ID" objectName=""></d3s-object-relationships>
                        </div>                        
                    </div>
                    <div class="col l9 m12 s12" *ngIf="isQueryConfigVisible">
                        <d3s-fusion-query-list [fusion]="fusion" (treeRequiresUpdate)="updateTree(tree)"></d3s-fusion-query-list>
                    </div>
                </div>
                `,
    providers: [FusionService, PermissionsService],
})

export class FusionItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;    
    private fusionId: number;
    private fusion: FusionConfigurationDetails;

    private selectedFusionAttributeTypeId: number;
    private selectedFusionAttribute: any;
    private initialFusionAttributeId: number;

    private selectedFusionQueryAttributeTypeId: number;
    private selectedFusionQueryAttribute: any;
    private initialFusionQueryAttributeId: number;


    private showFusionRules: boolean = false;
    private isHistoryVisible: boolean = false;
    private isManualLoadVisible: boolean = false;
    private isQueryConfigVisible: boolean = false;

    @ViewChild(FusionStructureTreeComponent) private fusionTreeComponent: FusionStructureTreeComponent;
    
    constructor(private headerBreadcrumbService: HeaderBreadcrumbService,
            private route: ActivatedRoute,
            private router: Router,
            private fusionService: FusionService,
            protected rightSidebarService: RightSidebarService,
            protected titleService: Title,
            protected permissionsService: PermissionsService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');
                
        this.sub = this.route.params.subscribe(params => {

            this.fusionId = +params['fusionId'];
            this.selectedFusionAttributeTypeId = +params['fusionAttributeTypeId'];
            this.initialFusionAttributeId = +params['fusionAttributeId'];
            this.selectedFusionQueryAttributeTypeId = +params['fusionQueryAttributeTypeId'];
            this.initialFusionQueryAttributeId = +params['fusionQueryAttributeId'];
            this.isQueryConfigVisible = params['showQueryConfig'] == 'true';         
            
            if (!this.fusion || this.fusion.ID != this.fusionId) {
                this.loadPermissions(this.permissionsService, StringConstants.ObjectFusion , this.fusionId);
                this.fusionService.getFusionConfiguration(this.fusionId)
                    .then(result => {
                        this.isLoading = false;
                        this.fusion = result;
                        
                        this.buildBreadcrumb();

                        this.setBrowserTitle(this.titleService, `Fusion - ${this.fusion.Name}`);

                        this.setRightSideBar(this.fusion.HasDashboards, this.fusion.Manual);
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

    private setRightSideBar(hasDashboard: boolean, isManual: boolean) {
        this.rightSidebarService.clearItems();
        this.setCommonRightSideBar(false, true, hasDashboard);

        this.rightSidebarService.showItem(new RightSidebarItem('History', 'fusionhistory', ['fa-archive']));
        this.rightSidebarService.showItem(new RightSidebarItem('Fusion Rules', 'fusionrules', ['fa-code-fork']));

        if (isManual) this.rightSidebarService.showItem(new RightSidebarItem('Load', 'fusionload', ['fa-file-excel-o']));           
    }
    
    private buildBreadcrumb() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion', SiteUrlHelpers.SITE_URL_FUSION_ROOT));
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.fusion.Name));

        if (this.selectedFusionAttributeTypeId && this.fusionTreeComponent.fusionAttributeTypes) {
            this.addFusionAttributeTypeBreadcrumb(this.selectedFusionAttributeTypeId);
        }
        else if (this.selectedFusionQueryAttributeTypeId && this.fusionTreeComponent.fusionQueryAttributeTypes) {
            this.addFusionQueryAttributeTypeBreadcrumb(this.selectedFusionQueryAttributeTypeId);
        }
        else if (this.isQueryConfigVisible) {
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion Query Configuration', `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};showQueryConfig=true`));            
        }
    }

    private addFusionAttributeTypeBreadcrumb(id: number) {
        var items = this.fusionTreeComponent.fusionAttributeTypes.filter(x => x.ID == id);

        if (items.length > 0) {
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(items[0].Name, `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionAttributeTypeId=${items[0].ID}`));            
        }
    }

    private addFusionQueryAttributeTypeBreadcrumb(id: number) {
        var items = this.fusionTreeComponent.fusionQueryAttributeTypes.filter(x => x.ID == id);

        if (items.length > 0) {
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(items[0].Name, `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionQueryAttributeTypeId=${items[0].ID}`));
        }
    }
    
    private changeFusionAttributeTypeId(event) {
        this.selectedFusionAttribute = null;
        this.selectedFusionQueryAttribute = null;
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionAttributeTypeId=${event}`);
    }   

    private showQueryConfig(val) {
        if(val) this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};showQueryConfig=true`);
    }

    private changeFusionQueryAttributeTypeId(event) {
        this.selectedFusionAttribute = null;
        this.selectedFusionQueryAttribute = null;
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionQueryAttributeTypeId=${event}`);
    }  

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {        
        if (activatedItem.tag == 'fusionhistory') this.isHistoryVisible = !this.isHistoryVisible;
        else if (activatedItem.tag == 'fusionload') this.isManualLoadVisible = !this.isManualLoadVisible;
        else if (activatedItem.tag == 'fusionrules') this.showFusionRules = !this.showFusionRules;  
    }

    protected updateTree(tree) {
        tree.load();
    }

};