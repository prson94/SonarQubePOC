///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { NgSwitch, NgSwitchDefault, NgSwitchCase } from '@angular/common';
import { Column, DataTable } from 'primeng/primeng';
import { FusionFilter } from '../../models/fusion.model';
import { FusionService } from '../../services/fusion.service';
import { TileActionsComponent } from './tile-actions.component';
import { DeleteForm } from '../forms/delete.form';
import { FormMode } from '../../models/form.model';

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

    fusionFilters: FusionFilter[];
    selectedRow: FusionFilter;

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
        if (this.fusionTypeID == null || this.fusionID == null) {
            this.formMode = FormMode.Default;
            this.fusionFilters = null;
            this.selectedRow = null;
            this.isLoading = false;
            return;
        }
        this.fusionService.getFusionConfigurationFilters(this.fusionTypeID, this.fusionID)
            .then(data => {
                console.log(data);
                this.fusionFilters = data;
                this.selectedRow = this.fusionFilters[0];
                this.isLoading = false;
            });
    }
}