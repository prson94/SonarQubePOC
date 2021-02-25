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
        if (changes['selected'] && changes['selected'].currentValue != null) {
            if (this.selected.Conditions) {
                let matchedCondition = this.selected.Conditions.find(x => x.Uid == this.selected.ConditionUid);
                if (matchedCondition)
                    this.matchedCondition = matchedCondition;
                else {
                    this.matchedCondition = null;
                }
            } else {
                this.matchedCondition = null;
            }
        }

        this.summedMeasures = this.getSum();
    }


    private getSum(): number {
        if (this.measures && this.measures.length > 0) {
            var res: number = 0;
            this.measures.forEach((x) => {
                let match = x.Conditions?.find((c) => c.Uid == x.ConditionUid);
                let weight = 0;
                if (match) {
                    weight = +match.Weight;
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
        return matches.map(x => x.Position).join(" and ");
    }

    public getAsPrecentageNoMax(val: number): string {

        if (val == undefined || val == null)
            return 'undefined';

        if (val == 0)
            return '0%';
        if (!val)
            return;

        if (val > 1) {
            return (val * 100).toFixed(2) + "%";
        }

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
