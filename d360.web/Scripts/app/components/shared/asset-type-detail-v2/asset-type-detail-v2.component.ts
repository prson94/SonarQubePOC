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
import {
    AssetTypeModel,
    AssetTypeModelClass,
    BusinessTypeModel,
    DiagramTypeModel,
    ModelTypeModel,
    PolicyTypeModel,
    RuleTypeModel,
    TechnicalTypeModel
} from '../../../models/asset.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AssetTypeService } from '../../../services/asset-type.service';
import {
    AssetTypeDetailCategory,
    AssetTypeDetailField,
    AssetTypeDetailFieldType,
    ControlsOptions,
    OpenBehaviour
} from './asset-type-detail-v2.model';

declare var CurrentResourceID;

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
    @Input() class: AssetTypeModelClass;
    @Input() controlsOptions: ControlsOptions = { showEdit: false, showOpen: OpenBehaviour.NEW_TAB };
    @Output() onEditClick: EventEmitter<string> = new EventEmitter<string>();

    assetTypeModel: AssetTypeModel;
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
        if (changes.uid || changes.class) {
            this.load();
        }
    }

    ngOnDestroy() {
        this.subscription?.unsubscribe();
    }

    public load(): void {
        this.isLoading = true;

        this.subscription = this.assetTypeService.getAssetTypeDetails<AssetTypeModel>
        (this.uid, this.class).subscribe((data) => {
            this.assetTypeModel = data;
            this.fillCategories(data);
            this.loadState();
            this.isLoading = false;
            this.cdRef.markForCheck();
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

    onAssetTypeOpen(isNewTab: boolean = false) {
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

    private fillBasicCategories(assetTypeModel: AssetTypeModel): void {
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
                value: assetTypeModel.BackgroundColor
            },
            {
                name: $localize`Icon`,
                type: AssetTypeDetailFieldType.ICON,
                value: assetTypeModel.Icon
            }
        ]);
        this.addFieldsToCategory($localize`System Fields`, [
            { name: 'UID', type: AssetTypeDetailFieldType.SYSTEM, value: assetTypeModel.Uid }
        ]);
    }

    private fillCategories(assetTypeModel: AssetTypeModel): void {
        this.fillBasicCategories(assetTypeModel);
        switch (this.class) {
            case AssetTypeModelClass.BusinessType:
                let businessTypeModel: BusinessTypeModel = <BusinessTypeModel>assetTypeModel;
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Predicate to Parent',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: businessTypeModel.PredicateToParent
                    }
                ]);
                this.addFieldsToCategory($localize`Settings`, [
                    {
                        name: 'Use as Transformation?',
                        type: AssetTypeDetailFieldType.BOOL,
                        value: businessTypeModel.UseAsTransformation
                    },
                    {
                        name: 'Auto Display Owner/Parent?',
                        type: AssetTypeDetailFieldType.BOOL,
                        value: businessTypeModel.AutoDisplayParent
                    },
                    { name: 'Edit Parent?', type: AssetTypeDetailFieldType.BOOL, value: businessTypeModel.EditParent }
                ]);
                break;
            case AssetTypeModelClass.TechnicalType:
                let technicalTypeModel: TechnicalTypeModel = <TechnicalTypeModel>assetTypeModel;
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Predicate to Parent',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: technicalTypeModel.PredicateToParent
                    }
                ]);
                this.addFieldsToCategory($localize`Settings`, [
                    {
                        name: 'Use as Transformation?',
                        type: AssetTypeDetailFieldType.BOOL,
                        value: technicalTypeModel.UseAsTransformation
                    },
                    {
                        name: 'Auto Display Owner/Parent?',
                        type: AssetTypeDetailFieldType.BOOL,
                        value: technicalTypeModel.AutoDisplayParent
                    },
                    { name: 'Edit Parent?', type: AssetTypeDetailFieldType.BOOL, value: technicalTypeModel.EditParent }
                ]);
                break;
            case AssetTypeModelClass.DiagramType:
                let diagramTypeModel: DiagramTypeModel = <DiagramTypeModel>assetTypeModel;
                this.addFieldsToCategory($localize`System Fields`, [
                    {
                        name: 'ID',
                        type: AssetTypeDetailFieldType.SYSTEM,
                        value: diagramTypeModel.Id
                    }
                ]);
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Flow Object Type',
                        type: AssetTypeDetailFieldType.FLOW_OBJECT_TYPE,
                        value: diagramTypeModel.FlowObjectType
                    }
                ]);
                break;
            case AssetTypeModelClass.ModelType:
                let modelTypeModel: ModelTypeModel = <ModelTypeModel>assetTypeModel;
                this.addFieldsToCategory($localize`System Fields`, [
                    {
                        name: 'ID',
                        type: AssetTypeDetailFieldType.SYSTEM,
                        value: modelTypeModel.Id
                    }
                ]);
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Predicate to Parent',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: modelTypeModel.PredicateToParent
                    }
                ]);
                this.addFieldsToCategory($localize`Settings`, [
                    {
                        name: 'Maximum Depth',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: modelTypeModel.MaximumDepth
                    }
                ]);
                break;
            case AssetTypeModelClass.PolicyType:
                let policyTypeModel: PolicyTypeModel = <PolicyTypeModel>assetTypeModel;
                this.addFieldsToCategory($localize`System Fields`, [
                    {
                        name: 'ID',
                        type: AssetTypeDetailFieldType.SYSTEM,
                        value: policyTypeModel.Id
                    }
                ]);
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Predicate to Parent',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: policyTypeModel.PredicateToParent
                    }
                ]);
                this.addFieldsToCategory($localize`Settings`, [
                    {
                        name: 'Maximum Depth',
                        type: AssetTypeDetailFieldType.TEXT,
                        value: policyTypeModel.MaximumDepth
                    }
                ]);
                break;
            case AssetTypeModelClass.RuleType:
                let ruleTypeModel: RuleTypeModel = <RuleTypeModel>assetTypeModel;
                this.addFieldsToCategory($localize`System Fields`, [
                    {
                        name: 'ID',
                        type: AssetTypeDetailFieldType.SYSTEM,
                        value: ruleTypeModel.Id
                    }
                ]);
                this.addFieldsToCategory($localize`General`, [
                    {
                        name: 'Flow Object Type',
                        type: AssetTypeDetailFieldType.FLOW_OBJECT_TYPE,
                        value: ruleTypeModel.FlowObjectType
                    }
                ]);
                break;
        }
    }
}
