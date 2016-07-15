///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { DataTable, Column } from 'primeng/primeng';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { AdminBaseComponent } from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { FusionService } from '../../services/fusion.service';
import { FusionType } from '../../models/fusion.model';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FusionAttributesTile } from '../tiles/fusion-attributes.tile';
import { FusionConfigurationTile } from '../tiles/fusion-configuration.tile';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-fusion',
    providers: [FusionService],
    directives: [
        DataTable,
        Column,
        TileActionsComponent,
        PeopleResponsibilitiesTile,
        FusionConfigurationTile,
        FusionAttributesTile,
        ObjectDetailTile,
        FieldDefinitionTile,
    ],
    templateUrl: 'scripts/app/components/admin/admin-fusion.component.html',
})

export class AdminFusionComponent extends AdminBaseComponent {
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    fusionTypes: FusionType[];
    selectedRow: FusionType;

    constructor(pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, private fusionService: FusionService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
        this.areaDescription = "Here you will find all Fusion sources and synchronization settings.";
        this.areaName = "Fusion Types";
        this.setCommonItems();
        this.load();
    }

    load() {
        this.isLoading = true;
        this.fusionService.getFusionTypes('$orderby=Name')
            .then(data => {
                this.fusionTypes = data;
                this.selectedRow = this.fusionTypes[0];
                this.isLoading = false;
            });
    }

    select(e) {
        this.selectedRow = e.data;
    }
}


