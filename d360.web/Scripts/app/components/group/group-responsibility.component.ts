import { Component, Input, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { GroupService, ResourcesService } from '../../services/index';
import { CountObject } from '../../models/resource.model';
import { Group } from '../../models/group.model';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-group-responsibility',
    styles: [`
    div.relationship-container{
        max-height: 360px;min-height:200px;
        overflow: auto;
    }
    div.relationship{
        padding:5px 3px 5px 0;
        cursor: pointer; cursor: hand;        
    }
    div.relationship .name{
        text-transform: uppercase;
        color: rgba(84,164,218,1);
        font-weight:bold;
    }
    div.relationship .count, div.relationship .empty-count{
        color:#ffffff;        
        font-weight: bold;
        padding:2px;
        border-radius:3px;
    }
    div.active{
        background:#d3d5d8;
    }
    div.relationship .count{
        background-color: rgba(84,164,218,1);
    }
    div.relationship .empty-count{
        background-color:#646464;
    }

  `],
    template: `
                <header>
                    Items {{group?.Name}} Owns
                    <d3s-tile-actions hasExport="false" hasFilterMode="true" [filterMode]="showFilter" (filterModeChange)="showFilter = !showFilter"></d3s-tile-actions> 
                </header>
                <div *ngIf="!isLoading" class="row">
                    <div class="col l3 s12 relationship-container"><!--left nav-->
                        <div class="row relationship" *ngFor="let r of items; let i = index" [ngClass]="{'active' : isSelected(r)}" (click)="select(r)">
                            <div class="col s10 name" [title]="r.Type | technicalNameToDisplayValue">{{r.TypeName}}</div>
                            <div class="col s2 count center" [ngClass]="{'empty-count': r.Count == 0, 'count': r.Count != 0}">{{r.Count}}</div>
                        </div>                        
                    </div>
                    <div class="col l9 s12">       
                        <d3s-resource-responsibility-grid-component [simpleFilter]="showFilter" *ngIf="selected != null" [Id]="group?.ID" [type]="'groups'" [objectType]="selected.Type" [objectId]="selected.TypeID"></d3s-resource-responsibility-grid-component>
                    </div>                    
                </div>
`
    ,
    providers: [GroupService, ResourcesService]
})

export class GroupResponsibilityComponent extends BaseComponent implements OnChanges  {    
    @Input() group: Group = null;
    private items: CountObject[] = new Array<CountObject>();
    private selected: CountObject;
    private showFilter: boolean = true;
    
    constructor(private groupService: GroupService) { super();}
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['group'] && this.group)
            this.load();
    }
    
    isSelected(item: any) {
        return (item == this.selected);
    }

    select(item: any) {
        this.selected = item;
    }

    load() {
        this.isLoading = true;

        this.groupService.getResponsibilityBreakdownByGroup(this.group.ID)
            .then(r => {
                this.items = r;
                if (this.items && this.items.length > 0)
                    this.select(this.items[0]);

                this.isLoading = false;
            });
    }
    

}