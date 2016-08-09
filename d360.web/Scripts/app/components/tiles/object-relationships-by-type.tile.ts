///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { RelationshipsService } from '../../services/index';
import {DataTable, Column} from 'primeng/primeng';
import { ObjectRelationshipCount } from '../../models/relationship.model';


@Component({
    selector: 'd3s-object-relationships-by-type-tile',
    directives: [DataTable, Column],
    providers: [RelationshipsService],  
    template: `
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>    
                <p-dataTable *ngIf="!isLoading" [value]="relationships" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data" [(selection)]="selected" >                                                                        
                    <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                                                
                </p-dataTable> 
                `,
})

export class ObjectRelationshipsByTypeTile extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() targetType: string;
    @Input() targetTypeID: number;

    relationships: any[];
    selected: any;

    
    constructor(protected relationshipsService: RelationshipsService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null || this.targetType == null || this.targetTypeID == null )
            return;

        this.isLoading = false;

    
    }
    
}
