import { Component, Input, OnDestroy, EventEmitter, Output, OnChanges, SimpleChanges } from "@angular/core";
import * as _ from "lodash";
import { LazyLoadEvent } from "primeng/api";
import { MetricAssetDefinitionViewModel, MetricRuleResultOperation, ScoreType } from "../../../../models/metrics.model";
import { DataQualityEvidenceItemModel, DataQualityEvidenceModel, PointBreakdown } from "../../../../models/score.model";
import { ScoreService } from "../../../../services/score.service";
import { CompanySettingsService } from "../../../../services/settings.service";
import { BaseComponent } from "../../base.component";

@Component({
    selector: "measure-rule-results",
    templateUrl: `./measure-rule-results.component.html`,
    styleUrls: ["measure-rule-results.less"],
    providers: [ScoreService]
})

export class MeasureRuleResultsComponent extends BaseComponent implements OnDestroy, OnChanges {
    @Input() scoreItem: PointBreakdown;
    @Input() definition: MetricAssetDefinitionViewModel;
    @Input() assetName: string;
    @Input() assetTypeName: string;
    @Output() onClose = new EventEmitter;

    Evidence: DataQualityEvidenceModel;
    selected: DataQualityEvidenceItemModel;
    activeTab: string = "Result";
    currentSearchPhrase: string;
    previousEvent: LazyLoadEvent;
    totalRecords: number;
    rowsPerPage: number = 25;

    constructor(
        private scoreService: ScoreService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.currentSearchPhrase = null;
        if (changes["scoreItem"] && changes["scoreItem"].currentValue !== changes["scoreItem"].previousValue) {
            this.selected = null;
            this.currentSearchPhrase = null;
            if (this.scoreItem) {
                this.getResults(1, 250);
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

    performLazyLoad(event: LazyLoadEvent) {
        if (_.isEqual(event, this.previousEvent)) {
            return;
        }
        this.previousEvent = event;
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        this.rowsPerPage = event.rows;
        this.getResults((event.first / event.rows), event.rows, event.sortField, ((event.sortOrder == 1) ? "asc" : "desc"));
    }

    getResults(pageNum: number, pageSize: number, sortField: string = null, sortOrder: string = null) {
        if (this.scoreItem) {
            if (this.scoreItem.ScoreType == ScoreType.DataQuality) {
                this.isLoading = true;
                if (this.currentSearchPhrase) {
                    this.currentSearchPhrase = this.currentSearchPhrase.replace("&", "");
                    this.currentSearchPhrase = encodeURI(this.currentSearchPhrase);
                }
                this.scoreService.getDataQualityEvidenceForScoreItem(this.scoreItem.ScoreItemUid, pageNum, pageSize, this.currentSearchPhrase, sortField, sortOrder)
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

    downloadDisabled() {
        let disabled = false;
        if (this.Evidence) {
            disabled = (this.totalRecords > 500);
        }
        return disabled;
    }

    downloadTooltip() {
        let message = "";
        if (this.totalRecords) {
            message = (this.totalRecords > 500) ?
                "Download is limited to 500 rows or less. Please filter this list and try your download again." :
                "Download these rule results";
        }
        return message;
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

    getMissingRuleResultMessage() {
        let message = "";
        if (this.selected && !this.selected.EffectiveDate) {
            let operation = MetricRuleResultOperation[this.definition?.DataQuality?.ResultOperation+""];
            if (operation) {
                message = "For this scoring date, no rule results were found for this asset and rule. "
                    + "A pass fraction of 0 will therefore be used in place of this missing result. As this measure uses the ";
                if (operation === MetricRuleResultOperation.Maximum) {
                    message += "maximum pass fraction, the measure score will most likely be unaffected by this.";
                }
                else if (operation === MetricRuleResultOperation.Minimum) {
                    message += "minimum pass fraction, the measure score will be 0.";
                }
                else { // (operation === MetricRuleResultOperation.Average)
                    message += "average pass fraction, the measure score may be lower than expected.";
                }
            }
        }

        return message;
    }
}