///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone, OnDestroy } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { DomainType, IDomainService } from '../../models/domain.model';
import { DomainService, HeaderBreadcrumbService, PageHeader, RightSidebarService } from '../../services/index';
import { DataTable, Column } from 'primeng/primeng';
import { AdminBaseComponent} from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';
import { DeleteForm } from '../forms/delete.form';
import { Title } from '@angular/platform-browser';
import { AuditComponent} from '../shared/audit.component';

@Component({
    selector: 'admin-domain',
    providers: [DomainService],
    directives: [
        ObjectDetailTile,
        FieldDefinitionTile,
        PeopleResponsibilitiesTile,
        DataTable,
        Column,
        NgSwitch,
        NgSwitchCase,
        NgSwitchDefault,
        TileActionsComponent,
        DynamicEditorComponent,
        DeleteForm,
        AuditComponent
    ],
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