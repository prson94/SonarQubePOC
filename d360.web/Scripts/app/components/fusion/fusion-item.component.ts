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
import { AuthenticationService } from '../../services/authentication.service';

@Component({
    selector: 'd3s-fusion-item',
    template: ` <d3s-loading [isLoading]="isLoading"></d3s-loading>                                                                          
                <div class="row" *ngIf="!isLoading">
                    <div class="col l3 m12 s12">
                        <div class="tile tile-detail">
                            <header>Structure</header>
                            <d3s-fusion-structure-tree #tree [fusion]="fusion" (loaded)="buildBreadcrumb()" [showFusionQueryConfig]="isQueryConfigVisible" (showFusionQueryConfigChange)="showQueryConfig($event)" [fusionAttributeTypeId]="selectedFusionAttributeTypeId" (fusionAttributeTypeIdChange)="changeFusionAttributeTypeId($event)" [fusionQueryAttributeTypeId]="selectedFusionQueryAttributeTypeId" (fusionQueryAttributeTypeIdChange)="changeFusionQueryAttributeTypeId($event)"></d3s-fusion-structure-tree>
                        </div>
                    </div>
                    <div class="col l9 m12 s12" *ngIf="!isQueryConfigVisible">
                        <d3s-fusion-attribute *ngIf = "fusionId !=0" [initialFusionAttributeId]="initialFusionAttributeId" [initialFusionQueryAttributeId]="initialFusionQueryAttributeId" [fusionId]="fusionId" [selectedFusionAttributeTypeId]="selectedFusionAttributeTypeId" [selectedFusionQueryAttributeTypeId]="selectedFusionQueryAttributeTypeId" [selectedFusionQueryAttribute]="selectedFusionQueryAttribute" [selectedFusionAttribute]="selectedFusionAttribute"></d3s-fusion-attribute>    
                        <d3s-fusion-attribute-tabs *ngIf = "fusionId ==0" [objectPermissions]="permissions" [initialFusionAttributeId]="initialFusionAttributeId" [initialFusionQueryAttributeId]="initialFusionQueryAttributeId" [fusionId]="fusionId" [selectedFusionAttributeTypeId]="selectedFusionAttributeTypeId" [selectedFusionQueryAttributeTypeId]="selectedFusionQueryAttributeTypeId" [selectedFusionQueryAttribute]="selectedFusionQueryAttribute" [selectedFusionAttribute]="selectedFusionAttribute"></d3s-fusion-attribute-tabs>                        
                    </div>
                    <div class="col l9 m12 s12" *ngIf="isQueryConfigVisible">
                        <d3s-fusion-query-list [fusion]="fusion" (treeRequiresUpdate)="updateTree(tree)"></d3s-fusion-query-list>
                    </div>
                </div>
                `,
    providers: [FusionService, PermissionsService],
})
//<d3s-fusion-attribute-item-details [fusionAttributeId]="selectedFusionAttribute.ID" [name]="selectedFusionAttribute.Name" [objectType]="selectedFusionQueryAttributeTypeId ? 'FusionQueryAttribute':'FusionAttribute'"></d3s-fusion-attribute-item-details>

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

    private isQueryConfigVisible: boolean = false;

    @ViewChild(FusionStructureTreeComponent) private fusionTreeComponent: FusionStructureTreeComponent;
    
    constructor(private headerBreadcrumbService: HeaderBreadcrumbService,
            private route: ActivatedRoute,
            private router: Router,
            private fusionService: FusionService,
            protected rightSidebarService: RightSidebarService,
            protected titleService: Title,
            protected permissionsService: PermissionsService,
            private authenticationService: AuthenticationService
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
                        this.setObjectInfo('Fusion', this.fusionId, undefined, this.fusion.AssetID);
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

        this.rightSidebarService.showItem(new RightSidebarItem('History', 'fusionhistory', ['fa-archive'], `/fusion/history/${this.fusionId}`));
        if (this.authenticationService.isAdmin) this.rightSidebarService.showItem(new RightSidebarItem('Fusion Rules', 'fusionrules', ['fa-code-fork'], `/fusion/rules/${this.fusionId}/${this.fusion.FusionTypeID}`));

        if (isManual) this.rightSidebarService.showItem(new RightSidebarItem('Load', 'fusionload', ['fa-file-excel-o'], `/fusion/manual/load/${this.fusionId}`));           
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
        if (event == this.selectedFusionAttributeTypeId) {
            //console.log('current type is same as selected');
            return;
        }
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
    
    protected updateTree(tree) {
        tree.load();
    }

};