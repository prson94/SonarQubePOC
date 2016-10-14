import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { ResponsibilityItem, IResponsibilityService } from '../../models/responsibility.model';
import { FormMessage } from '../../models/form.model';
import { ResponsibilityService } from '../../services/responsibility.service';
import { BaseComponent } from '../shared/base.component';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-people-responsibilities-tile',
    templateUrl: './people-responsibilities.tile.html',
    providers: [ResponsibilityService],
})

export class PeopleResponsibilitiesTile extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() title: string = "Responsibilities";
    @Input() showHidden: boolean = false;

    responsibilities = new Array<ResponsibilityItem>();
    selectedRow = new ResponsibilityItem();
    addingRow = new ResponsibilityItem();
    
    private isEditing = false;
    private isDeleting = false;
    private isAdding = false;

    constructor(private responsibilityService: ResponsibilityService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
            }
            if (p == 'objectID') {
                this.objectID = changes['objectID'].currentValue;
            }
        }

        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;
        this.responsibilityService.getResponsibilityDetail(this.objectID, this.objectType)
            .then(data => {
                this.responsibilities = data;
                this.selectedRow = this.responsibilities[0];
                this.isLoading = false;
            });
    }

    edit(id: number): void {        
        this.isEditing = true;
    }

    delete(id: number): void {        
        this.isDeleting = true;
    }

    add(): void {
        this.addingRow = new ResponsibilityItem();
        this.addingRow.ObjectID = this.objectID;
        this.addingRow.ObjectType = this.objectType;
        this.isAdding = true;
    }

    confirmDeleteRow(id: number): void {
        this.isDeleting = false;
        this.load();
    }

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.responsibilities = _.orderBy(this.responsibilities, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
}





