import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';

import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { ViewChildren } from '@angular/core';



export class AdminBaseComponent extends BaseComponent {
    public areaName: string;
    public areaLink: string = undefined;    
    public area: string = "Administration";
    public adminHeading: string;
    public tabTitle: string = 'Admin'

    @ViewChildren('treetableRows') treeTableElements: any;
    private isDefaultTreeValuesSet: boolean = false;


    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected titleService: Title, rightSidebarService?: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    setCommonItems() {

        this.area = ['Artifacts', 'Attributes', 'Lookups', 'Models', 'Policies', 'Predicates', 'Relationships', 'Rules', 'Surveys', 'Workflows', 'Action Types']
            .indexOf(this.areaName) !== -1 ? 'Configuration' : "Administration";

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));
        if (this.adminHeading)
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.adminHeading));     
        
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, this.areaLink));
        this.rightSidebarService.clearItems();
        this.rightSidebarService.clearButtons();
        this.rightSidebarService.setCurrentArea(this.areaName, this.area === 'Configuration' ? 'fa-sliders' : "fa-cog", this.tabTitle);
        this.rightSidebarService.setCurrentObject(null,null,null,null,true);
        this.rightSidebarService.showHeader(true);
        this.setBrowserTitle(this.titleService, this.areaName);
    }       


    //Prime NG tree table doesnt handle default values good, trigger click on first element in p-treetable to mark it as seletected
    ngAfterContentChecked() {
        if (this.treeTableElements !== undefined && !this.isDefaultTreeValuesSet) {
            if (!this.treeTableElements.some(x => x.nativeElement.className.includes('ui-state-highlight'))) {
                this.treeTableElements.map((x, index) => {
                    if (index == 0) {
                        x.nativeElement.click();
                        this.isDefaultTreeValuesSet = true;
                    }
                });
            }
        }
    }
}