import { Component, OnInit, OnDestroy } from "@angular/core";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { RulesService } from "../../../services/rules.service";
import { StateService } from "../../../services/state.service";
import { AdminBaseComponent } from "../admin-base.component";
import { RuleType } from "../../../models/rule.model";
import { Title } from "@angular/platform-browser";
import { SecondaryNavItem } from "../../../models/secondaryNav.model";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { AssetTypeClass, AssetTypeApiModel } from "../../../models/asset.model";
import { StringConstants } from "../../../static/string-constants";
import { AssetTypeService } from "../../../services/asset-type.service";
import { AssetService } from "../../../services/asset.service";
import { CompanySettingsService } from "../../../services/settings.service";
import '@angular/localize/init';

@Component({
    selector: "d3s-admin-rules-component",
    providers: [AssetTypeService, AssetService],
    templateUrl: "./admin-rules.component.html"
})

export class AdminRulesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    ruleTypes: AssetTypeApiModel[] = [];
    selected: AssetTypeApiModel;
    showEditor: boolean = false;
    showDelete: boolean = false;
    assetTypeClass: AssetTypeClass;
    theDeleteCallback: Function;
    private isDimensionsVisible: boolean = false;

    get assetTypeEditorTitle(): string {
        if (this.selected === null) {
            return $localize`New Rule Type`;
        }
        return $localize`Edit Rule Type`;
    }

    constructor(
        private stateService: StateService,
        protected secondaryNavService: SecondaryNavService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private assetTypeService: AssetTypeService,
        private assetsService: AssetService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Rules;
        this.setCommonItems();
        this.theDeleteCallback = this.deleteRuleType.bind(this);
    }

    ngOnInit() {
        this.assetTypeClass = AssetTypeClass.Rule;
        this.getRuleTypes();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    protected getRuleTypes() {
        this.isLoading = true;
        this.assetTypeService.getAssetTypesByClass(AssetTypeClass.Rule)
            .subscribe((result) => {
                this.ruleTypes = result;
                this.isLoading = false;
                if (this.ruleTypes.length > 0) {
                    this.selected = this.ruleTypes[0];
                    this.selectedItemChange(this.selected.ID);
                }
            });
    }

    deleteRuleType(uid: string) {
        this.assetTypeService.deleteSingleAssetType(uid).subscribe((result) => {
            this.showDelete = false;
            if (result.type != "error") {
                result.title = $localize`Success` + "!";
                this.showMessageForResult(this.messagesService, result, $localize`Item successfully removed` + ".");
                this.ruleTypes = this.ruleTypes.filter((x) => x.uid !== uid);
                this.selected = this.ruleTypes.length > 0 ? this.ruleTypes[0] : null;
            }
            else {
                this.showMessageForResult(this.messagesService, result);
            }
            this.stateService.reloadLeftNavMenu();
        });
    }

    saveRuleType($event) {
        this.showEditor = false;
        this.getRuleTypes();
        this.stateService.reloadLeftNavMenu();
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.ruleTypes.length > 0 ? this.ruleTypes[0] : null;
        }
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    protected showHideBreadcrumbItem(activatedItem: SecondaryNavItem) {
        if (activatedItem.tag == "dimensions") {
            this.isDimensionsVisible = !this.isDimensionsVisible;
        }
    }

    selectedItemChange(objectId: number) {
        this.loadDataAndExecuteAction();
        this.buildSecondaryNavigationForObject(objectId ? objectId : 0, StringConstants.ObjectRuleType, null, this.assetTypeClass);
    }

    private loadDataAndExecuteAction() {
        if (this.selected) {
            this.assetsService.getAssetTypeLegacyData(this.selected.uid)
                .subscribe((res) => {
                    this.selected.ID = res.ObjectID;
                    this.selected.AssetTypeID = res.AssetTypeID;
                });
        }
    }
}