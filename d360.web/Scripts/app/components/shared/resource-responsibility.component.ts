import { Component, Input, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { ResourcesService } from '../../services/resources.service';
import { CountObject } from '../../models/resource.model';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-responsibility-tile',
    template: `
                <header *ngIf="isMe">
                    <ng-container i18n>Items You Own</ng-container>
                    <d3s-tile-actions [hasExport]="true" (exportClick)="export()" hasFilterMode="true" [(filterMode)]="showFilter"></d3s-tile-actions> 
                </header>
                <header *ngIf="!isMe">
                    <ng-container i18n>Items {{resource?.FirstName}} Owns</ng-container>
                    <d3s-tile-actions [hasExport]="true" (exportClick)="export()" hasFilterMode="true" [(filterMode)]="showFilter"></d3s-tile-actions> 
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
                        <d3s-resource-responsibility-grid-component 
                            *ngIf="selected != null" 
                            [simpleFilter]="showFilter" 
                            [type]="'resources'" 
                            [Id]="resourceId" 
                            [objectType]="selected.Type" 
                            [objectId]="selected.TypeID" 
                            [responsibilityTypeId]="responsibilityTypeId">
                        </d3s-resource-responsibility-grid-component>
                    </div>                    
                </div>
            `
    ,
    providers: [ResourcesService]
})

export class ResourceResponsibilityComponent implements OnChanges {
    @Input() responsibilityTypeUid: string = "";
    @Input() resourceId: any = 0;
    @Input() resource: any = null;
    private itemsres: any[] = [];
    private items: CountObject[] = new Array<CountObject>();
    private selected: CountObject;
    isLoading = false;
    isMe = false;
    showFilter: boolean = true;

    constructor(private resourcesService: ResourcesService) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['resourceId'] && this.resourceId > 0) this.resource = null;
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

        this.resourcesService.getResponsibilityBreakdownByResource(this.resourceId, this.responsibilityTypeUid)
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
        this.resourcesService.exportResponsibilitiesByResourceByType(this.resourceId, this.selected.Type, this.selected.TypeID, this.responsibilityTypeUid);
    }
}