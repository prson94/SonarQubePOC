import { Input, Output, Component, OnInit, EventEmitter, SimpleChange } from '@angular/core';
import { ResponsibilityType, ResponsibilityTypeRelation } from '../../../models/responsibility-type.model';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import * as _ from 'lodash';
import '@angular/localize/init';

@Component({
    selector: 'd3s-responsibility-type-form',
    templateUrl: './responsibility-type.form.html',
    providers: [ResponsibilityTypeService],
})

export class ResponsibilityTypeForm implements OnInit {
    @Input() id: number;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    isLoading = true;
    private item: ResponsibilityType = new ResponsibilityType();

    private selectedAllocations: string[] = [];

    saveLabel = $localize`Save`;
    cancelLabel = $localize`Cancel`;

    constructor(private responsibilityTypeService: ResponsibilityTypeService) {

    }

    ngOnInit() {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let reloadRequired = false;
        for (let p in changes) {
            if (p == 'id') {
                if (changes[p].currentValue == 0) {
                    this.load();
                }
                if (changes[p].currentValue && (changes[p].currentValue != changes[p].previousValue)) {
                    reloadRequired = true;
                }
                if (reloadRequired)
                    this.load();
            }
        }
    }

    load(): void {
        this.isLoading = true;
        this.responsibilityTypeService.getResponsibilityType(this.id)
            .subscribe(r => {
                this.item = r;
                this.getSelectedAllocations();
                this.isLoading = false;
            });
    }

    save(): void {
        this.isLoading = true;
        this.getTypeRelations();

        //avoid sending this back to the server
        this.item.AllocationsList = null;

        if (this.id == 0) {
            this.responsibilityTypeService.postResponsibilityType(this.item)
                .subscribe(d => {
                    this.isLoading = false;
                    this.onSaveComplete.emit(d);
                });
        } else {
            this.responsibilityTypeService.putResponsibilityType(this.item)
                .subscribe(d => {
                    this.isLoading = false;
                    this.onSaveComplete.emit(d);
                });
        }

    }

    cancel(): void {
        this.onCancel.emit(null);
    }

    private getTypeRelations() {
        this.item.ResponsibilityTypeRelations = [];
        if (this.selectedAllocations)
            this.selectedAllocations.forEach((s) => {
                let r = new ResponsibilityTypeRelation();
                r.ObjectID = parseInt(s.split('|')[1]);
                r.ObjectType = s.split('|')[0];
                r.ResponsibilityTypeID = this.item.ID;
                this.item.ResponsibilityTypeRelations.push(r);
            });
    }

    private getSelectedAllocations() {
        this.selectedAllocations = [];
        if (this.item.ResponsibilityTypeRelations)
            this.item.ResponsibilityTypeRelations.forEach(r => {
                let s = r.ObjectID.toString()
                this.selectedAllocations.push(r.ObjectType + '|' + r.ObjectID.toString());
            });
    }

    private isValid() {
        let valid = true;
        if (!this.item.Name || this.item.Name.length <= 0 || this.item.Name.length > 250) {
            valid = false;
        }
        if (this.item.Description && this.item.Description.length > 4000) {
            valid = false;
        }
        if (this.selectedAllocations.length < 1) {
            valid = false;
        }

        return valid
    }
}
