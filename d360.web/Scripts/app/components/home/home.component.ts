///<reference path="../../es6-shim.d.ts"/>
import {Component} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'home',
    templateUrl: 'scripts/app/components/home/home.component.html'
})

export class HomeComponent extends BaseComponent {
    private showActivityDetails: boolean = false;
    private showBoardDetails: boolean = false;
    private showAssignmentDetails: boolean = false;


    private selectedArtifactTypeId: number;
    private selectedArtifactTypeName: string;

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Home');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Home'));
    }

    private onShowActivityDetails(event) {
        this.showActivityDetails = true;        
        this.showBoardDetails = false;
        this.showAssignmentDetails = false;
        this.selectedArtifactTypeId = event.Id;
        this.selectedArtifactTypeName = event.name;
    }

    private onShowBoardDetails(event) {
        this.showBoardDetails = true;
        this.showAssignmentDetails = false;
        this.showActivityDetails = false;        
    }

    private updateBoardCounts() {

    }
}