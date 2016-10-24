import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService, PageHeader, FusionService, RightSidebarService, MessagesService  } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component';
import { FormMode } from '../../models/form.model';
import { FusionType } from '../../models/fusion.model';
import { ObjectStyle } from '../../models/object-style.model';
import { Title } from '@angular/platform-browser';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-fusion',
    providers: [FusionService],
    templateUrl: './admin-fusion.component.html',
})

export class AdminFusionComponent extends AdminBaseComponent implements OnDestroy {
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    fusionTypes: FusionType[];
    selectedRow: FusionType;
    newFusionType: FusionType;
    newFusionStyle: ObjectStyle;

    constructor(rightSidebarService: RightSidebarService,
        pageHeader: PageHeader,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private fusionService: FusionService,
        titleService: Title,
        private messagesService: MessagesService) {
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
        this.newFusionStyle = new ObjectStyle();
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
                    console.log(data);
                    this.showMessageForResult(this.messagesService, data);
                    this.load();
                    this.formMode = FormMode.Default;
                })
        } else if (this.formMode == FormMode.Adding) {
            this.fusionService.postFusionType(this.newFusionType, this.newFusionStyle)
                .then(data => {
                    console.log(data);
                    this.showMessageForResult(this.messagesService, data);
                    this.load();
                    this.formMode = FormMode.Default;
                });
        }
    }
}


