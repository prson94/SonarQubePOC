import { Input, Output, Component, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { FusionConfiguration, FusionType, FusionFilter } from '../../../models/fusion.model';
import { FusionService } from '../../../services/fusion.service';
import { GridColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
 
@Component({
    selector: 'd3s-fusion-configuration-schedule-tile',
    templateUrl: './fusion-configuration-schedule.tile.html',
    providers: [FusionService]
})

export class FusionConfigurationScheduleTile extends BaseComponent implements OnChanges {
    @Input() fusionID: number;
    @Input() title: string = 'Agent Execution Schedule';
    @Output() onClose = new EventEmitter();

    formMode: FormModeConfig = FormModeConfig.Default;
    FormModeConfig = FormModeConfig;

    fusionConfigurationSchedules: any[];
    selectedRow: any;

    constructor(private router: Router, private fusionService: FusionService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        for (let p in changes) {
            if (p == 'fusionType') {
                this.load();
            }

        }
    }

    load(): void {
        this.isLoading = true;
        if (this.fusionID == null) {
            //this.formMode = FormModeConfig.Default;
            this.fusionConfigurationSchedules = null;
            this.selectedRow = null;
            this.isLoading = false;
            return;
        }
        this.fusionService.getFusionConfigurationSchedules(1, this.fusionID)
            .then(data => {
                this.fusionConfigurationSchedules = data;
                this.isLoading = false;
            });
    }
}


enum FormModeConfig {
    Default,
    Editing,
    Adding,
    Deleting,
    Filters,
    AddingFilter,
    Scheduling
}
