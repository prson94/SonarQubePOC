import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { ResponsibilityItem, ResponsibilityItemDetail, IResponsibilityService } from '../../../models/responsibility.model';
import { FormMessage } from '../../../models/form.model';
import { ResponsibilityService } from '../../../services/responsibility.service';
import { PermissionsService } from '../../../services/permissions.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router, ActivatedRoute } from '@angular/router';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-people-responsibilities-tile',
    templateUrl: './people-responsibilities.tile.html',
    providers: [ResponsibilityService, PermissionsService],
})

export class PeopleResponsibilitiesTile extends BaseComponent implements OnChanges {
    @Input() assetID: number;
    @Input() overrideItemID: number;
    @Input() title: string = "Responsibilities";

    responsibilities = new Array<ResponsibilityItemDetail>();
    selectedRow = new ResponsibilityItemDetail();
    addingRow = new ResponsibilityItemDetail();

    private isEditing = false;
    private isDeleting = false;
    private isAdding = false;

    constructor(private responsibilityService: ResponsibilityService, private permissionsService: PermissionsService, private router: Router) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'assetID') {
                this.assetID = changes['assetID'].currentValue;
            }            
            if (p == 'overrideItemID') {
                this.overrideItemID = changes['overrideItemID'].currentValue;
            }            
        }

        this.load();
    }

    load(): void {

        if (this.assetID == null)
            return;

        this.isLoading = true;
        
        this.responsibilityService.getResponsibilityDetail(this.assetID)
            .subscribe(data => {  
                this.responsibilities = data;
                this.selectedRow = this.responsibilities[0];
                this.isLoading = false;                
            });
        this.loadPermissionsById(this.permissionsService, this.assetID);
    }

    edit(item): void {  
        this.selectedRow = item;
        this.isEditing = true;
    }

    delete(id: number): void {        
        this.isDeleting = true;
    }

    add(): void {
        this.addingRow = new ResponsibilityItemDetail();
        this.addingRow.AssetID = this.assetID;
        //this.addingRow.OverrideID = this.overrideItemID;
        this.isAdding = true;
    }

    confirmDeleteRow(id: number): void {
        this.isDeleting = false;
        this.load();
    }
    
    navigate(url: string) {
        this.router.navigateByUrl(url);
    }

    onDoubleClick(item: any) {
        if (item == null)
            return;

        if (item.AssigningItemID == item.ObjectID && item.AssigningItemType == item.ObjectType)
            this.isEditing = true;
    }

    private columnSort(event) {
        console.log(event);
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.responsibilities = _.orderBy(this.responsibilities, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }    
}