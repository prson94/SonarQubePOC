import * as _ from 'lodash';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";
import {Component, Input, OnInit} from '@angular/core';

import {FusionPromotionExecutionStats} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-promotion-history',
    templateUrl: './fusion-promotion-history.component.html',
    providers: [FusionService],
})

export class FusionPromotionHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;

    private executions: FusionPromotionExecutionStats[] = [];
    private selected: FusionPromotionExecutionStats;

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionPromotionHistory(this.maxRows)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    this.executions = res;
                    this.selected = res.length > 0 ? res[0] : null;

                    this.isLoading = false;
                }
            );
    }

    private nullDateSort(event) {
        this.executions = _.sortBy(this.executions, event.field);

        if (event.order == -1) {
            this.executions.reverse();
        }
    }
}
