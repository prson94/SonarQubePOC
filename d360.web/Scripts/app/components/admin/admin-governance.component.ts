import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService, RightSidebarService, ResponsibilityTypeService, MessagesService } from '../../services/index';
import { ResponsibilityType, IResponsibilityTypeService } from '../../models/responsibility-type.model';
import { FormMode } from '../../models/form.model';
import { AdminBaseComponent } from './admin-base.component';
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
        this.setCommonItems();
        this.setCommonRightSideBar();
        this.load();
    }
    

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(): void {
        this.responsibilityTypeService.getResponsibilityTypes()
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
        //console.log(id);
        this.selectedRow = this.responsibilityTypeItems.find(i => i.ID == id);
        //console.log(this.selectedRow);
        //console.log(this.responsibilityTypeItems);

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
        var msg = {
            type: 'success',
            title: 'Success',
            message: 'Item deleted successfully'
        };

        if (e == 'error') {
            msg = {
                type: 'error',
                title: 'Error',
                message: 'An error occurred'
            }
        }

        this.showMessageForResult(this.messagesService, e);
        this.formMode = FormMode.Default;
        this.load();
    }

    cancel() {
        this.formMode = FormMode.Default;
    }
}