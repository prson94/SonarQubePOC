
import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { DomainType, IDomainService } from '../../models/domain.model';
import { DomainService, HeaderBreadcrumbService, PageHeader, RightSidebarService } from '../../services/index';
import { AdminBaseComponent} from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'admin-domain',
    providers: [DomainService],
    templateUrl: 'scripts/app/components/admin/admin-domain.component.html',
})

export class AdminDomainComponent extends AdminBaseComponent implements OnDestroy {
    domainTypes = new Array<DomainType>(); 
    objectType = 'DomainType';
    selectedRow: DomainType;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    newRow: DomainType = new DomainType();

    constructor(rightSidebarService : RightSidebarService,private domainService: DomainService, pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService );
        this.areaDescription = "All type of reference data lists for the organization are defined here. To add a new type of list, go under Actions and select Add type.";
        this.areaName = "Reference Types";
        this.setCommonItems();
        this.setCommonRightSideBar();
        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load() {

        this.isLoading = true;
        this.domainService.getDomains()
            .then(data => {
                this.domainTypes = data;
                this.selectedRow = this.domainTypes[0];
                this.isLoading = false;
            });       
    }
}