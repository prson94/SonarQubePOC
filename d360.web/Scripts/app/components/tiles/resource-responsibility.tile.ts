
import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { ResourcesService } from '../../services/index';
import { Resource, CountObject } from '../../models/resource.model';


declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-responsibility-tile',
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
                <header *ngIf="isMe">
                    Items You Own
                    <d3s-tile-actions [hasExport]="true" (exportClick)="export()" hasFilterMode="true" (filterModeChange)="showFilter = !showFilter"></d3s-tile-actions> 
                </header>
                <header *ngIf="!isMe">
                    Items {{resource?.FirstName}} Owns
                    <d3s-tile-actions [hasExport]="true" (exportClick)="export()" hasFilterMode="true" (filterModeChange)="showFilter = !showFilter"></d3s-tile-actions> 
                </header>
                <div *ngIf="!isLoading" class="row">
                    <div class="col l3 s12 relationship-container"><!--left nav-->
                        <div class="row relationship" *ngFor="let r of items; let i = index" [ngClass]="{'active' : isSelected(r)}" (click)="select(r)">
                            <div class="col s10 name" [title]="r.Type | technicalNameToDisplayValue">{{r.TypeName}}</div>
                            <div class="col s2 count center" [ngClass]="{'empty-count': r.Count == 0, 'count': r.Count != 0}">{{r.Count}}</div>
                        </div>                        
                    </div>
                    <div class="col l9 s12">       
                        <d3s-resource-responsibility-grid-component *ngIf="selected != null" [simpleFilter]="showFilter" [type]="'resources'" [Id]="resourceId" [objectType]="selected.Type" [objectId]="selected.TypeID"></d3s-resource-responsibility-grid-component>
                    </div>                    
                </div>
`
    ,
    providers: [ResourcesService]
})

export class ResourceResponsibilityTile implements OnInit, OnChanges {
    @Input() resourceId: any = 0;
    @Input() resource: Resource = null;
    private items: CountObject[] = new Array<CountObject>();
    private selected: CountObject;
    isLoading = false;
    isMe = false;
    showFilter = true;

    constructor(private resourcesService: ResourcesService) { }

    ngOnInit() { }

    ngOnChanges() {
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

        if (this.resource != null)
            this.resourceId = this.resource.ID;

        this.isMe = (this.resourceId == CurrentResourceID);

        this.resourcesService.getResponsibilityBreakdownByResource(this.resourceId)
            .then(r => {
                this.items = r;
                if (this.items && this.items.length > 0)
                    this.select(this.items[0]);

                if (this.resource == null)
                    this.resourcesService.getResource(this.resourceId)
                        .then(res => {
                            this.resource = res;
                            this.isLoading = false;
                        });
                else
                    this.isLoading = false;
            });
    }

    export() {
        this.resourcesService.exportResponsibilitiesByResourceByType(this.resourceId, this.selected.Type, this.selected.TypeID);
    }

}