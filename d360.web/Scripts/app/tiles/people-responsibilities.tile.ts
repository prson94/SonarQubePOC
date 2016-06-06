///<reference path="../es6-shim.d.ts"/>
import {Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';
import { DataTable, DataTableDirectives } from 'angular2-datatable/datatable';
import { ResponsibilityItem } from '../models/responsibility.model';
import { ResponsibilityItemForm } from '../forms/responsibility-item.form';
import { FormMessage } from '../models/form.model';
import { DeleteForm } from '../forms/delete.form';

@Component({
    selector: 'people-responsibilities-tile',
    directives: [DataTableDirectives, ResponsibilityItemForm, DeleteForm ],//, DeleteGeneric],
    templateUrl: 'scripts/app/tiles/people-responsibilities.tile.html',
    viewProviders: [HTTP_PROVIDERS],
    styles: [`
    .selected {
        background-color: #86ccf9;        
    }
    tbody tr:not(.selected):not(.inline-edit):hover {
        background-color: #ddd;
    }
    td {
        padding-left:3px; 
    }
    `]
})

export class PeopleResponsibilitiesTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: string;
    @Input() title: string;
    @Input() showHidden: boolean = false;

    private responsibilities = new Array<ResponsibilityRowItem>();
    private selectedRow = new ResponsibilityRowItem();
    private addingRow = null;
    private isLoading = false;

    private deleteIsLoading = false;

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
                this.selectedRow = null; //this.responsibilities[0];

                this.isLoading = false;
            });

    }

    selectRow(id: number): void {
        this.selectedRow = this.responsibilities[this.responsibilities.findIndex(d => d.ResponsibilityID == id)];
    }

    editRow(id: number): void {
        var row = this.responsibilities.find(w => w.ResponsibilityID == id);

        this.responsibilities.forEach(w => {
            w.isDeleting = false;
            if (w.ResponsibilityID == id)
                w.isEditing = true;
            else
                w.isEditing = false;
        });

    }

    deleteRow(id: number): void {
        var row = this.responsibilities.find(w => w.ResponsibilityID == id);

        this.responsibilities.forEach(w => {
            w.isEditing = false;
            if (w.ResponsibilityID == id)
                w.isDeleting = true;
            else
                w.isDeleting = false;
        });
    }

    confirmDeleteRow(id: number): void {

        this.load();
        //var row = this.responsibilities.find(r => r.ResponsibilityID == id);
        //if (!row)
        //    return;
        //this.load();

        //this.deleteIsLoading = true;
        //var headers = new Headers();
        //headers.append('Content-Type', 'application/json');

        //this.http.delete('/form/DeleteResponsibilityByID?id=' + id)
        //    .map(data => data.json())
        //    .subscribe(
        //    s => {
        //        //console.log(s);
        //        this.deleteIsLoading = false;
        //        row.isDeleting = false;
        //        //responsibilities.remove(row) instead if success?
        //        this.load();
        //    }
        //);
    }

    updateRow(event: any): void {
        console.log(event);
        var message = event.message;
        var item = event.item;
        var initialItem = event.initialItem;

        if (message.isSuccess)
            item.isEditing = false;

    }

    addRow(): void {
        if (this.addingRow)
            return;
        this.addingRow = new ResponsibilityRowItem();
        this.addingRow.ResponsibilityID = -1;
        this.addingRow.ObjectID = 1;
        this.addingRow.Visible = true;
        this.addingRow.ObjectType = "DomainType";
        this.addingRow.isEditing = true;
    }

    confirmAddRow(event) {
        //console.log(event);
        var message = event.message;

        if (message.isSuccess) {
            this.addingRow = null;
            this.load();
        }
    }

}


class ResponsibilityRowItem extends ResponsibilityItem {
    isEditing = false;
    isDeleting = false;
}





