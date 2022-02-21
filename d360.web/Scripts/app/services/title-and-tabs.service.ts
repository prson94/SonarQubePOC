import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { Observable, of, Subject, Subscription } from 'rxjs';
import { switchMap, takeUntil, tap } from 'rxjs/operators';
import { AssetGridBaseComponent } from '../components/assets-grid/asset-grid-base.component';
import { AssetGridObject } from '../components/assets-grid/asset-grid.model';
import { ArtifactType } from '../models/artifact-type.model';
import { AssetTypeClass } from '../models/asset.model';
import { Breadcrumb } from '../models/breadcrumb.model';
import { SecondaryNavCurrentObject, SecondaryNavItem } from '../models/secondaryNav.model';
import { SiteUrlHelpers } from '../static/site-url-helpers';
import { ArtifactTypeService } from './artifact-type.service';
import { HeaderBreadcrumbService } from './header-breadcrumb.service';
import { SecondaryNavService } from './right-sidebar.service';
import { CompanySettingsService } from './settings.service';
import { WebAnalyticsService } from './web-analytics.service';

declare var CurrentResourceID;

@Injectable({
  providedIn: 'root'
})
export class TitleAndTabsService extends AssetGridBaseComponent {
  isInitialize: boolean = false;
  sub: any;
  artifactTypeHierarchy: ArtifactType[];
  sidePanelStorageKey: string;
  artifactType: ArtifactType;
  gridObject: AssetGridObject;
  navigationItemsSubs: Subscription[] = [];
  currentAreaNameSubscription: any;
  currentAreaName: string;
  artifactTypeId: number;

  constructor(
    private http: HttpClient,
    private router: Router,
    private route: ActivatedRoute,
    private artifactTypeService: ArtifactTypeService,
    private titleService: Title,
    headerBreadcrumbService: HeaderBreadcrumbService,
    settingsService: CompanySettingsService,
    secondaryNavService: SecondaryNavService,
    webAnalyticsService: WebAnalyticsService,
  ) {
    super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);
    console.log('this.isInitialize');
    console.log(this.isInitialize);
  }

  initializeTitleAndTabsInRightSidebar(routeParams: Observable<Params>) {
    // debugger;
    this.sub = routeParams.subscribe(params => {
      // debugger;
      this.getArtifactTypeIdFromRouteParams(params)

      this.isLoading = true;
      this.artifactTypeHierarchy = [];
      this.headerBreadcrumbService.setCurrentObjectInfo('ArtifactType', this.artifactTypeId);
      this.logAction('open', 'ArtifactType', this.artifactTypeId);
      this
        .artifactTypeService
        .getArtifactTypeDetails(this.artifactTypeId, true)
        .subscribe((artifactType) => {
          let folderName: string = '#Business';
          this.areaLink = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`;

          if (artifactType.Class == AssetTypeClass.TechnicalAsset) {
            folderName = '#Technical';
            this.areaLink = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`;
          }

          this.sidePanelStorageKey = 'list_' + AssetTypeClass[artifactType.Class] + '_' + CurrentResourceID;

          this.headerBreadcrumbService.getFolderTitle(folderName).then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();

            this.folderTitle = res;
            this.area = res;

            this.artifactType = artifactType;
            this.gridObject = ArtifactType.AsGridObject(this.artifactType);
            this.setObjectInfo('ArtifactType', this.artifactType.ID);

            this.artifactTypeHierarchy.push(this.artifactType);
            this.createBreadcrumbHierarchy(artifactType);

            this.setBrowserTitle(this.titleService, this.artifactType.Name);
            this.isLoading = false;
          });
        });
    });
  }

  getArtifactTypeIdFromRouteParams(params: Params) {
    if (params.artifactTypeId) {
      this.artifactTypeId = Number(params.artifactTypeId);
    } else if (params.objectId) {
      this.artifactTypeId = Number(params.objectId);
    }
    return this.artifactTypeId;
  }

  createBreadcrumbHierarchy(artifact: ArtifactType) {
    if (artifact.ParentID) {
      var detailsSub = this.artifactTypeService.getArtifactTypeDetails(artifact.ParentID).subscribe(parent => {
        this.artifactTypeHierarchy.unshift(parent);
        if (parent.ParentID)
          this.createBreadcrumbHierarchy(parent);
        else
          this.displayBreadcrumb();
      });

      this.navigationItemsSubs.push(detailsSub);
    } else
      this.displayBreadcrumb();
  }

  displayBreadcrumb() {
    this.headerBreadcrumbService.clearBreadcrumbs();
    this.currentAreaNameSubscription =
      this.headerBreadcrumbService
        .getAreaName('ArtifactType', this.artifactTypeHierarchy[0].ID)
        .subscribe(result => {
          this.currentAreaName = result
          this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : this.folderTitle, this.areaLink));
          this.artifactTypeHierarchy.forEach(x => {
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(
              x.Name,
              SiteUrlHelpers.getObjectUrl("ArtifactType", x.ID),
              false,
              "ArtifactType",
              x.ID,
              null,
              null,
              true,
              x.ParentID > 0));

          });

          var breadCrumbsSub = this.headerBreadcrumbService.getAssetFolderIcon('ArtifactType', this.artifactType.ID, this.currentAreaName ? this.currentAreaName : this.folderTitle).subscribe(res => {
            this.setCommonSecondaryNavTabs({ hasAudit: false, hasOwnership: false, hasDashboard: this.artifactType.HasDashboards });
            this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject('ArtifactType', this.artifactType.ID, this.artifactType.Name, null, true, null, this.artifactType.AssetTypeUID));
            this.secondaryNavService.setCurrentArea(this.artifactType.Name, res, 'Assets');
            if (this.artifactType.HasV2Workflows) {
              this.secondaryNavService
                .showItem(
                  new SecondaryNavItem(
                    'Workflow',
                    'workflowmonitor',
                    ['fa-usb'],
                    `/sidebar/workflowmonitor${this.objectContextUrl()};isAdminPage=false`));
            }
          });
          this.navigationItemsSubs.push(breadCrumbsSub);
        });
  }

}
