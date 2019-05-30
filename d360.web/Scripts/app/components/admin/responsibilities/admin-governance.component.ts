import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { MessagesService } from '../../../services/messages.service';
import { ResponsibilityType, IResponsibilityTypeService } from '../../../models/responsibility-type.model';
import { FormMode } from '../../../models/form.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'admin-governance',
    providers: [ResponsibilityTypeService],
    templateUrl: './admin-governance.component.html',
})

export class AdminGovernanceComponent extends AdminBaseComponent implements OnDestroy {    
    private formMode = FormMode.Default;
    private FormMode = FormMode;

    private responsibilityTypeItems = new Array<ResponsibilityType>();
    private selectedRow = new ResponsibilityType();

    constructor(rightSidebarService: RightSidebarService, private responsibilityTypeService: ResponsibilityTypeService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title, protected messagesService: MessagesService) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Responsibility Types";
        this.adminHeading = "Security";
        this.setCommonItems();
        this.setCommonRightSideBar();
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/ResponsibilityType/${this.selectedRow.ID}`
            });
        }
        this.load();
    }
    

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(): void {
        this.responsibilityTypeService.getAdminResponsibilityTypes()
            .then(data => {
                this.responsibilityTypeItems = data;
                this.selectedRow = this.responsibilityTypeItems[0];
            });
    }

    add(): void {
        this.formMode = FormMode.Adding;
    }

    edit(id: number): void {
        this.formMode = FormMode.Editing;        
        this.selectedRow = this.responsibilityTypeItems.find(i => i.ID == id);
    }
     
    delete(id: number): void {
        this.formMode = FormMode.Deleting;
        this.selectedRow = this.responsibilityTypeItems.find(i => i.ID == id);
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
}