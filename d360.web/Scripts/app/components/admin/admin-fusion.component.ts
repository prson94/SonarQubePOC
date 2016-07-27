///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone, OnDestroy } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { DataTable, Column, InputText, Editor, Header, Button } from 'primeng/primeng';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService, PageHeader, FusionService, RightSidebarService  } from '../../services/index';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { AdminBaseComponent } from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { FusionType, ObjectStyle } from '../../models/fusion.model';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FusionAttributesTile } from '../tiles/fusion-attributes.tile';
import { FusionConfigurationTile } from '../tiles/fusion-configuration.tile';
import { Title } from '@angular/platform-browser';
import { DeleteForm } from '../forms/delete.form';
import { AuditComponent} from '../shared/audit.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-fusion',
    providers: [FusionService],
    directives: [
        DataTable,
        Column,
        Editor,
        Header,
        Button,
        InputText,
        TileActionsComponent,
        PeopleResponsibilitiesTile,
        FusionConfigurationTile,
        FusionAttributesTile,
        ObjectDetailTile,
        FieldDefinitionTile,
        DeleteForm,
        AuditComponent
    ],
    templateUrl: 'scripts/app/components/admin/admin-fusion.component.html',
})

export class AdminFusionComponent extends AdminBaseComponent implements OnDestroy {
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    fusionTypes: FusionType[];
    selectedRow: FusionType;
    newFusionType: FusionType;
    newFusionStyle: ObjectStyle;

    constructor(rightSidebarService: RightSidebarService, pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, private fusionService: FusionService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = "Here you will find all Fusion sources and synchronization settings.";
        this.areaName = "Fusion Types";
        this.setCommonItems();
        this.setCommonRightSideBar();
        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load() {
        this.isLoading = true;
        this.fusionService.getFusionTypes('$orderby=Name')
            .then(data => {
                this.fusionTypes = data;
                this.selectedRow = (this.fusionTypes && this.fusionTypes.length) ? this.fusionTypes[0] : null;
                this.isLoading = false;
            });
    }
    
    add() {
        this.newFusionType = new FusionType();
        this.formMode = FormMode.Adding;
    }

    edit() {
        this.isLoading = true;
        this.fusionService.getFusionTypeStyle(this.selectedRow.ID)
            .then(data => {
                
                this.newFusionStyle = data;

                if (!this.newFusionStyle) {
                    this.newFusionStyle = new ObjectStyle();
                    this.newFusionStyle.ObjectType = 'FusionType';
                    this.newFusionStyle.ObjectID = this.selectedRow.ID;
                    this.newFusionStyle.IconBackColor = '#000000';
                    this.newFusionStyle.IconForeColor = '#ffffff';
                }

                this.newFusionType = _.cloneDeep(this.selectedRow);
                this.isLoading = false;
                this.formMode = FormMode.Editing;
            });
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {
        this.isLoading = true;
        if (this.formMode == FormMode.Editing) {
            this.fusionService.putFusionType(this.newFusionType, this.newFusionStyle)
                .then(data => {
                    this.load();
                    this.formMode = FormMode.Default;
                })
        } else if (this.formMode == FormMode.Adding) {
            this.fusionService.postFusionType(this.newFusionType, this.newFusionStyle)
                .then(data => {
                    this.load();
                    this.formMode = FormMode.Default;
                });
        }
    }
}


