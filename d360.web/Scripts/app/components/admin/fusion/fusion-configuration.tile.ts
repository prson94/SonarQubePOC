import {Input, Output, Component, OnChanges, SimpleChange} from '@angular/core';
import {Router} from '@angular/router';
import {FusionType} from '../../../models/fusion.model';
import {FusionService} from '../../../services/fusion.service';
import {GridColumn} from '../../../models/grid-definition.model';
import {BaseComponent} from '../../shared/base.component';
import {SiteUrlHelpers} from '../../../static/site-url-helpers';
import {JsonResult} from "../../../models/jsonresult.model";
import { MessagesObservableService } from '../../../services/messages-observable.service';

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

    theDeleteTypeCallback: Function;
    theMarkitLineageCallback: Function;

    fusionConfigurations: any[];
    selectedRow: any;

    columns: GridColumn[];

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(private router: Router, private fusionService: FusionService, protected messagesService: MessagesObservableService) {
        super();
        this.theDeleteTypeCallback = this.deleteFusionConfig.bind(this);
        this.theMarkitLineageCallback = this.runMarkitLineage.bind(this);
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
        this.fusionService.getFusionConfigurationGridDefinition(this.fusionType.ID).subscribe(
            data => {
                this.columns = data;

                this.fusionService.getFusionConfigurationsByType(this.fusionType.ID).subscribe(
                    data => {
                        this.fusionConfigurations = data;
                        this.selectedRow = this.fusionConfigurations[0];
                        this.isLoading = false;
                    }
                );
            }
        );
    }

    deleteFusionConfig(id: number) {
        this.fusionService.deleteFusionConfiguration(id).subscribe(
            result => {
                this.formMode = FormModeConfig.Default;
                this.showMessageForResult(this.messagesService, result);
                this.load();
            }
        );
    }

    runMarkitLineage(id: number) {
        this.fusionService.postRunMarkitLineage(id).subscribe(
            result => {
                this.formMode = FormModeConfig.Default;
                this.showMessageForResult(this.messagesService, <JsonResult>result);

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
    MarkitLineage
}
