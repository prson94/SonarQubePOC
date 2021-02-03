import { Component, Input, OnInit, OnDestroy, EventEmitter, Output } from '@angular/core';
import { ScoreService } from '../../../../services/score.service';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'measure-rule-results',
    templateUrl: `./measure-rule-results.component.html`,
    providers: [ScoreService]
})

export class MeasureRuleResultsComponent extends BaseComponent implements OnInit, OnDestroy {

    //@Input() Measure: MetricAssetViewModel;

    @Output() onClose = new EventEmitter;

    constructor(
        private scoreService: ScoreService
    ) {
        super();
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    ngOnInit() {
        this.isLoading = true;
        //if (this.Measure.Uid) {
        //    this.metricsService.getMetricsVersionHistory(this.Measure.Uid)
        //        .subscribe(result => {
        //            this.metricHistoryRecords = result;
        //            if (this.metricHistoryRecords) {
        //                this.metricHistoryRecords.forEach(g => {
        //                    let n = {
        //                        data: g,
        //                        children: [],
        //                        expanded: true
        //                    }

        //                    this.metricTree.push(n);

        //                });
        //                if (this.metricTree !== null && this.metricTree.length > 0) {
        //                    this.selectNode(this.metricTree[0]);
        //                }
        //            }
                    this.isLoading = false;
        //        });
        //}
        //else {
        //    this.selection = null;
        //    this.metricTree = [];
        //}
    }

    cancel() {
        this.onClose.emit(null);
    }


    getAsPrecentage(val: number) {
        if (val == 0)
            return '0%';
        if (!val)
            return;
        if (val == 1)
            return '100%'
        let s = val + '0000';
        s = s.replace('0.', '');
        if (s.length > 6)
            s = (s.substr(0, 2)) + '.' + s[2] + "%";
        else
            s = (s.substr(0, 2)) + "%";
        if (s.startsWith('0'))
            s = s.substr(1, s.length);
        return s;
    }

}