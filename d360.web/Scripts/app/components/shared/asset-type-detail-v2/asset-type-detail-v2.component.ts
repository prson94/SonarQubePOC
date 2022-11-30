import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnDestroy,
    Output,
    SimpleChange,
    ViewEncapsulation
} from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthenticationService } from '../../../services/authentication.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AssetTypeService } from '../../../services/asset-type.service';
import {
    AssetTypeDetailCategory,
    AssetTypeDetailField,
    AssetTypeDetailFieldType,
    ControlsOptions,
    OpenBehaviour
} from './asset-type-detail-v2.model';
import { AssetTypeApiModel, AssetTypeClass } from "../../../models/asset.model";

declare const CurrentResourceID;

@Component({
    selector: 'ig-asset-type-detail-v2',
    templateUrl: './asset-type-detail-v2.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [
        `.p-datatable-wrapper table {
            table-layout: unset !important;
        }`
    ],
    encapsulation: ViewEncapsulation.None
})


export class AssetTypeDetailV2Component implements OnChanges, OnDestroy {
    @Input() uid: string;
    @Input() controlsOptions: ControlsOptions = { showEdit: false, showOpen: OpenBehaviour.NEW_TAB };
    @Output() onEditClick: EventEmitter<string> = new EventEmitter<string>();

    assetTypeModel: AssetTypeApiModel;
    categories: AssetTypeDetailCategory[] = [];
    subscription: Subscription;

    isAdmin: boolean;
    isLoading: boolean;

    constructor(
        private router: Router,
        private assetTypeService: AssetTypeService,
        private authService: AuthenticationService,
        private cdRef: ChangeDetectorRef
    ) {
        this.authService.checkCurrentUserAdmin().subscribe((res) => {
            this.isAdmin = res;
        });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes.uid || changes.parentUid) {
            this.load();
        }
    }

    ngOnDestroy() {
        this.subscription?.unsubscribe();
    }

    public load(): void {
        this.isLoading = true;
        this.subscription = this.assetTypeService.GetAssetTypeByUid(this.uid).subscribe((data) => {
            this.assetTypeModel = data;
            this.categories = [];
            if (this.assetTypeModel) {
                this.fillCategories(this.assetTypeModel);
                this.loadState();
                this.isLoading = false;
                this.cdRef.markForCheck();
            }
        });
    }

    onStateSave() {
        localStorage.setItem(
            this.localStorageKey,
            JSON.stringify(
                this.categories.map((category) => {
                    return { name: category.name, active: category.active };
                })
            )
        );
    }

    private loadState() {
        const states: { name: string; active: boolean }[] = JSON.parse(localStorage.getItem(this.localStorageKey));
        if (states !== null) {
            states.forEach((state) => {
                const category = this.categories.find((category) => category.name === state.name);
                if (category) {
                    category.active = state.active;
                }
            });
        }
    }

    get localStorageKey(): string {
        return `asset_type_detail_${ CurrentResourceID }_${ this.uid }`;
    }

    onAssetTypeOpen(isNewTab = false) {
        const openUrl = `${ SiteUrlHelpers.getAssetTypeUrl(this.uid) }`;
        if (isNewTab) {
            window.open(openUrl, '_blank');
        } else {
            this.router.navigateByUrl(openUrl);
        }
    }

    private sortCategories(categories: AssetTypeDetailCategory[]): AssetTypeDetailCategory[] {
        const priority = [$localize`General`, $localize`Settings`, $localize`Styles`, $localize`System Fields`];
        return categories.sort((a, b) => {
            return priority.indexOf(a.name) - priority.indexOf(b.name);
        });
    }

    private addFieldsToCategory(name: string, fields: AssetTypeDetailField[]): void {
        const category = this.categories.find((category) => category.name === name);
        if (category) {
            category.fields.push(...fields);
        } else if (fields.length > 0) {
            this.categories.push({
                name,
                active: true,
                fields
            });
            this.categories = this.sortCategories(this.categories);
        }
    }

    private fillBasicCategories(assetTypeModel: AssetTypeApiModel): void {
        this.addFieldsToCategory($localize`General`, [
            { name: $localize`Name`, type: AssetTypeDetailFieldType.TEXT, value: assetTypeModel.Name },
            {
                name: $localize`Display Format`,
                type: AssetTypeDetailFieldType.TEXT,
                value: assetTypeModel.DisplayFormat
            },
            {
                name: $localize`Description`,
                type: AssetTypeDetailFieldType.HTML,
                value: assetTypeModel.Description
            }
        ]);
        this.addFieldsToCategory($localize`Styles`, [
            {
                name: $localize`Background Color`,
                type: AssetTypeDetailFieldType.COLOR,
                value: assetTypeModel.IconStyle.BackColor
            },
            {
                name: $localize`Icon`,
                type: AssetTypeDetailFieldType.ICON,
                value: assetTypeModel.IconStyle.Icon
            }
        ]);
        this.addFieldsToCategory($localize`System Fields`, [
            { name: 'UID', type: AssetTypeDetailFieldType.SYSTEM, value: assetTypeModel.uid }
        ]);
    }

    private fillCategories(assetTypeModel: AssetTypeApiModel): void {
        this.fillBasicCategories(assetTypeModel);
        switch (assetTypeModel.Class.ID) {
            case AssetTypeClass.BusinessAsset:
            case AssetTypeClass.TechnicalAsset:
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Predicate to Parent',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: assetTypeModel.PredicateInverse
                    }
                ]);
                this.addFieldsToCategory($localize`Settings`, [
                    {
                        name: 'Use as Transformation?',
                        type: AssetTypeDetailFieldType.BOOL,
                        value: assetTypeModel.UseAsTransformation
                    },
                    {
                        name: 'Auto Display Owner/Parent?',
                        type: AssetTypeDetailFieldType.BOOL,
                        value: assetTypeModel.AutoDisplayParent
                    },
                    { name: 'Edit Parent?', type: AssetTypeDetailFieldType.BOOL, value: assetTypeModel.CanEditParent }
                ]);
                break;
            case AssetTypeClass.Rule:
            case AssetTypeClass.DiagramAsset:
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Flow Object Type',
                        type: AssetTypeDetailFieldType.FLOW_OBJECT_TYPE,
                        value: assetTypeModel.FlowObjectType
                    }
                ]);
                break;
            case AssetTypeClass.Model:
            case AssetTypeClass.Policy:
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Predicate to Parent',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: assetTypeModel.PredicateInverse
                    }
                ]);
                this.addFieldsToCategory($localize`Settings`, [
                    {
                        name: 'Maximum Depth',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: assetTypeModel.HierarchyMaximumDepth
                    }
                ]);
                break;
        }
    }
}
