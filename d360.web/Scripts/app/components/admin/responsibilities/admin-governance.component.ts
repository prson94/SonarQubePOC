import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { ResponsibilityType, IResponsibilityTypeService } from '../../../models/responsibility-type.model';
import { FormMode } from '../../../models/form.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'admin-governance',
    providers: [ResponsibilityTypeService],
    templateUrl: './admin-governance.component.html',
})

export class AdminGovernanceComponent extends AdminBaseComponent implements OnDestroy {
    private formMode = FormMode.Default;
    private FormMode = FormMode;
    private forceRulesReloadFlag: boolean = false;

    private responsibilityTypeItems = new Array<ResponsibilityType>();
    private selectedRow = new ResponsibilityType();

    constructor(secondaryNavService: SecondaryNavService, private responsibilityTypeService: ResponsibilityTypeService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, protected messagesService: MessagesObservableService) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = "Responsibilities";
        this.adminHeading = "Security";
        this.tabTitle = 'Responsibility Types';
        this.load();
    }

    selectedItemChange() {
        this.buildSecondaryNavigationForObject(this.selectedRow ? this.selectedRow.ID : 0,  'ResponsibilityType');
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(): void {
        this.responsibilityTypeService.getAdminResponsibilityTypes()
            .subscribe(data => {
                this.responsibilityTypeItems = data;
                this.selectedRow = this.responsibilityTypeItems[0];
                this.selectedItemChange();
            });
    }

    add(): void {
        this.formMode = FormMode.Adding;
    }

    edit(id: number): void {
        this.formMode = FormMode.Editing;
        this.selectedRow = this.responsibilityTypeItems.find(i => i.ID == id);
        this.selectedItemChange();
    }

    delete(id: number): void {
        this.formMode = FormMode.Deleting;
        this.selectedRow = this.responsibilityTypeItems.find(i => i.ID == id);
        this.selectedItemChange();
    }

    save(e: any) {
        this.showMessageForResult(this.messagesService, e);
        this.formMode = FormMode.Default;
        this.load();
    }

    confirmDelete(e: any) {
        if (e == 'error') {
            this.messagesService.showError('Error', 'An error occurred');
        }
        else {
            this.messagesService.showInfoMessage('Success', 'Item deleted successfully');
        }

        this.formMode = FormMode.Default;
        this.load();
    }

    cancel() {
        this.formMode = FormMode.Default;
    }

    responsibilityRelationDelete() {
        this.forceRulesReloadFlag = !this.forceRulesReloadFlag;
    }
}