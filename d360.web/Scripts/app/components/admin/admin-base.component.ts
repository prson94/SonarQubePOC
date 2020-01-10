import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';

import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { ViewChildren } from '@angular/core';
import { SecondaryNavCurrentObject } from '../../models/secondaryNav.model';



export class AdminBaseComponent extends BaseComponent {
    public areaName: string;
    public areaLink: string = undefined;    
    public area: string = "Administration";
    public adminHeading: string;
    public tabTitle: string = 'Admin'

    @ViewChildren('treetableRows') treeTableElements: any;
    private isDefaultTreeValuesSet: boolean = false;


    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected titleService: Title, secondaryNavService?: SecondaryNavService) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    setCommonItems() {

        this.area = ['Business Assets','Technical Assets','Artifacts', 'Attributes', 'Lookups', 'Models', 'Policies', 'Predicates', 'Relationships', 'Rules', 'Surveys', 'Workflow Actions', 'Workflows']
            .indexOf(this.areaName) !== -1 ? 'Configuration' : "Administration";

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));
        if (this.adminHeading)
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.adminHeading));     
        
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, this.areaLink));
        this.setBrowserTitle(this.titleService, this.areaName);
        this.secondaryNavService.clearItems();
        this.secondaryNavService.clearButtons();
        this.secondaryNavService.setCurrentArea(this.areaName, this.area === 'Configuration' ? 'fa-sliders' : "fa-cog", this.tabTitle);
        this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(null,null,null,null,true));
        this.secondaryNavService.showHeader(true);
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