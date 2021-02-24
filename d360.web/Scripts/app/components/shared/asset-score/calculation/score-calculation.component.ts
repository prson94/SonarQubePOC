import { Component, Input, OnChanges, SimpleChanges } from "@angular/core";
import { MetricFieldTypeViewModel, ScoreType, MetricAssetDefinitionViewModel } from "../../../../models/metrics.model";
import { Operator } from "../../../../models/operator.model";
import { PointBreakdown, PointBreakDownConditionItem} from "../../../../models/score.model";
import { BaseComponent } from "../../base.component";

@Component({
    selector: "score-calculation",
    templateUrl: `score-calculation.component.html`
})
export class ScoreCalculationComponent extends BaseComponent implements OnChanges {
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

    private isRuleResultsModalVisible: boolean = false;

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['selected'] && changes['selected'].currentValue != null) {
            let matchedCondition = this.selected.Conditions.find(x => x.Uid == this.selected.ConditionUid);
            if (matchedCondition)
                this.matchedCondition = matchedCondition;
            else {
                this.matchedCondition = null;
            }
        }
    }


    private getSum(): number {
        var res = 0;
        this.measures.forEach(x => res += x.Weight);
        return res;
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

}
