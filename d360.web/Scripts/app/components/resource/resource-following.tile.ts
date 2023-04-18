import { Component, Input, OnChanges } from '@angular/core';
import { ResourcesService } from '../../services/resources.service';
import { CountObject } from '../../models/resource.model';
import { BaseComponent } from '../shared/base.component';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-resource-following-tile',    
    templateUrl: './resource-following.tile.html',
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
        return (item === this.selected);
    }

    select(item: any) {
        this.selected = item;
    }

    load() {
        this.isLoading = true;

        if (this.resource != null)
            {this.resourceId = this.resource.ResourceID;}

		this.isMe = (this.resourceId === this.settingsService.CurrentResourceID);

        this.resourcesService.getFollowingBreakdownByResource(this.resourceId)
            .subscribe((r) => {
                this.items = r;
                if (this.items && this.items.length > 0)
                    {this.select(this.items[0]);}

                if (this.resource == null)
                    {this.resourcesService.getResource(this.resourceId)
                        .subscribe((res) => {
                            this.itemsres = res.items;
                            if (this.itemsres.length > 0) {
                                this.resource = this.itemsres[0];
                            }
                            this.isLoading = false;
                        });}
                else
                    {this.isLoading = false;}
            });
    }

    export() {
        this.resourcesService.exportFollowingByResourceByType(this.resourceId, this.selected.Type, this.selected.TypeID);
    }
}