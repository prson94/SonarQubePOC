import * as _ from 'lodash';
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';

import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionPromotionExecutionStats } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-promotion-history',
    templateUrl: './fusion-promotion-history.component.html',
    providers: [FusionService],
})

export class FusionPromotionHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;

    private executions: FusionPromotionExecutionStats[] = [];
    private selected: FusionPromotionExecutionStats;

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionPromotionHistory(this.maxRows)
            .then(res => {
                this.executions = res;
                this.selected = res.length > 0 ? res[0] : null;
                this.isLoading = false;
            })
        ;
    }

    private nullDateSort(event) {
        this.executions = _.sortBy(this.executions, event.field);
        if (event.order == -1) {
            this.executions.reverse();
        }
    }
};
