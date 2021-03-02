import { Component, Input, OnChanges, OnInit, SimpleChanges } from "@angular/core";
import { match } from "core-js/fn/symbol";
import { MetricFieldTypeViewModel, ScoreType, MetricAssetDefinitionViewModel } from "../../../../models/metrics.model";
import { Operator } from "../../../../models/operator.model";
import { PointBreakdown, PointBreakDownConditionItem} from "../../../../models/score.model";
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

    private isRuleResultsModalVisible: boolean = false;

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

        this.summedMeasures = this.getSum();
    }


    private getSum(): number {
        if (this.measures && this.measures.length > 0) {
            var res: number = 0;
            this.measures.forEach((x) => {
                let match = x.Conditions?.find((c) => c.Uid === x.ConditionUid);
                let weight = 0;
                if (match) {          
                    // GOV-13832 Make sure the weight is defined on the condition, if it is not fall back to the weight on the measure.
                    weight = (isNaN(+match.Weight) ? +x.Weight : +match.Weight);                    
                } else {                    
                    weight = +x.Weight;
                }
                res += +weight;
            });            
            return res;
        }
    }

    public showRuleResults(isVisible: boolean) {
        this.isRuleResultsModalVisible = isVisible;
    }

    showPassTest(): boolean {
        let show = true;

        show = (this.scoreType !== ScoreType.DataQuality);

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

    showConditionGroups(): boolean {
        return this.selected && (this.selected.Conditions.length > 0);
    }
   
    formatWeight(num: number) {
        if (num) {
            return (num * 100).toFixed(2).replace(/[.,]00$/, "") + "%";
        } else {
            return "(default)";
        }
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
        return (val * 100).toFixed(2).replace(/(\.[0]*?)0+/g,"") + "%";
    }
}
