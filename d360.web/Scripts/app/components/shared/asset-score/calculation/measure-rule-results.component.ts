import { Component, Input, OnDestroy, EventEmitter, Output, OnChanges, SimpleChanges } from "@angular/core";
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

    constructor(
        private scoreService: ScoreService
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["scoreItem"] && changes["scoreItem"].currentValue !== changes["scoreItem"].previousValue) {
            if (this.scoreItem) {
                this.getResults(null);
            }
        }
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    cancel() {
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

    getResults(searchPhrase: string) {
        if (this.scoreItem) {
            this.isLoading = true;
            this.scoreService.getDataQualityEvidenceForScoreItem(this.scoreItem.ScoreItemUid, searchPhrase)
                .subscribe((result) => {
                    this.Evidence = result;
                    if (this.Evidence.items.length > 0) {
                        this.selected = this.Evidence.items[0];
                    }
                    this.isLoading = false;
                });
        }
    }

    performDownload() {
        if (this.scoreItem) {
            this.scoreService.getDataQualityEvidenceForScoreItemExcel(this.scoreItem.ScoreItemUid, this.currentSearchPhrase);
        }
    }

    performSimpleSearch(phrase: string) {
        this.currentSearchPhrase = phrase;
        this.getResults(phrase);
    }

    selectedItemChange(ruleResultUid: string) {

    }
}