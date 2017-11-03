import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { ResponsibilityItem, ResponsibilityItemDetail, IResponsibilityService } from '../../../models/responsibility.model';
import { FormMessage } from '../../../models/form.model';
import { ResponsibilityService } from '../../../services/responsibility.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router, ActivatedRoute }       from '@angular/router';

@Component({
    selector: 'd3s-people-responsibilities-tile',
    templateUrl: './people-responsibilities.tile.html',
    providers: [ResponsibilityService],
})

export class PeopleResponsibilitiesTile extends BaseComponent implements OnChanges {
    @Input() assetID: number;
    @Input() title: string = "Responsibilities";

    responsibilities = new Array<ResponsibilityItemDetail>();
    selectedRow = new ResponsibilityItemDetail();
    addingRow = new ResponsibilityItemDetail();

    private isEditing = false;
    private isDeleting = false;
    private isAdding = false;

    constructor(private responsibilityService: ResponsibilityService, private router: Router) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'assetID') {
                this.assetID = changes['assetID'].currentValue;
            }
            //if (p == 'objectID') {
            //    this.objectID = changes['objectID'].currentValue;
            //}
        }

        this.load();
    }

    load(): void {

        if (this.assetID == null)
            return;

        this.isLoading = true;
        this.responsibilityService.getResponsibilityDetail(this.assetID)
            .then(data => {                
                //data.forEach(d => {
                //    d. = SiteUrlHelpers.getObjectUrl(d.ObjectType, d.ObjectID, d.ObjectTypeID);
                //    d.ResponsibleObjectUrl = SiteUrlHelpers.getObjectUrl(d.ResponsibleObjectType, d.ResponsibleObjectID);
                //    d.PrimaryOwnerResourceUrl = SiteUrlHelpers.getObjectUrl('Resource', d.PrimaryOwnerResourceID);
                //});
                this.responsibilities = data;
                this.selectedRow = this.responsibilities[0];
                this.isLoading = false;
                console.log(this.responsibilities);
            });
    }

    edit(id: number): void {        
        this.isEditing = true;
    }

    delete(id: number): void {        
        this.isDeleting = true;
    }

    add(): void {
        this.addingRow = new ResponsibilityItemDetail();
        this.addingRow.AssetID = this.assetID;
        this.isAdding = true;
    }

    confirmDeleteRow(id: number): void {
        this.isDeleting = false;
        this.load();
    }
    
    navigate(url: string) {
        this.router.navigateByUrl(url);
    }

    onDoubleClick(e: any) {
        if (e == null || e.data == null)
            return;

        if (e.data.AssigningItemID == e.data.ObjectID && e.data.AssigningItemType == e.data.ObjectType)
            this.isEditing = true;
    }
}





