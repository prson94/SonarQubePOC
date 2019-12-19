import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { FusionService } from '../../../services/fusion.service';
import { AssetStyleService } from '../../../services/asset-style.service';
import { AdminBaseComponent } from '../admin-base.component';
import { FormMode } from '../../../models/form.model';
import { FusionType } from '../../../models/fusion.model';
import { Title } from '@angular/platform-browser';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeStyle } from '../../../models/asset-type-style.model';

@Component({
    selector: 'd3s-admin-fusion',
    providers: [FusionService, AssetStyleService],
    templateUrl: './admin-fusion.component.html',
})

export class AdminFusionComponent extends AdminBaseComponent implements OnDestroy {
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    fusionTypes: FusionType[];
    selectedRow: FusionType;
    newFusionType: FusionType;
    newFusionStyle: AssetTypeStyle;

    constructor(
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private fusionService: FusionService,
        titleService: Title,
        private messagesService: MessagesObservableService,
        private objectStyleService: AssetStyleService
    ) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = 'Fusion';
        this.tabTitle = 'Fusion Types';
        this.adminHeading = 'Integration';
        this.setCommonItems();
        this.setCommonSecondaryNavTabs();

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/FusionType/${this.selectedRow.ID}`;
            });
        }

        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load() {
        this.isLoading = true;
        this.fusionService.getFusionTypes('$orderby=Name').subscribe(
            data => {
                this.fusionTypes = data;
                this.selectedRow = (this.fusionTypes && this.fusionTypes.length) ? this.fusionTypes[0] : null;
                this.isLoading = false;
            }
        );
    }

    add() {
        this.newFusionType = new FusionType();
        this.newFusionStyle = new AssetTypeStyle();
        this.newFusionStyle.IconBackColor = '#000000';
        this.newFusionStyle.IconForeColor = '#ffffff';
        this.formMode = FormMode.Adding;
    }

    edit() {
        this.isLoading = true;
        this.objectStyleService.getAssetTypeStyle(this.selectedRow.AssetTypeID).subscribe(
            data => {
                this.newFusionStyle = data;

                if (!this.newFusionStyle) {
                    this.newFusionStyle = new AssetTypeStyle();
                    this.newFusionStyle.ID = this.selectedRow.AssetTypeID;
                    this.newFusionStyle.IconBackColor = '#000000';
                    this.newFusionStyle.IconForeColor = '#ffffff';
                }

                this.newFusionType = _.cloneDeep(this.selectedRow);
                this.isLoading = false;
                this.formMode = FormMode.Editing;
            }
        );
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {
        this.isLoading = true;
        if (this.formMode == FormMode.Editing) {
            this.fusionService.putFusionType(this.newFusionType, this.newFusionStyle).subscribe(
                data => {
                    this.showMessageForResult(this.messagesService, data);
                    this.load();
                    this.formMode = FormMode.Default;
                }
            );
        } else if (this.formMode == FormMode.Adding) {
            this.fusionService.postFusionType(this.newFusionType, this.newFusionStyle).subscribe(
                data => {
                    this.showMessageForResult(this.messagesService, data);
                    this.load();
                    this.formMode = FormMode.Default;
                }
            );
        }
    }
}
