import { Component, OnDestroy } from "@angular/core";
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { SecondaryNavService } from "../../../services/right-sidebar.service";
import { ResponsibilityTypeService } from "../../../services/responsibility-type.service";
import { ResponsibilityType } from "../../../models/responsibility-type.model";
import { FormMode } from "../../../models/form.model";
import { AdminBaseComponent } from "../admin-base.component";
import { Title } from "@angular/platform-browser";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { StringConstants } from "../../../static/string-constants";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: "admin-governance",
    providers: [ResponsibilityTypeService],
    templateUrl: "./admin-governance.component.html",
})

export class AdminGovernanceComponent extends AdminBaseComponent implements OnDestroy {
    formMode = FormMode.Default;
    FormMode = FormMode;
    private forceRulesReloadFlag: boolean = false;

    responsibilityTypeItems = new Array<ResponsibilityType>();
    selectedRow = new ResponsibilityType();

    theDeleteCallback: Function;

    searchText = $localize`Search...`;
    deletePromptText = $localize`Are you sure you want to delete this responsibility type?`;

    constructor(
        secondaryNavService: SecondaryNavService,
        private responsibilityTypeService: ResponsibilityTypeService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Responsibilities;
        this.adminHeading = StringConstants.SubArea_Security;
        this.tabTitle = $localize`Responsibility Types`;
        this.theDeleteCallback = this.doDelete.bind(this);
    }

    selectedItemChange() {
        this.responsibilityTypeService.getAdminResponsibilityTypeDetails(this.selectedRow.uid).subscribe((res) => {
            this.selectedRow.ID = res.data.ID;
            this.buildSecondaryNavigationForObject(this.selectedRow.ID, "ResponsibilityType");
        });
    }

    ngOnInit() {
        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(): void {
        this.responsibilityTypeService.getAdminResponsibilityTypes()
            .subscribe((data) => {
                this.secondaryNavService.clearItems();
                this.responsibilityTypeItems = data;
                this.selectedRow = this.responsibilityTypeItems[0];
                this.selectedItemChange();
            });
    }

    add(): void {
        this.formMode = FormMode.Adding;
    }

    edit(uid: string): void {
        this.formMode = FormMode.Editing;
        this.selectedRow = this.responsibilityTypeItems.find((i) => i.uid === uid);
        this.selectedItemChange();
    }

    delete(uid: string): void {
        this.formMode = FormMode.Deleting;
        this.selectedRow = this.responsibilityTypeItems.find(i => i.uid === uid);
        this.selectedItemChange();
    }

    save(e: any) {
        this.showMessageForResult(this.messagesService, e);
        this.formMode = FormMode.Default;
        this.load();
    }

    cancel() {
        this.formMode = FormMode.Default;
    }

    responsibilityRelationDelete() {
        this.forceRulesReloadFlag = !this.forceRulesReloadFlag;
    }
    doDelete() {
        this.responsibilityTypeService.deleteResponsibilityType(this.selectedRow.uid, true).subscribe((res) => {
            if (res && res.Success) {
                this.messagesService.showInfoMessage($localize`Success`, $localize`Item deleted successfully`);
            }
            else {
                this.messagesService.showError($localize`Error`, $localize`An error occurred`);
            }
            this.formMode = FormMode.Default;
            this.selectedRow = null;
            this.load();
        });
    }
}