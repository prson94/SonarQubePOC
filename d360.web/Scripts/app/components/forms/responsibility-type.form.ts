///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { ResponsibilityType, IResponsibilityTypeService, ResponsibilityTypeRelation } from '../../models/responsibility-type.model';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { FormMessage, FormHelper } from '../../models/form.model';
import { SelectItem } from 'primeng/primeng';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-responsibility-type-form',
    templateUrl: 'scripts/app/components/forms/responsibility-type.form.html',
    providers: [ResponsibilityTypeService],
})

export class ResponsibilityTypeForm implements OnInit {
    @Input() id: number;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter(); 
    @Output() onCancel = new EventEmitter();

    private isLoading = false;
    private item: ResponsibilityType;

    private selectedAllocations: string[] = [];

    constructor(private responsibilityTypeService: ResponsibilityTypeService) {

    }

    ngOnInit() {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
            }
        }
    }

    load(): void {
        this.isLoading = true;
        this.responsibilityTypeService.getResponsibilityType(this.id)
            .then(r => {
                this.item = r;
                this.getSelectedAllocations();
                this.isLoading = false;
            });
    }

    save(): void {
        this.isLoading = true;
        this.getTypeRelations();
        if (this.id == 0) {
            this.responsibilityTypeService.postResponsibilityType(this.item)
                .then(d => {
                    //console.log(d);
                    this.isLoading = false;
                    this.onSaveComplete.emit(null);
                });
        } else {
            this.responsibilityTypeService.putResponsibilityType(this.item)
                .then(d => {
                    //console.log(d);
                    this.isLoading = false;
                    this.onSaveComplete.emit(null);
                });
        }

    }

    cancel(): void {
        this.onCancel.emit(null);
    }

    private getTypeRelations() {
        this.item.ResponsibilityTypeRelations = [];
        if (this.selectedAllocations)
            this.selectedAllocations.forEach(s => {
                let r = new ResponsibilityTypeRelation();
                r.ObjectID = parseInt(s.split('|')[1]);
                r.ObjectType = s.split('|')[0];
                r.ResponsibilityTypeID = this.item.ID;
                this.item.ResponsibilityTypeRelations.push(r);
            });
    }

    private getSelectedAllocations() {
        this.selectedAllocations = [];
        //console.log(this.item);
        if (this.item.ResponsibilityTypeRelations)
            this.item.ResponsibilityTypeRelations.forEach(r => {
                let s = r.ObjectID.toString()
                this.selectedAllocations.push(r.ObjectType + '|' + r.ObjectID.toString());
            });
    }
}
