import {Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {Router} from '@angular/router';
import {forkJoin, Subject} from "rxjs";
import {takeUntil} from "rxjs/operators";

import {FusionType} from '../../../models/fusion.model';
import {GridColumn} from '../../../models/grid-definition.model';

import {FusionService} from '../../../services/fusion.service';
import {MessagesService} from '../../../services/messages.service';

import {SiteUrlHelpers} from '../../../static/site-url-helpers';

import {BaseComponent} from '../../shared/base.component';

@Component({
    selector: 'd3s-fusion-configuration-tile',
    templateUrl: './fusion-configuration.tile.html',
    providers: [FusionService]
})

export class FusionConfigurationTile extends BaseComponent implements OnChanges {
    @Input() fusionType: FusionType;
    @Input() title: string = 'Configurations';

    destroySubject$: Subject<void> = new Subject();

    formMode: FormModeConfig = FormModeConfig.Default;
    FormModeConfig = FormModeConfig;

    theDeleteTypeCallback: Function;

    fusionConfigurations: any[];
    selectedRow: any;

    columns: GridColumn[];

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(
        private router: Router,
        private fusionService: FusionService,
        protected messagesService: MessagesService
    ) {
        super();

        this.theDeleteTypeCallback = this.deleteFusionConfig.bind(this);
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

        forkJoin(
            this.fusionService.getFusionConfigurationGridDefinition(this.fusionType.ID),
            this.fusionService.getFusionConfigurationsByType(this.fusionType.ID)
        ).subscribe(
            (
                [
                    getFusionConfigurationGridDefinition,
                    getFusionConfigurationsByType
                ]
            ) => {
                this.columns = getFusionConfigurationGridDefinition;

                this.fusionConfigurations = getFusionConfigurationsByType;
                this.selectedRow = this.fusionConfigurations[0];

                this.isLoading = false;
            }
        );
    }

    deleteFusionConfig(id: number) {
        this.fusionService
            .deleteFusionConfiguration(id)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                result => {
                    this.formMode = FormModeConfig.Default;
                    this.showMessageForResult(this.messagesService, result);
                    this.load();
                }
            );
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
