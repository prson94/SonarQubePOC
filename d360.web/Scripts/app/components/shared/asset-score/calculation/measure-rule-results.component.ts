import { Component, Input, OnDestroy, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { DataQualityEvidenceItemModel, DataQualityEvidenceModel } from '../../../../models/score.model';
import { ScoreService } from '../../../../services/score.service';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'measure-rule-results',
    templateUrl: `./measure-rule-results.component.html`,
    styleUrls: ['measure-rule-results.less'],
    providers: [ScoreService]
})

export class MeasureRuleResultsComponent extends BaseComponent implements OnDestroy, OnChanges {
    @Input() scoreItemUid: string;
    @Output() onClose = new EventEmitter;

    Evidence: DataQualityEvidenceModel;
    selected: DataQualityEvidenceItemModel;
    activeTab: string = "Result";

    constructor(
        private scoreService: ScoreService
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges): void {
        console.log(changes);
        if (changes["scoreItemUid"] && changes["scoreItemUid"].currentValue !== changes["scoreItemUid"].previousValue) {
            console.log('here');
            if (this.scoreItemUid) {
                this.isLoading = true;
                this.scoreService.getDataQualityEvidenceForScoreItem(this.scoreItemUid)
                    .subscribe((result) => {
                        console.log(result);
                        this.Evidence = result;
                        this.isLoading = false;
                    });
            }
        }
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    cancel() {
        this.onClose.emit(null);
    }

    selectedItemChange(ruleResultUid: string) {

    }

}