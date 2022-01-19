import { Component, Input, OnChanges, OnInit, SimpleChanges } from "@angular/core";
import { MetricFieldTypeViewModel, ScoreType, MetricAssetDefinitionViewModel } from "../../../../models/metrics.model";
import { Operator } from "../../../../models/operator.model";
import { PointBreakdown, PointBreakDownConditionItem} from "../../../../models/score.model";
import { CompanySettingsService } from "../../../../services/settings.service";
import { BaseComponent } from "../../base.component";

@Component({
    selector: "score-calculation",
    templateUrl: `score-calculation.component.html`
})
export class ScoreCalculationComponent extends BaseComponent implements OnChanges{
    
    @Input() scoreType: ScoreType;
    @Input() definition: MetricAssetDefinitionViewModel;
    @Input() selected: PointBreakdown;
    @Input() measures: PointBreakdown[];
    @Input() formattedCheck: string = "";
    @Input() assetName: string;
    @Input() assetTypeName: string;
    @Input() fields: MetricFieldTypeViewModel[] = [];

    matchedCondition: PointBreakDownConditionItem;

    Operator = Operator;

    summedMeasures: number = 0;

    isRuleResultsModalVisible: boolean = false;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["selected"] && changes["selected"].currentValue != null) {

            let matchedCondition = null;
            if (this.selected.IsGroup && this.selected.Measures && this.selected.Measures.length > 0) {
                this.selected.Measures.forEach((m) => {
                    m.Conditions?.forEach((c) => {
                        if (c.Uid === m.ConditionUid) {
                            matchedCondition = c;
                        }
                    });
                });
            } else {
                this.selected.Conditions?.forEach((x) => {
                    if (x.Uid === this.selected.ConditionUid) {
                        matchedCondition = x;
                    }
                });
            }
           
            if (matchedCondition) {
                this.matchedCondition = matchedCondition;
            }
            else {
                this.matchedCondition = null;
            }
        }
    }

    public showRuleResults(isVisible: boolean) {
        this.isRuleResultsModalVisible = isVisible;
    }

    ruleResultsVisible(): boolean {
        return (this.scoreType === ScoreType.DataQuality && !this.selected.IsGroup);
    }

    showPassTest(): boolean {
        let show = true;

        if (this.selected.IsGroup) {
            show = false;
        }
        else {
            if (this.scoreType === ScoreType.DataQuality && !this.selected.Threshold) {
                show = false;
            }
        }
        
        return show;
    }

    isDate(item) {
        if (this.fields && this.fields.length > 0) {
            let f = this.fields.find(x => x.Name === item.FieldName);
            if (f) {
                return (f.Type === "Date")
            }
        }
    }

    formatWeight(num: number) {
        if (num) {
            return (num * 100).toFixed(2).replace(/[.,]00$/, "") + "%";
        } else {
            return "(default)";
        }
    }

    formatThreshold(num: number) {
        if (num) {
            return (num * 100).toFixed(3).replace(/[.,]00$/, "") + "%";
        } else {
            return "(default)";
        }
    }

    formulaMultiplierLabel() {
        return (!this.selected.IsGroup && this.selected._groupDisplayMaxWeight) ? "group weight" : "100%";
    }

    weightLabel() {
        return (this.matchedCondition && !this.selected.IsGroup) ? "condition group weight" : "measure weight";
    }

    getOtherMatchedGroups(): string {
        var matches = this.selected.Conditions.filter((x) => {
            return (this.selected.OtherConditions.indexOf(x.Uid) !== -1);
        });
        return matches.map((x) => x.Position).join(" and ");
    }

    public getAsPrecentageNoMax(val: number): string {

        if (val === 0) {
            return "0%";
        }
        if (!val) {
            return;
        }
        return (val * 100).toFixed(2).replace(/0+$/g, "").replace(/(\.[0]*?)0*$/g, "") + "%";
    }
}
