import { Input, Output, Component, OnChanges, SimpleChange, ChangeDetectorRef } from '@angular/core';
import { ResponsibilityItem, ResponsibilityItemDetail, IResponsibilityService, ResponsibilityItemDetailV2 } from '../../../models/responsibility.model';
import { FormMessage } from '../../../models/form.model';
import { ResponsibilityService } from '../../../services/responsibility.service';
import { PermissionsService } from '../../../services/permissions.service';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router, ActivatedRoute } from '@angular/router';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-people-responsibilities-tile',
    templateUrl: './people-responsibilities.tile.html',
    providers: [ResponsibilityService, PermissionsService],
})

export class PeopleResponsibilitiesTile extends BaseComponent implements OnChanges {
    @Input() assetID: number;
    @Input() assetUid: string;
    @Input() overrideItemID: number;
    @Input() title: string = "Responsibilities";
    @Input() showTitle: boolean = true;
    @Input() showRowTools: boolean = true;

    public deleteCallback: Function;
   
    responsibilities = new Array<ResponsibilityItemDetailV2>();
    selectedRow = new ResponsibilityItemDetailV2();
    addingRow = new ResponsibilityItemDetailV2();

    private isEditing = false;
    private isDeleting = false;
    private isAdding = false;

    constructor(
        private responsibilityService: ResponsibilityService,
        private permissionsService: PermissionsService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private router: Router,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
        this.deleteCallback = this.deleteResponsibility.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'assetID') {
                this.assetID = changes['assetID'].currentValue;
            }
            if (p == 'assetUid') {
                this.assetUid = changes['assetUid'].currentValue;
            } 
            if (p == 'overrideItemID') {
                this.overrideItemID = changes['overrideItemID'].currentValue;
            }            
        }

        this.load();
    }

    load(): void {
        if (this.assetUid == null)
            return;

        this.isLoading = true;
        
        this.responsibilityService.getResponsibilityDetail(this.assetUid)
            .subscribe(data => {
                this.responsibilities = data.filter((x) => x.IsVisible === true);
                this.selectedRow = this.responsibilities[0];
                this.isLoading = false;
                this.ref.markForCheck();
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
        this.addingRow = new ResponsibilityItemDetailV2();        
        this.isAdding = true;
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

    private deleteResponsibility() {

        this.responsibilityService.deleteResponsibility(this.assetUid, this.selectedRow.ResponsibilityUid, this.selectedRow.GroupResourceUid ?? this.selectedRow.ResourceUid).
            subscribe(result => {
                this.isDeleting = false;  
                if (result) {
                    this.showMessageForResult(this.messagesService, result);                  
                }                
                this.load();                            
            });
    }

    private deleteMessage() {
        return `Are you sure you want to delete the ${this.selectedRow.Responsibility} - ${ this.selectedRow.Group ?? this.selectedRow.Resource }?`
    }
}