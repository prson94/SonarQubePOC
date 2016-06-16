///<reference path="../../es6-shim.d.ts"/>
import {Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';
import { ResponsibilityItem } from '../../models/responsibility.model';
import { ResponsibilityItemForm } from '../forms/responsibility-item.form';
import { FormMessage } from '../../models/form.model';
import { DeleteForm } from '../forms/delete.form';
import { DataTable, Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-people-responsibilities-tile',
    directives: [DataTable, Column, ResponsibilityItemForm, DeleteForm],//, DeleteGeneric],
    templateUrl: 'scripts/app/components/tiles/people-responsibilities.tile.html',
    viewProviders: [HTTP_PROVIDERS],
    styles: []
})

export class PeopleResponsibilitiesTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() title: string;
    @Input() showHidden: boolean = false;

    responsibilities = new Array<ResponsibilityItem>();
    selectedRow = new ResponsibilityItem();
    addingRow = new ResponsibilityItem();
    private isLoading = false;
    private isEditing = false;
    private isDeleting = false;
    private isAdding = false;

    http: Http;

    constructor(http: Http) {
        this.http = http;
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
        this.http.get('/api/' + this.objectType + '/' + this.objectID + '/ownership?showHidden=' + this.showHidden )
            .map(data => data.json())
            .subscribe(data => {
                this.responsibilities = data;
                this.selectedRow = this.responsibilities[0]; //this.responsibilities[0];

                console.log(this.selectedRow);

                this.isLoading = false;
            });

    }

    edit(id: number): void {
        this.selectedRow = this.responsibilities.find(r => r.ResponsibilityID == id);
        this.isEditing = true;
    }

    delete(id: number): void {
        this.selectedRow = this.responsibilities.find(r => r.ResponsibilityID == id);
        this.isDeleting = true;
    }

    add(): void {
        this.addingRow = new ResponsibilityItem();
        //this.addingRow.ResponsibilityID = -1;
        this.addingRow.ObjectID = this.objectID;
        this.addingRow.ObjectType = this.objectType;
        this.isAdding = true;
    }

    confirmDeleteRow(id: number): void {
        this.isDeleting = false;
        this.load();
    }
}





