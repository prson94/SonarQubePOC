import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { ResourcesService } from '../../services/resources.service';
import { CountObject } from '../../models/resource.model';
import { BaseComponent } from '../shared/base.component';
import { CompanySettingsService } from '../../services/settings.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-following-tile',    
    template: `
                <header *ngIf="isMe">
                    Items You Follow
                    <d3s-tile-actions hasExport="true" (exportClick)="export()"></d3s-tile-actions>    
                </header>
                <header *ngIf="!isMe">
                    Items {{resource?.FirstName}} Follows
                    <d3s-tile-actions hasExport="true" (exportClick)="export()"></d3s-tile-actions>      
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>      
                <div *ngIf="!isLoading" class="row">
                    <div class="col l3 s12 relationship-container">
                        <div class="row relationship" *ngFor="let r of items; let i = index" [ngClass]="{'active' : isSelected(r)}" (click)="select(r)">
                            <div class="col s10 name" [title]="r.Type | technicalNameToDisplayValue">{{r.TypeName}}</div>
                            <div class="col s2 count center" [ngClass]="{'empty-count': r.Count == 0, 'count': r.Count != 0}">{{r.Count}}</div>
                        </div>                        
                    </div>
                    <div class="col l9 s12">       
                        <d3s-resource-following-grid-tile *ngIf="selected != null" [simpleFilter]="showFilter" [resourceId]="resourceId" [objectType]="selected.Type" [objectId]="selected.TypeID"></d3s-resource-following-grid-tile>
                    </div>                    
                </div>
`
    ,
    providers: [ResourcesService]
})

export class ResourceFollowingTile extends BaseComponent implements OnChanges {
    @Input() resourceId: any = 0;
    @Input() resource: any = null;
    private itemsres: any[] = [];
    private items: CountObject[] = new Array<CountObject>();
    private selected: CountObject;

    showFilter = true;
    isLoading = false;
    isMe = false;

    constructor(
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }
    
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
            this.resourceId = this.resource.ResourceID;

        this.isMe = (this.resourceId == CurrentResourceID);

        this.resourcesService.getFollowingBreakdownByResource(this.resourceId)
            .subscribe(r => {
                this.items = r;
                if (this.items && this.items.length > 0)
                    this.select(this.items[0]);

                if (this.resource == null)
                    this.resourcesService.getResource(this.resourceId)
                        .subscribe(res => {
                            this.itemsres = res.items;
                            if (this.itemsres.length > 0) {
                                this.resource = this.itemsres[0];
                            }
                            this.isLoading = false;
                        });
                else
                    this.isLoading = false;
            });
    }

    export() {
        this.resourcesService.exportFollowingByResourceByType(this.resourceId, this.selected.Type, this.selected.TypeID);
    }
}