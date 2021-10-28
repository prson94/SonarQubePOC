import { Component, Input, OnChanges, SimpleChanges, ViewEncapsulation } from "@angular/core";
import { MetricAssetVersionConditionItemFieldValueViewModel, MetricAssetVersionConditionViewModel } from "../../../../models/metrics.model";
import { CompanySettingsService } from "../../../../services/settings.service";
import { CommonScreenReferencesModel } from "../../../admin/scoring/common-screen-references-model";
import { BaseComponent } from "../../base.component";



@Component({
    selector: "measure-conditions-details",
    templateUrl: "./measure-conditions-details.component.html",
    styles: [""],
    encapsulation: ViewEncapsulation.None
})

export class MeasureConditionsDetailsComponent extends BaseComponent implements OnChanges {
    @Input() conditionGroups: MetricAssetVersionConditionViewModel[];
    @Input() matchAll: boolean;
    @Input() screenReferences: CommonScreenReferencesModel;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.formatConditions();
    }

    formatConditions() {
        if (!this.conditionGroups || (this.screenReferences.operators.length === 0 && this.screenReferences.fields.length === 0)) {
            return;
        }
        this.conditionGroups.forEach((cg) => {
            let conditions = cg.ConditionItems;
            cg.DisplayWeight = (cg.Weight * 100);
            conditions.forEach((c) => {
                const field = this.screenReferences.fields.find((f) => f.ApiName === c.ConditionFieldTypeName);
                c.OperatorText = this.screenReferences.operators.find((o) => o.ID === c.Operator).Name;

                if (field) {
                    c.FieldTypeName = field.Name;
                    c.FieldType = field;

                    switch (field.Type) {
                        case "Lookup":
                            if (field.Values && field.Values.length > 0) {
                                if (c.Values && c.Values[0]) {
                                    let valueModel: MetricAssetVersionConditionItemFieldValueViewModel = field.Values.find((o) => o.Value === c.Values[0]);
                                    valueModel = field.Values.find((o) => o.Value === c.Values[0]);
                                    if (valueModel) {
                                        c.SingleValue = c.Values[0];
                                        c.ValuesText = valueModel.Text;
                                    }
                                }
                            }
                            break;
                        case "Date":
                            if (c.Values && c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = new Date(c.Values[0]).toLocaleDateString();

                            }
                            break;
                        case "DateTime":
                            if (c.Values && c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = new Date(c.Values[0]).toLocaleString();
                            }
                            break;
                        default:
                            if (c.Values && c.Values[0]) {
                                c.SingleValue = c.Values[0];
                                c.ValuesText = c.Values[0];
                            }
                            break;
                    }
                }
            });
        });
    }   
}