import { Component, NgZone, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { MapsService } from '../../../services/maps.service';
import { MapType } from '../../../models/map.model';

@Component({
    selector: 'd3s-admin-maps-editor',
    providers: [MapsService],
    template: 
`
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <header>{{isAdding ? 'Add' : 'Edit'}} Map Type</header>
    <div class="row">
        <div class="col s12">
            <div class="FieldName">
                Name
            </div>
            <div>
                <input type="text" [(ngModel)]="mapType.Name"  style="width: 100%"/>
            </div>
        </div>
        <div class="col s12">
            <div class="FieldName">
                Description
            </div>
            <div>
                <p-editor [(ngModel)]="mapType.Description"></p-editor>
            </div>
        </div>
        <div class="col s12" *ngIf="!isAdding">
            <div class="FieldName">
                Relationship Priority
            </div>
            <div style="margin:0 auto; width: 50%">
                <ng-container *ngIf="intersectTypes != null && intersectTypes.length > 0; else noIntersects">
                    <p-orderList [value]="intersectTypes" dragDrop="true" (onReorder)="reorder()" [responsive]="true">
                        <ng-template let-item pTemplate="item">
                            <div style="border-bottom: 1px solid #eee">{{item.ObjectName}}</div>
                        </ng-template>
                    </p-orderList>
                </ng-container>
                <ng-template #noIntersects>
                    There are no relationships to this map type
                </ng-template>
            </div>
        </div>
        <div class="col s12" style="padding-top: 10px;">
            <button pButton label="Save" (click)="save()" [disabled]="!valid()"></button>
            <button pButton label="Cancel" (click)="onCancel.emit()"></button>
        </div>
    </div>
</div>

`
})

export class AdminMapsEditorComponent extends BaseComponent implements OnInit {
    @Input() mapTypeId: number;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    mapType: MapType;
    intersectTypes: any[] = [];
    selectedIntersectType: any;
    isAdding: boolean = false;

    constructor(
        private mapsService: MapsService,
        protected messagesService: MessagesService) {
        super();

    }

    ngOnInit() {
        this.load();
    }

    load() {
        if (this.mapTypeId == null || this.mapTypeId < 1) {
            this.isAdding = true;
            this.mapType = new MapType();
            return;
        }

        this.isLoading = true;
        this.mapsService.getMapType(this.mapTypeId)
            .then(r => {
                console.log(r);
                this.mapType = r;
            })
            .then(() => this.mapsService.getMapTypeIntersectTypes(this.mapTypeId))
            .then(r => {
                console.log(r);
                this.intersectTypes = r;
                this.isLoading = false;
            });
    }

    save() {
        this.mapType.MapTypeOrders = [];

        this.intersectTypes.forEach(i => {
            this.mapType.MapTypeOrders.push({
                MapTypeID: this.mapType.ID,
                IntersectTypeID: i.ID,
                Order: i.Order
            });
        });

        this.isLoading = true;
        if (this.isAdding)
        {
            this.mapsService.addMapType(this.mapType)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    this.onSave.emit();
                });
        }
        else
        {
            this.mapsService.editMapType(this.mapType)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    this.onSave.emit();
                });
        }
    }

    valid() {
        if (this.mapType == null)
            return false;
        if (this.mapType.Name == null || this.mapType.Name.length < 1)
            return false;

        return true;
    }

    reorder() {
        for (let i = 0; i < this.intersectTypes.length; i++)
        {
            this.intersectTypes[i].Order = i + 1;
        }
        console.log(this.intersectTypes);
    }

}


