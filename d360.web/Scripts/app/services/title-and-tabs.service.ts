import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Params } from '@angular/router';
import { Observable, Subject } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import { AssetGridBaseComponent } from '../components/assets-grid/asset-grid-base.component';
import { AssetGridObject } from '../components/assets-grid/asset-grid.model';
import { ObjectType } from '../enums/app.enum';
import { Param } from '../enums/param.enum';
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
  artifactTypeBreadcrumbElements: ArtifactType[];
  sidePanelStorageKey: string;
  artifactType: ArtifactType;
  gridObject: AssetGridObject;
  artifactTypeId: number;
  destroy = new Subject<void>();

  constructor(
    private artifactTypeService: ArtifactTypeService,
    private titleService: Title,
    headerBreadcrumbService: HeaderBreadcrumbService,
    settingsService: CompanySettingsService,
    secondaryNavService: SecondaryNavService,
    webAnalyticsService: WebAnalyticsService,
  ) {
    super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);
  }

  initializeTitleAndTabsCheck(routeParams: Observable<Params>, params: Params, activeTabTitle?: string): void {
    if (!this.isInitialize && params[Param.ObjectType] === ObjectType.ArtifactType) {
      this.initializeTitleAndTabsInRightSidebar(routeParams, activeTabTitle);
    }
  }

  initializeTitleAndTabsInRightSidebar(routeParams: Observable<Params>, activeTabTitle?: string): void {
    this.secondaryNavService.activeTabTitle = activeTabTitle;
    routeParams.pipe(
      takeUntil(this.destroy),
      switchMap((params: Params): Observable<ArtifactType> => {
        this.artifactTypeId = this.secondaryNavService.getArtifactTypeIdFromRouteParams(params);
        this.secondaryNavService.artifactTypeId = this.artifactTypeId;
        this.isLoading = true;
        this.artifactTypeBreadcrumbElements = [];
        this.headerBreadcrumbService.setCurrentObjectInfo('ArtifactType', this.artifactTypeId);
        this.logAction('open', 'ArtifactType', this.artifactTypeId);
        return this.artifactTypeService.getArtifactTypeDetails(this.artifactTypeId, true);
      }),
      switchMap((artifactType: ArtifactType): Promise<string> => {
        this.artifactType = artifactType;
        let folderName: string = '#Business';
        this.areaLink = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`;
        if (artifactType.Class === AssetTypeClass.TechnicalAsset) {
          folderName = '#Technical';
          this.areaLink = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`;
        }
        this.sidePanelStorageKey = 'list_' + AssetTypeClass[artifactType.Class] + '_' + CurrentResourceID;
        return this.headerBreadcrumbService.getFolderTitle(folderName);
      }),
    ).subscribe((folderTitle: string) => {
      this.headerBreadcrumbService.clearBreadcrumbs();
      this.folderTitle = folderTitle;
      this.area = folderTitle;
      this.gridObject = ArtifactType.AsGridObject(this.artifactType);
      this.setObjectInfo('ArtifactType', this.artifactType.ID);
      this.artifactTypeBreadcrumbElements.push(this.artifactType);
      this.createBreadcrumbHierarchy(this.artifactType);
      this.setBrowserTitle(this.titleService, this.artifactType.Name);
      this.isLoading = false;
      this.isInitialize = true;
    });
  }

  createBreadcrumbHierarchy(artifact: ArtifactType) {
    if (artifact.ParentID) {
      this.artifactTypeService.getArtifactTypeDetails(artifact.ParentID).pipe(
        takeUntil(this.destroy)
      ).subscribe((parent: ArtifactType) => {
        this.artifactTypeBreadcrumbElements.unshift(parent);
        if (parent.ParentID) {
          this.createBreadcrumbHierarchy(parent);
        } else {
          this.displayBreadcrumb();
        }
      });
    } else {
      this.displayBreadcrumb();
    }
  }

  displayBreadcrumb() {
    this.headerBreadcrumbService.clearBreadcrumbs();
    this.headerBreadcrumbService.getAreaName('ArtifactType', this.artifactTypeBreadcrumbElements[0].ID).pipe(
      takeUntil(this.destroy),
      switchMap((areaName: string): Observable<string> => {
        this.fillBreadcrumbWithElements(areaName);
        return this.headerBreadcrumbService.getAssetFolderIcon('ArtifactType', this.artifactType.ID, areaName ? areaName : this.folderTitle);
      }),
    ).subscribe((iconName: string) => {
      this.setCommonSecondaryNavTabs({ hasAudit: false, hasOwnership: false, hasDashboard: this.artifactType.HasDashboards });
      this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject('ArtifactType', this.artifactType.ID, this.artifactType.Name, null, true, null, this.artifactType.AssetTypeUID));
      this.secondaryNavService.setCurrentArea(this.artifactType.Name, iconName, 'Assets');
      if (this.artifactType.HasV2Workflows) {
        this.secondaryNavService.showItem(
          new SecondaryNavItem(
            'Workflow',
            'workflowmonitor',
            ['fa-usb'],
            `/sidebar/workflowmonitor${this.objectContextUrl()};isAdminPage=false`
          )
        );
      }
    });
  }

  fillBreadcrumbWithElements(areaName: string): void {
    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(areaName ? areaName : this.folderTitle, this.areaLink));
    this.artifactTypeBreadcrumbElements.forEach((artifactTypeBreadcrumbElement: ArtifactType) => {
      this.headerBreadcrumbService.showBreadcrumb(
        new Breadcrumb(
          artifactTypeBreadcrumbElement.Name,
          SiteUrlHelpers.getObjectUrl("ArtifactType", artifactTypeBreadcrumbElement.ID),
          false,
          "ArtifactType",
          artifactTypeBreadcrumbElement.ID,
          null,
          null,
          true,
          artifactTypeBreadcrumbElement.ParentID > 0
        )
      );
    });
  }

  ngOnDestroy() {
    this.destroy.next();
    this.destroy.complete();
  }
}
