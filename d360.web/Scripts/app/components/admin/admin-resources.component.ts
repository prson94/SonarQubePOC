///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone, OnInit } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { PageHeader } from '../../services/page-header.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AdminBaseComponent } from './admin-base.component';
import { DataTable, Column } from 'primeng/primeng';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { DeleteForm } from '../forms/delete.form';
import { FormMode } from '../../models/form.model';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { DynamicGridComponent } from '../shared/dynamic-grid.component';


@Component({
    selector: 'd3s-admin-resources',
    directives: [DataTable, Column, TileActionsComponent, NgSwitch, NgSwitchCase, NgSwitchDefault, DeleteForm, FieldDefinitionTile, DynamicGridComponent],
    templateUrl: 'scripts/app/components/admin/admin-resources.component.html'
})

export class AdminResourcesComponent extends AdminBaseComponent {

    private objectType = 'ResourceType';
    private objectID = 1;

    constructor(pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Here you will find all current resources.";
        this.areaName = "Resources";
        this.setCommonItems();
    }

    ngOnInit() {
    }

    resourceUri(): string {
        return `/api/resources/${this.objectID}?$orderby=LastName,FirstName`;
    }
}