///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { NgSwitch, NgSwitchDefault, NgSwitchCase } from '@angular/common';
import { Column, DataTable, InputText, Button, SelectItem, Dropdown } from 'primeng/primeng';
import { FusionFilter, FusionAttributeType } from '../../models/fusion.model';
import { FusionService } from '../../services/fusion.service';
import { TileActionsComponent } from './tile-actions.component';
import { DeleteForm } from '../forms/delete.form';
import { FormMode, FormHelper } from '../../models/form.model';

@Component({
    selector: 'd3s-fusion-filters-tile',
    directives: [
        DataTable,
        Column,
        TileActionsComponent,
        NgSwitch,
        NgSwitchDefault,
        NgSwitchCase,
        DeleteForm,
        InputText,
        Button,
        Dropdown,
    ],
    templateUrl: 'scripts/app/components/tiles/fusion-filters.tile.html',
    providers: [FusionService]
})

export class FusionFiltersTile implements OnChanges {
    @Input() fusionTypeID: number;
    @Input() fusionID: number;
    @Input() title: string = 'Synchronization Filters';

    isLoading = false;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    errorMessage = "";

    fusionFilters: FusionFilter[];
    selectedRow: FusionFilter;
    newFilter: FusionFilter;

    fusionAttributeTypes: FusionAttributeType[];
    fusionTypeList: SelectItem[];
    selectedFusionType: string;

    constructor(private fusionService: FusionService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        console.log('ngOnChanges');
        for (let p in changes) {
            if (p == 'fusionTypeID' || p == 'fusionID') {
                this.load();
            }

        }
    }

    load(): void {
        this.isLoading = true;
        this.errorMessage = "";
        if (this.fusionTypeID == null || this.fusionID == null) {
            this.formMode = FormMode.Default;
            this.fusionFilters = null;
            this.selectedRow = null;
            this.newFilter = null;
            this.isLoading = false;
            return;
        }
        this.fusionService.getFusionConfigurationFilters(this.fusionTypeID, this.fusionID)
            .then(data => {
                console.log(data);
                this.fusionFilters = data;
                this.selectedRow = this.fusionFilters[0];
                this.isLoading = false;
            }).then(() =>
                this.fusionService.getFusionAttributeTypeList(this.fusionID))
            .then(data => {
                this.fusionAttributeTypes = data;
                console.log(data);
                this.fusionTypeList = FormHelper.getSelectList(this.fusionAttributeTypes,'Name','ID');
                console.log(this.fusionAttributeTypes);
            });
    }

    edit() {
        this.newFilter = new FusionFilter();
        this.newFilter.Filter = this.selectedRow.Filter;
        this.formMode = FormMode.Editing;
    }

    add() {
        this.newFilter = new FusionFilter();
        this.newFilter.FusionID = this.fusionID;
        this.newFilter.FusionAttributeTypeID = this.fusionTypeID;
        this.formMode = FormMode.Adding;
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {
        if (this.formMode == FormMode.Editing) {
            this.selectedRow.Filter = this.newFilter.Filter;
            this.fusionService.putFusionConfigurationFilter(this.selectedRow)
                .then(data => {
                    this.formMode = FormMode.Default;
                    this.load();
                });
        } else if (this.formMode == FormMode.Adding) {
            try {
                this.newFilter.FusionID = this.fusionID;
                this.newFilter.FusionAttributeTypeID = parseInt(this.selectedFusionType);

            } catch (e) {
                this.errorMessage = 'An error occured while attempting to add the filter';
            }
            
            this.fusionService.postFusionConfigurationFilter(this.newFilter)
                .then(data => {
                    this.formMode = FormMode.Default;
                    this.load();
                });
        }
    }
}