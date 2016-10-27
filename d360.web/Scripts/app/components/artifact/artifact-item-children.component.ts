import { Input, Component, OnInit } from '@angular/core';
import { BaseComponent} from '../shared/base.component';
import { ObjectStatisticsService } from '../../services/index';
import { ObjectStatistics, ObjectStatisticChildItem } from '../../models/object-statistics.model';

@Component({
    selector: 'd3s-artifact-item-children',    
    template: `                 
                <header>Children of {{objectName}}</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading" class="row">
                    <div class="col l3 s12 child-container"><!--left nav-->
                        <div class="row child" *ngFor="let child of children" [ngClass]="{'active' : selected==child}" (click)="selected=child;">
                                <div class="col s10 name">{{child.Name}}</div>
                                <div class="col s2 count center">{{child.Count}}</div>
                        </div>                                                
                    </div>
                    <div class="col l9 s12">     
                        <d3s-artifact-item-child-grid [parentId]="objectID" [artifactTypeId]="selected?.TypeID"></d3s-artifact-item-child-grid>
                    </div>                    
                </div>
                `,
    providers: [ObjectStatisticsService],
})

export class ArtifactItemChildrenComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;

    private children: ObjectStatisticChildItem[] = [];
    private selected: ObjectStatisticChildItem;

    constructor(protected objectStatisticsService: ObjectStatisticsService) {
        super();
    }

    ngOnInit() {        
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.objectStatisticsService.getObjectStatistics(this.objectID, this.objectType)
            .then(res => {
                this.children = res.Items;                
                this.selected = this.children.length > 0 ? this.children[0] : null;
                this.isLoading = false;
            });
    }    
};