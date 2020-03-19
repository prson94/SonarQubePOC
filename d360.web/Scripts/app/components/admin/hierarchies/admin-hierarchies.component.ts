import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { PoliciesService } from '../../../services/policies.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { AssetTypeService } from '../../../services/asset-type.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { ModelsService } from '../../../services/models.service';

@Component({
    selector: 'd3s-admin-models-component',
    providers: [ModelsService, AssetTypeService, PoliciesService],
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

    constructor(
        private activatedRoute: ActivatedRoute,
        private stateService: StateService,
        protected assetTypeService: AssetTypeService,
        protected policiesService: PoliciesService,
        secondaryNavService: SecondaryNavService,
        private modelService: ModelsService,
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
                this.areaName = 'Models';
                this.tabTitle = 'Model Types';
                this.objectType = 'TaxonomyType';

                this.getModelTypes();
            }
            else if (url.toUpperCase() == 'POLICIES') {
                this.assetTypeClass = AssetTypeClass.Policy;
                this.areaName = 'Policies';
                this.tabTitle = 'Policy Types';
                this.objectType = 'PolicyType';

                this.getPolicyTypes();
            }
            this.selectedItemChange();
        })
    }

    selectedItemChange() {
        if (this.selected) {
            switch (this.assetTypeClass) {
                case AssetTypeClass.Model:
                    this.assetTypeService.getAssetTypeObjectAndID(this.selected.uid).subscribe(res => {
                        this.selectedItemID = res.ObjectID;
                        this.buildSecondaryNavigationForObject(this.selected ? this.selectedItemID : 0, this.objectType);
                    });
                    break;
                case AssetTypeClass.Policy:
                    this.buildSecondaryNavigationForObject(this.selected ? this.selected.ID : 0, this.objectType);
                    break;
                default:
            }
        }
    }

    getSelectedItemID() {
        if (this.selected) {
            switch (this.assetTypeClass) {
                case AssetTypeClass.Model:
                    this.assetTypeService.getAssetTypeObjectAndID(this.selected.uid).subscribe(res => {
                        return res.ObjectID;
                    });
                    break;
                case AssetTypeClass.Policy:
                    return this.selected.ID;
                    break;
                default:
            }
        }
    }

    ngOnInit() {
        this.theDeleteCallback = this.deleteType.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar(); 
    }

    getModelTypes() {
        this.isLoading = true;
        this.modelService
            .getModels()
            .subscribe(results => {
                this.types = results.sort((a, b) => a.Name.localeCompare(b.Name));

                if (this.types.length && this.types.length > 0) {
                    this.selected = this.types[0];
                    this.selectedItemChange();
                }
                this.isLoading = false;
            }, error => this.error = error);
    }

    getPolicyTypes() {
        this.isLoading = true;

        this.policiesService.getPolicyTypes()
            .subscribe(
                result => {
                    this.types = result.sort((a, b) => a.Name.localeCompare(b.Name));

                    if (this.types.length > 0) {
                        this.selected = this.types[0];
                        this.selectedItemChange();
                    }

                    this.isLoading = false;
                }
            );
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
        if (this.assetTypeClass == AssetTypeClass.Model) {
            this.getModelTypes();
        }
        else if (this.assetTypeClass == AssetTypeClass.Policy) {
            this.getPolicyTypes();
        }

        this.stateService.reloadLeftNavMenu();
    }

    deleteType(id: number) {
        this
            .assetTypeService
            .deleteAssetType(id)
            .subscribe(res => {
                this.showMessageForResult(this.messagesService, res);

                if (res.type != 'error') {
                    this.types = this.types.filter(x => x.AssetTypeID != id);
                    this.selected = this.types.length > 0 ? this.types[0] : null;
                    this.selectedItemChange();
                    this.stateService.reloadLeftNavMenu();
                }

                this.showDelete = false;
            })
            ;
    }
}
