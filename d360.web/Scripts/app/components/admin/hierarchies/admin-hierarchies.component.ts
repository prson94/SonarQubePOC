import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { AssetTypeService } from '../../../services/asset-type.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { forEach } from 'core-js/fn/array';
import { StringConstants } from '../../../static/string-constants';

@Component({
    selector: 'd3s-admin-models-component',
    providers: [AssetTypeService],
    templateUrl: './admin-hierarchies.component.html'
})

export class AdminHierarchiesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    types: any[] = [];
    error: any;
    selected: any = null;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    assetTypeClass: AssetTypeClass;
    AssetTypeClass = AssetTypeClass;
    selectedItemID: number;
    selectedAssetTypeID: number;

    constructor(
        private activatedRoute: ActivatedRoute,
        private stateService: StateService,
        protected assetTypeService: AssetTypeService,
        secondaryNavService: SecondaryNavService,
        private messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title
    ) {

        super(headerBreadcrumbService, titleService, secondaryNavService);

        this.activatedRoute.parent.url.subscribe((urlPath) => {
            const url = urlPath[urlPath.length - 1].path;

            if (!url) {
                console.error('UNSPECIFIED ASSET TYPE FOR HIERARCHY TYPE ADMIN PAGE');

                return;
            }

            if (url.toUpperCase() == 'TAXONOMIES') {
                this.assetTypeClass = AssetTypeClass.Model;
                this.areaName = StringConstants.Section_Models;
                this.tabTitle = 'Model Types';
                this.objectType = 'TaxonomyType';
            }
            else if (url.toUpperCase() == 'POLICIES') {
                this.assetTypeClass = AssetTypeClass.Policy;
                this.areaName = StringConstants.Section_Policies;
                this.tabTitle = 'Policy Types';
                this.objectType = 'PolicyType';
            }

            this.getAssetTypes();
            this.selectedItemChange();
        })
    }

    selectedItemChange() {
        if (this.selected) {

            this.assetTypeService.getAssetTypeObjectAndID(this.selected.uid).subscribe(res => {
                this.selectedItemID = res.ObjectID;
                this.selectedAssetTypeID = res.Id;

                this.types.forEach(t => {
                    if (t.uid == this.selected.uid) {
                        t['AssetTypeId'] = res.Id;
                    }
                });
                this.buildSecondaryNavigationForObject(this.selected ? this.selectedItemID : 0, this.objectType);

            });
        }
    }

    openEditor(item) {
        this.selected = item;
        this.selectedItemChange();
        this.showEditor = true;
    }

    openDelete(item) {
        this.selected = item;
        this.selectedItemChange();
        this.showDelete = true;
    }

    ngOnInit() {
        this.theDeleteCallback = this.deleteType.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getAssetTypes() {
        this.isLoading = true;
        this.assetTypeService
            .getAssetTypesByClass(this.assetTypeClass)
            .subscribe(results => {
                let t = results.sort((a, b) => a.Name.localeCompare(b.Name));
                this.types = t.map((item) => {
                    return { MaximumDepth: item.HierarchyMaximumDepth, ...item };
                });

                if (this.types.length && this.types.length > 0) {
                    this.selected = this.types[0];
                    this.selectedItemChange();
                }
                this.isLoading = false;
            }, error => this.error = error);
    }

    add() {
        this.selected = null;
        this.showEditor = true;
    }

    closeEditor() {
        this.showEditor = false;

        if (this.selected == null && this.types.length > 0) {
            this.selected = this.types[0];
            this.selectedItemChange();
        }
    }

    save(event) {
        this.showEditor = false;
        this.getAssetTypes();
        this.stateService.reloadLeftNavMenu();
    }

    deleteType(id: number) {
        var uid = this.types.filter(x => x.AssetTypeId == id)[0].uid;

        this
            .assetTypeService
            .deleteSingleAssetType(uid)
            .subscribe(res => {
                this.showMessageForResult(this.messagesService, res);

                if (res.type != 'error') {
                    this.types = this.types.filter(x => x.uid != this.selected.uid);
                    this.selected = this.types.length > 0 ? this.types[0] : null;
                    this.selectedItemChange();
                    this.stateService.reloadLeftNavMenu();
                }

                this.showDelete = false;
            });
    }
}
