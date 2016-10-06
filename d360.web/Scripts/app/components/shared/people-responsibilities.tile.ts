
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { ResponsibilityItem, IResponsibilityService } from '../../models/responsibility.model';
import { FormMessage } from '../../models/form.model';
import { ResponsibilityService } from '../../services/responsibility.service';
import { BaseComponent } from '../shared/base.component';

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
        //this.selectedRow = this.responsibilities.find(r => r.ID == id);
        this.isEditing = true;
    }

    delete(id: number): void {
        //this.selectedRow = this.responsibilities.find(r => r.ID == id);
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
}





