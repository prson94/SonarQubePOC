import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { FusionConfiguration, FusionType, FusionFilter } from '../../../models/fusion.model';
import { FusionService } from '../../../services/fusion.service';
import { GridColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-fusion-configuration-tile',
    templateUrl: './fusion-configuration.tile.html',
    providers: [FusionService]
})

export class FusionConfigurationTile extends BaseComponent implements OnChanges {
    @Input() fusionType: FusionType;
    @Input() title: string = 'Configurations';
    
    formMode: FormModeConfig = FormModeConfig.Default;
    FormModeConfig = FormModeConfig;

    fusionConfigurations: any[];
    selectedRow: any;

    fusionFilters: FusionFilter[];
    selectedFusionFilter: FusionFilter;


    columns: GridColumn[];

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
        if (this.fusionType == null) {
            this.formMode = FormModeConfig.Default;
            this.fusionConfigurations = null;
            this.selectedRow = null;
            this.isLoading = false;
            return;
        }
        this.fusionService.getFusionConfigurationGridDefinition(this.fusionType.ID)
            .then(data => { this.columns = data; })
            .then(() => this.fusionService.getFusionConfigurationsByType(this.fusionType.ID))
            .then(data => {
                this.fusionConfigurations = data;
                this.selectedRow = this.fusionConfigurations[0];
                this.isLoading = false;
            });
    }

    private openFusion(fusion) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${fusion.ID}`);
    }
}


enum FormModeConfig {
    Default,
    Editing,
    Adding,
    Deleting,
    Filters,
    AddingFilter,
    Scheduling,
    QueryOverrides
}
