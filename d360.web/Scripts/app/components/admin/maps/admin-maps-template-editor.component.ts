import { Component, NgZone, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { MessagesService } from '../../../services/messages.service';
import { MapsService } from '../../../services/maps.service';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { FormMode } from '../../../models/form.model';
import { MapTypeTemplate, MapTypeTemplateItem } from '../../../models/map.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-maps-template-editor',
    providers: [MapsService],
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <header>{{isAdding ? 'Add' : 'Edit'}} Map Type Template</header>
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Name
            </div>
            <div>
                <input type="text" [(ngModel)]="template.Name"  style="width: 100%"/>
            </div>
        </div>
        <div class="col s12" style="padding-top: 15px">
            <p-pickList [source]="availableIntersects" [target]="selectedIntersects" [showSourceControls]="false" [showTargetControls]="false" sourceHeader="Available Types" targetHeader="Template Types" (onMoveToTarget)="onMove()" (onMoveToSource)="onMove()" [responsive]="true">
                <ng-template let-item pTemplate="item">
                    <div style="border-bottom:1px solid gray">
                        {{item.ObjectName}}
                        <ng-container *ngIf="item.isTarget != null && item.isTarget == true">
                            <input type="checkbox"  [(ngModel)]="item.isRequired"/> Required?
                        </ng-container>
                    </div>
                </ng-template>
            </p-pickList>
        </div>
        <div class="col s12" style="padding-top: 10px;">
            <button pButton label="Save" (click)="save()" [disabled]="!valid()"></button>
            <button pButton label="Cancel" (click)="onCancel.emit()"></button>
        </div>
    </div>
</div>
`
})

export class AdminMapsTemplateEditorComponent extends BaseComponent implements OnInit {
    @Input() mapTypeTemplateId: number = null;
    @Input() mapTypeId: number = null;
    @Output() onSave = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    template: MapTypeTemplate;
    intersectTypes: any[] = [];

    availableIntersects: any[] = [];
    selectedIntersects: any[] = [];

    isAdding: boolean = false;

    constructor(private mapsService: MapsService,
    protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {

        if (this.mapTypeTemplateId == null || this.mapTypeTemplateId < 1) {
            this.isAdding = true;
            this.isLoading = true;
            this.template = new MapTypeTemplate();
            this.template.MapTypeID = this.mapTypeId;
            this.mapsService.getMapTypeIntersectTypes(this.mapTypeId)
                .then(r => {
                    this.intersectTypes = r;
                    this.availableIntersects = _.cloneDeep(this.intersectTypes);
                    this.selectedIntersects = [];
                    this.isLoading = false;
                });
        } else {
            this.isLoading = true;
            this.mapsService.getMapTypeTemplate(this.mapTypeTemplateId)
                .then(r => {
                    this.template = r;
                })
                .then(() => this.mapsService.getMapTypeIntersectTypes(this.mapTypeId))
                .then(r => {
                    this.intersectTypes = r;
                    this.availableIntersects = _.cloneDeep(this.intersectTypes);
                    this.selectedIntersects = [];
                    this.template.Items.forEach(i => {
                        let intersect = this.intersectTypes.find(t => t.ID == i.IntersectTypeID);
                        if (intersect != null) {
                            this.selectedIntersects.push(intersect);
                            let x = this.availableIntersects.findIndex(a => a.ID == intersect.ID);
                            if (x > -1) {
                                this.availableIntersects.splice(x, 1);
                            }
                        }
                    });
                    console.log(this.intersectTypes, this.availableIntersects, this.selectedIntersects, this.template);
                    this.isLoading = false;
                });
        }
    }

    save() {
        this.isLoading = true;

        this.template.Items = [];

        this.selectedIntersects.forEach(i => {
            let item = new MapTypeTemplateItem();
            item.IntersectTypeID = i.ID;
            item.IsRequired = i.isRequired == null ? false : i.isRequired;
            item.MapTypeTemplateID = this.template.ID;

            this.template.Items.push(item);
        });


        if (this.isAdding) {
            this.mapsService.addMapTypeTemplate(this.template)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    this.onSave.emit();
                })
        } else {
            this.mapsService.editMapTypeTemplate(this.template)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    this.onSave.emit();
                });
        }

    }
    valid() {
        if (this.template.Name == null || this.template.Name.length < 1)
            return false;

        return true;
    }

    onMove() {
        this.availableIntersects.sort((a, b) => this.sortIntersectItems(a, b));
        this.selectedIntersects.sort((a, b) => this.sortIntersectItems(a, b));
        this.availableIntersects.forEach(a => {
            a.isTarget = false;
        });
        this.selectedIntersects.forEach(a => {
            a.isTarget = true;
        })

    }

    sortIntersectItems(a: any, b: any) {
        let orderA = (a == null) ? 99999 : (a.Order == null ? 99999 : a.Order);
        let orderB = (b == null) ? 99999 : (b.Order == null ? 99999 : b.Order);

        if (orderA < orderB) return -1;
        if (orderA > orderB) return 1;

        if (orderA == orderB) {
            if (a != null && a.ObjectName != null && b != null && b.ObjectName != null) {
                if (a.ObjectName < b.ObjectName) return -1;
                if (a.ObjectName > b.ObjectName) return 1;
                if (a.ObjectName == b.ObjectName) return 0;
            } else
                return 0;
        }

    }
}


