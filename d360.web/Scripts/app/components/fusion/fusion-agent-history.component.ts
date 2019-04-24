import {Component, Input, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {BaseComponent} from '../shared/base.component';
import {FusionService} from '../../services/fusion.service';
import {FusionAgentExecutionStats, FusionConfigurationDetails} from '../../models/fusion.model';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import * as _ from 'lodash';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

@Component({
    selector: 'd3s-fusion-agent-history',
    templateUrl: './fusion-agent-history.component.html',
    providers: [FusionService],
})

export class FusionAgentHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;

    private executions: FusionAgentExecutionStats[] = [];
    private selected: FusionAgentExecutionStats;

    destroySubject$: Subject<void> = new Subject();

    @Input() fusion: FusionConfigurationDetails;

    constructor(
        private router: Router,
        private fusionService: FusionService
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionAgentHistory(
                this.maxRows,
                this.fusion ? this.fusion.ID : undefined
            )
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    this.executions = res;

                    this.selected = this.executions.length > 0 ? this.executions[0] : null;

                    this.isLoading = false;
                }
            );
    }

    private nullDateSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                
        this.executions = _.sortBy(this.executions, event.field);

        if (event.order == -1) {
            this.executions.reverse();
        }
    }

    private showFusion(fusion: FusionAgentExecutionStats) {
        if (!fusion) {
            console.log("ERROR NO SELECTED FUSION ITEM TO NAVIGATE TO.");

            return;
        }

        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('FusionType', fusion.FusionID));
    }

    private export() {
        this.fusionService.getFusionAgentHistoryExport(this.maxRows, this.fusion ? this.fusion.ID : undefined)
    }
}
