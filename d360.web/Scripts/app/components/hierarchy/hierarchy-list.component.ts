import { Component, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetTypeApiModel, AssetTypeClass } from '../../models/asset.model';
import { AssetTypeService } from '../../services/asset-type.service';
import { ActivatedRoute, Router } from '@angular/router';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';

import * as _ from 'lodash';
import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CompanySettingsService } from '../../services/settings.service';
import { NumberOfRowsByCategoryService } from '../../services/number-of-rows-by-category.service';
import { takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';



@Component({
    selector: 'd3s-hierarchy-list',
    providers: [AssetTypeService],
    templateUrl: 'hierarchy-list.component.html' 
})

export class HierarchyListComponent extends BaseComponent implements OnInit {
    public rowsPerPage: number;
    private types: AssetTypeApiModel[] = [];
    private selected: AssetTypeApiModel;
    private type: string;

    private assetTypeClass: AssetTypeClass;
    private navFolderName: string;
    private destroy = new Subject<void>();
    

    constructor(
        public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
        private assetTypeService: AssetTypeService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        private route: ActivatedRoute,
        private router: Router) {

        super(settingsService);

        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {

        this.type = this.route.parent.snapshot.data.type;

        switch (this.type) {
            case SiteUrlHelpers.SITE_URL_MODEL_ROOT:
                this.assetTypeClass = AssetTypeClass.Model;
                this.objectType = StringConstants.ObjectTaxonomyType;
                this.objectName = 'Models';
                this.navFolderName = '#Models';
                break;
            case SiteUrlHelpers.SITE_URL_POLICY_ROOT:
                this.assetTypeClass = AssetTypeClass.Policy;
                this.objectType = StringConstants.ObjectPolicyType;
                this.objectName = 'Policies';
                this.navFolderName = '#Policy';
                break;
        }

        this.setObjectInfo(this.objectType, -1);
        this.setCommonSecondaryNavTabs({ hasAudit: false });

        this.load();

        this.setBrowserTitle(this.titleService, this.objectName);

        this.setRowsPerPage();
        this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);
    }

    setRowsPerPage(): void {
        this.numberOfRowsByCategoryService.rowsPerPage.pipe(
            takeUntil(this.destroy)
        ).subscribe((rowsPerPage) => {
            this.rowsPerPage = rowsPerPage as number;
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.destroy.next();
        this.destroy.complete();
    }

    load() {
        this.isLoading = true;
        this.assetTypeService.getAssetTypesByClass(this.assetTypeClass).subscribe(
            result => {
                this.isLoading = false;

                this.types = result;
                this.types = _.sortBy(this.types, 'Name');

                if (this.types.length && this.types.length > 0) {
                    this.selected = this.types[0];
                }

                this.headerBreadcrumbService.getFolderTitle(this.navFolderName).then(res => {
                    this.headerBreadcrumbService.clearCurrentObjectInfo();
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res, undefined));

                    this.headerBreadcrumbService.getFolderIcon(res).subscribe(icon => {
                        this.secondaryNavService.setCurrentArea(res, icon, this.objectName);
                    });

                    this.secondaryNavService.showHeader(true);
                });
            }
        );
    }

    showAsset(asset: AssetTypeApiModel) {
        this.router.navigateByUrl(`${this.type}/structure/${asset.uid}`);
    }
}