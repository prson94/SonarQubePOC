import {Title} from '@angular/platform-browser';
import * as _ from 'lodash';
import {Subject} from "rxjs";
import {takeUntil} from "rxjs/operators";
import {Component, OnDestroy} from '@angular/core';

import {FormMode} from '../../../models/form.model';
import {FusionType} from '../../../models/fusion.model';
import {ObjectStyle} from '../../../models/object-style.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {RightSidebarService} from '../../../services/right-sidebar.service';
import {FusionService} from '../../../services/fusion.service';
import {ObjectStyleService} from '../../../services/object-style.service';
import {MessagesService} from '../../../services/messages.service';

import {AdminBaseComponent} from '../admin-base.component';

@Component({
    selector: 'd3s-admin-fusion',
    providers: [FusionService, ObjectStyleService],
    templateUrl: './admin-fusion.component.html',
})

export class AdminFusionComponent extends AdminBaseComponent implements OnDestroy {
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    fusionTypes: FusionType[];
    selectedRow: FusionType;
    newFusionType: FusionType;
    newFusionStyle: ObjectStyle;

    destroySubject$: Subject<void> = new Subject();

    constructor(
        rightSidebarService: RightSidebarService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private fusionService: FusionService,
        titleService: Title,
        private messagesService: MessagesService,
        private objectStyleService: ObjectStyleService
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);

        this.areaName = "Fusion Types";
        this.setCommonItems();
        this.setCommonRightSideBar();

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/FusionType/${this.selectedRow.ID}`
            });
        }

        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load() {
        this.isLoading = true;

        this.fusionService
            .getFusionTypes('$orderby=Name')
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                data => {
                    this.fusionTypes = data;
                    this.selectedRow = (this.fusionTypes && this.fusionTypes.length) ? this.fusionTypes[0] : null;

                    this.isLoading = false;
                }
            );
    }

    add() {
        this.newFusionType = new FusionType();
        this.newFusionStyle = new ObjectStyle();
        this.newFusionStyle.IconBackColor = '#000000';
        this.newFusionStyle.IconForeColor = '#ffffff';
        this.formMode = FormMode.Adding;
    }

    edit() {
        this.isLoading = true;
        this.objectStyleService.getObjectStyle(this.selectedRow.ID, 'FusionType')
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
            this.fusionService
                .putFusionType(this.newFusionType, this.newFusionStyle)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    data => {
                        this.showMessageForResult(this.messagesService, data);
                        this.load();
                        this.formMode = FormMode.Default;
                    }
                )
        } else if (this.formMode == FormMode.Adding) {
            this.fusionService
                .postFusionType(this.newFusionType, this.newFusionStyle)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    data => {
                        this.showMessageForResult(this.messagesService, data);
                        this.load();
                        this.formMode = FormMode.Default;
                    }
                );
        }
    }
}


