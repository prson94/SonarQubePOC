import { Component, Input, OnDestroy, EventEmitter, Output, OnChanges, SimpleChanges } from "@angular/core";
import * as _ from "lodash";
import { LazyLoadEvent } from "primeng/api";
import { ScoreType } from "../../../../models/metrics.model";
import { DataQualityEvidenceItemModel, DataQualityEvidenceModel, PointBreakdown } from "../../../../models/score.model";
import { ScoreService } from "../../../../services/score.service";
import { BaseComponent } from "../../base.component";

@Component({
    selector: "measure-rule-results",
    templateUrl: `./measure-rule-results.component.html`,
    styleUrls: ["measure-rule-results.less"],
    providers: [ScoreService]
})

export class MeasureRuleResultsComponent extends BaseComponent implements OnDestroy, OnChanges {
    @Input() scoreItem: PointBreakdown;
    @Input() assetName: string;
    @Input() assetTypeName: string;
    @Output() onClose = new EventEmitter;

    Evidence: DataQualityEvidenceModel;
    selected: DataQualityEvidenceItemModel;
    activeTab: string = "Result";
    currentSearchPhrase: string;
    previousEvent: LazyLoadEvent;
    totalRecords: number;

    constructor(
        private scoreService: ScoreService
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["scoreItem"] && changes["scoreItem"].currentValue !== changes["scoreItem"].previousValue) {
            if (this.scoreItem) {
                this.selected = null;
                this.currentSearchPhrase = null;
                this.getResults(1, 250);
            }
        }
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    cancel() {
        this.selected = null;
        this.onClose.emit(null);
    }

    getFormattedPredicate(i: number) {
        let predicate: string = "";

        if (this.selected.RollupPath[i + 1]) {
            predicate = this.selected.RollupPath[i + 1].Predicate;
            if (i > 0) {
                predicate = ", which " + predicate;
            }
            else {
                predicate = " " + predicate;
            }
        }

        return predicate;
    }

    performLazyLoad(event: LazyLoadEvent) {
        if (_.isEqual(event, this.previousEvent)) {
            return;
        }
        this.previousEvent = event;
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        this.getResults((event.first / event.rows), event.rows, event.sortField, ((event.sortOrder == 1) ? "asc" : "desc"));
    }

    getResults(pageNum: number, pageSize: number, sortField: string = null, sortOrder: string = null) {
        if (this.scoreItem) {
            if (this.scoreItem.ScoreType == ScoreType.DataQuality) {
                this.isLoading = true;
                this.scoreService.getDataQualityEvidenceForScoreItem(this.scoreItem.ScoreItemUid, pageNum, pageSize, this.currentSearchPhrase)
                    .subscribe((result) => {
                        this.Evidence = result;
                        if (this.Evidence.items.length > 0) {
                            this.selected = this.Evidence.items[0];
                        }
                        this.totalRecords = this.Evidence.items.length;
                        this.isLoading = false;
                    });
            }
        }
    }

    performDownload() {
        if (this.scoreItem) {
            this.scoreService.getDataQualityEvidenceForScoreItemExcel(this.scoreItem.ScoreItemUid, this.currentSearchPhrase);
        }
    }

    performSimpleSearch(phrase: string) {
        this.currentSearchPhrase = phrase;
        this.getResults(1, 250);
    }

    selectedItemChange(ruleResultUid: string) {

    }
}