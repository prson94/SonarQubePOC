import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService } from '../../services/messages.service';
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

    @ViewChildren('treetableRows') treeTableElements: any;
    private isDefaultTreeValuesSet: boolean = false;


    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected titleService: Title, rightSidebarService?: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    setCommonItems() {

        this.area = ['Artifacts', 'Attribute Groups', 'Lookup Types', 'Models', 'Policy Types', 'Predicates', 'Relationship Types', 'Rule Types', 'Surveys']
            .indexOf(this.areaName) !== -1 ? 'Configuration' : "Administration";

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));
        if (this.adminHeading)
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.adminHeading));     
        
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, this.areaLink));     
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