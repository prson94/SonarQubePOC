import { Component, Input, EventEmitter, Output, HostListener, ChangeDetectorRef, ViewChildren, QueryList } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetViewModel, MetricAssetVersionConditionViewModel, MetricAssetVersionConditionItemViewModel, ScoreTypeAllocation } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from "../../../models/form.model";
import { FormHelpers } from '../../../static/form-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Operator } from '../../../models/operator.model';
import { FormGroup, ValidatorFn, AbstractControl, FormControl } from '@angular/forms';
import { FieldCondition, FieldTypeAPIModelFieldCondition } from '../../../models/field-condition-grid.models';
import { PropertyGroupComponent } from '../../shared/controls/property-group/property-group.component';
import * as _ from 'lodash';
import { Observable } from 'rxjs';
import { FieldTypeHelper } from '../../../models/fieldtype-api.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { CommonScreenReferencesModel } from './common-screen-references-model';
import { CompanySettingsService } from '../../../services/settings.service';
import { AppSettingsEnum } from '../../../models/settings.model';

@Component({
    template: ''
})
export class BaseMeasureEditorComponent extends BaseComponent {
    @Input() model: MetricAssetViewModel = null;
    @Input() allocation: ScoreTypeAllocation;
    @Input() uid: string;
    @Input() parentUid: string;
    @Input() screenReferences: CommonScreenReferencesModel;
    @Input() maxScoreEffectiveDate: Date;

    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();
    public conditionGroupLink: string = "";
    public conditionAndWeightLink: string = "";

    //#region Tooltip data

    measurestooltip: string = 'Asset conditions can be used to more specifically target assets of the chosen type to be scored by your measures. '
        + 'Only those assets matching the conditions will be scored using these measures. '
        + 'Where you use multiple conditions, you can specify whether an asset must match all or any of the conditions in order to be score by these measures';

    weightTootlip: string = 'Weight determines the contributions of this measure to the overall score calculated for an asset.'
        + 'For example, a measure with a weight of 50% will contribute twice as much as a measure with a weight of 25%.'
        + 'The sum of all measures used to calculate the score should sum to 100% (at the top-level and within each group of measures).'
        + 'Where they do not, they will be adjusted when used to calculate the score.';

    groupingTooltip: string = 'Grouping measures can be used to organize your measures, collecting together a group of a'
        + 'similar nature(E.g.responsibility assignments, required field checks).'
        + 'Grouping measures do not have asset conditions, as they are not applied directly to the assets.';

    assetConditionsAndWeightingTooltip: string = "";
    conditionWeightTootlip: string = "";

    //#endregion

    child = "";
    closeLabel: string = "Cancel";
    conditionFormMode = FormMode.Default;
    conditionGroups: MetricAssetVersionConditionViewModel[] = [];
    matchConditionsOnly: string = "true";
    currentEffectiveDate: Date;
    displayWeight: number;
    displayEffectiveDate: Date;
    fields: any[] = [];
    FormMode = FormMode;
    hasModelChanged: boolean = false;
    isLoadingFields: boolean = false;
    isSaving: boolean = false;
    metricForm: FormGroup = null;
    matchType: string;
    maxHeight: number = window.innerHeight - 160;
    originalConditions: MetricAssetVersionConditionViewModel[];
    originalEffectiveDate: Date;
    originalModel: MetricAssetViewModel;
    saveLabel: string = "Create";
    showMatchPicker: boolean = false;
    verb = "Add";
    canAddGroup: boolean = false;
    private baseMenuItems = [
        { "title": "Duplicate" },
        { "title": "Delete" }
    ];

    private upMenuItems: any[] = [
        { title: "Move to Top" },
        { title: "Move Up" }
    ];

    private downMenuItems: any[] = [
        { title: "Move Down" },
        { title: "Move to Bottom" }
    ];

    @ViewChildren(PropertyGroupComponent) groups: QueryList<PropertyGroupComponent>;

    constructor(
        protected fieldsService: FieldsObservableService,
        protected metricsService: MetricsService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        protected cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
        let helpBaseUri: string = this.settingsService.getAppSetting(AppSettingsEnum.HelpBaseUri);
        this.conditionGroupLink = helpBaseUri + "Default.htm#d-admin/scoring-definitions.htm#Asset_conditions";
        this.conditionAndWeightLink = helpBaseUri + "Default.htm#d-admin/scoring-definitions.htm#Asset_conditions";

        this.conditionWeightTootlip = "<div>You can override the <b>Weight</b> set in the <b>Detail</b> section here, specifically for assets which meet the conditions of this group.</div>"
            + "<div style=\"padding-top: 8px;\" ><a (click)=\"test()\" target=\"_blank\" href=\"" + this.conditionGroupLink + "\"><i class=\"fa fa-external-link\"></i> Read more about Asset Conditions and Weighting</a></div>";

        this.assetConditionsAndWeightingTooltip = "<div>Asset Conditions and Weighting allows you to target specific subsets of your scoring asset type, "
            + "either choosing to apply your measures to only those assets which match your conditions, or applying different weights to different matches.</div>"
            + "<div style=\"padding-top: 8px;\"><a (click)=\"test()\" target=\"_blank\" href=\"" + this.conditionAndWeightLink + "\"><i class=\"fa fa-external-link\"></i> Read more about Asset Conditions and Weighting</a></div>";
    }

    menuOptions(includeUp: boolean, includeDown: boolean): any[] {

        if (includeUp && includeDown) {
            return this.baseMenuItems
                .concat(this.upMenuItems)
                .concat(this.downMenuItems);
        } else if (includeUp) {
            return this.baseMenuItems.concat(this.upMenuItems);
        } else if (includeDown) {
            return this.baseMenuItems.concat(this.downMenuItems);
        } else {
            return this.baseMenuItems;
        }
    }

    menuClicked(event, displyOrder, pos) {
        switch (event.value) {
            case 'Duplicate': this.duplicate(pos);
                break;
            case 'Delete': this.delete(pos);
                break;
            case 'Move to Top': this.moveTotop(displyOrder);
                break;
            case 'Move Up': this.moveUp(displyOrder);
                break;
            case 'Move Down': this.moveDown(displyOrder);
                break;
            case 'Move to Bottom': this.moveToBottom(displyOrder);
                break;
            default: console.log("unknown action");
                break;
        }
    }

    delete(pos) {
        this.conditionGroups = [...this.conditionGroups.filter((x) => x.Position != pos)];
        this.removeConditionGroupFormControls(pos);

        if (this.conditionGroups.length == 0)
            this.addNewGroup();
        this.orderConditionGroups();
    }
    moveTotop(pos) {
        this.moveGroupItems(pos, 1);
    }
    moveUp(pos) {
        this.moveGroupItems(pos, pos - 1);
    }
    moveDown(pos) {
        this.moveGroupItems(pos, pos + 1);
    }
    moveToBottom(pos) {
        this.moveGroupItems(pos, this.conditionGroups.length);
    }

    duplicate(pos) {
        let itemToDupe = this.conditionGroups.find(x => x.Position == pos);
        let newGroup = _.cloneDeep(itemToDupe);
        newGroup.Position = this.getMaxPositionForGroups();
        newGroup.DisplayOrder = this.getMaxDisplayOrderForGroups();
        this.addConditionGroupFormControls(newGroup.Position);
        this.conditionGroups.push(newGroup);
    }

    moveGroupItems(from, to) {
        let temp = from;
        let fromitem = this.conditionGroups.find(x => x.DisplayOrder == from);
        let toitem = this.conditionGroups.find(x => x.DisplayOrder == to);

        fromitem.DisplayOrder = to
        toitem.DisplayOrder = temp;
        this.orderConditionGroups();
    }

    addNewGroup() {
        let newGroup = new MetricAssetVersionConditionViewModel();
        newGroup.Position = this.getMaxPositionForGroups();
        newGroup.DisplayOrder = this.getMaxDisplayOrderForGroups();
        newGroup.MatchType = "All";
        newGroup.DisplayWeight = null;
        newGroup.conditionItemFields = [];
        this.addConditionGroupFormControls(newGroup.Position);

        this.conditionGroups.push(newGroup);
    }

    getMaxPositionForGroups(): number {
        if (this.conditionGroups.length > 0) {
            return (this.conditionGroups.map(x => x.Position).sort((a, b) => b - a)[0] + 1);
        } else {
            return 0;
        }
    }

    getMaxDisplayOrderForGroups(): number {
        if (this.conditionGroups.length > 0) {
            return (this.conditionGroups.map(x => x.DisplayOrder).sort((a, b) => b - a)[0] + 1);
        } else {
            return 0;
        }
    }
   
    canAddNewGroup(event: boolean) {
        if (this.conditionGroups.length > 0 && this.conditionGroups.every(x => x.conditionItemFields.filter(x => x.field).length > 0)) {
            this.canAddGroup = event;
        } else {
            this.canAddGroup = false;
        }
    } 

    orderConditionGroups() {
        this.conditionGroups.sort((a, b) => a.DisplayOrder - b.DisplayOrder);
        this.conditionGroups.forEach((x, i) => {
            let pos = i + 1;
            this.removeConditionGroupFormControls(pos);
            this.addConditionGroupFormControls(pos);
            x.DisplayOrder = pos;
        });
    }

    sortByDisplayOrder() {
        return this.conditionGroups.sort((a, b) => a.DisplayOrder - b.DisplayOrder);
    }

    addConditionGroupFormControls(index: number) {
        const prefix = `cg_${index}_`;
        this.metricForm.addControl(prefix + 'matchType', new FormControl());
        this.metricForm.addControl(prefix + 'weight', new FormControl('', [this.isValidWeightOptional()]));
    }

    removeConditionGroupFormControls(index: number) {
        const prefix = `cg_${index}_`;
        this.metricForm.removeControl(prefix + 'matchType');
        this.metricForm.removeControl(prefix + 'weight');
    }

    loadConditions() {
        if (this.model.ConditionGroups && this.model.ConditionGroups.length > 0) {
            this.matchType = (this.model.ConditionGroups[0].MatchType.toString() === 'All') ? 'All' : 'Any';
        }

        if (!this.model.ConditionGroups || this.model.ConditionGroups.length === 0) {
            this.conditionGroups = [];
            const dummyConditionGroup = new MetricAssetVersionConditionViewModel();
            dummyConditionGroup.Position = 1;
            dummyConditionGroup.DisplayOrder = 1;
            dummyConditionGroup.MatchType = "All";
            dummyConditionGroup.conditionItemFields = [];
            this.conditionGroups.push(dummyConditionGroup);

            this.addConditionGroupFormControls(1);

        } else if (this.model.ConditionGroups && this.model.ConditionGroups.length > 0) {
            this.conditionGroups = [];
            this.model.ConditionGroups.forEach((x) => {
                let newGroup: MetricAssetVersionConditionViewModel = new MetricAssetVersionConditionViewModel();
                newGroup.Uid = x.Uid;
                newGroup.MatchType = x.MatchType;
                newGroup.DisplayOrder = x.Position ?? this.getMaxPositionForGroups();
                newGroup.Position = newGroup.DisplayOrder; 
                newGroup.Threshold = x.Threshold;
                newGroup.Weight = x.Weight;
                if (newGroup.Weight) {
                    newGroup.DisplayWeight = +((x.Weight * 100).toFixed(2)) ?? this.model.Weight;
                }
                //get all condition items and convert them into FieldCoditions for the conditiongroup
                const conditions = x.ConditionItems;
                newGroup.conditionItemFields = [];
                if (conditions.length > 0) {
                    conditions.forEach((c) => {
                        const cond = new FieldCondition();
                        c.FieldType = this.screenReferences.fields.find(x => x.ApiName == c.ConditionFieldTypeName);
                        cond['uid'] = c.Uid;
                        cond.field = `${this.allocation.assetTypeUid}.${c.FieldType.ApiName}`;
                        cond.isValid = true;
                        cond.operator = c.Operator;
                        cond.value = c.Values[0];

                        if (c.FieldType.Type == 'DateTime' || c.FieldType.Type == 'Date') {
                            cond.value = new Date(cond.value);
                        }
                        if (c.FieldType.Type == 'Lookup') {
                            cond.value = cond.value.toString();
                        }

                        newGroup.conditionItemFields.push(cond);
                    })
                }

                this.addConditionGroupFormControls(newGroup.Position);
                this.conditionGroups.push(newGroup);
            });
            this.orderConditionGroups();
        }
    }

    showConditionMatch(cg): boolean{
        return cg.conditionItemFields.filter(x => x.field).length > 1;
    }

    loadConditionFieldOptions(): Observable<boolean> {
        this.isLoadingFields = true;

        const observer: Observable<boolean> = new Observable((obs) => {

            this.fieldsService.getFieldsV2(this.allocation.assetTypeUid, null, null).subscribe((res) => {
                const tempFields: FieldTypeAPIModelFieldCondition[] = [];
                res.forEach((f) => {
                    if (FieldTypeHelper.isFieldForOperator(f.Type)) {
                        let allowAdd: boolean = true;
                        if (f.Type.Lookup !== null && f.Type.Lookup !== undefined) {
                            allowAdd = f.Type.Lookup.List.Class !== "Model";
                        }
                        if (allowAdd) {
                            tempFields.push(f as FieldTypeAPIModelFieldCondition);
                        }
                    }
                });
                tempFields.forEach(f => {
                    f.Operators = [];
                    this.screenReferences.operators.forEach(op => {
                        if (op.AllowedDataTypes.some(x => x.Name.toLowerCase() === FieldTypeHelper.getFieldType(f.Type).toLowerCase())) {
                            f.Operators.push({ label: op.Name, value: op.ID });
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Lookup') {

                            const options = this.screenReferences.fields.find(x => x.ApiName === f.Name);
                            f.Values = [];
                            if (options && options.Values) {
                                options.Values.forEach(val => {
                                    f.Values.push({ value: val.Value.toString(), label: val.Text });
                                })
                            }
                        }

                        if (FieldTypeHelper.getFieldType(f.Type) === 'Boolean') {
                            f.Values = [];
                            f.Values.push({ value: 'true', label: 'True' });
                            f.Values.push({ value: 'false', label: 'False' });
                        }
                    });

                });
                this.fields = tempFields.filter(x => x.Operators.length > 0);
                this.isLoadingFields = false;

                obs.next(true);
                obs.complete();
            });
        });
        return observer;
    }

    getUtcDate(date: Date) {
        var utc = new Date(date.getTime() + date.getTimezoneOffset() * 60000);
        return new Date(utc);
    }

    setFormPropertiesBasedOnMode() {
        if (!this.model)
            this.model = new MetricAssetViewModel();
        this.child = "";
        this.model.ParentUid = null;
        this.currentEffectiveDate = null;

        if (this.isEditBasedOnUid()) {
            this.verb = "Edit"
            this.saveLabel = "Save Changes";
            this.closeLabel = "Close";
            if (this.model.EffectiveDate !== null) {
                var date = this.utcToLocal(new Date(this.model.EffectiveDate));
                this.currentEffectiveDate = new Date(this.model.EffectiveDate);
                this.displayEffectiveDate = date;
            }
        }
        else {
            this.model = new MetricAssetViewModel();
            this.model.Weight = null;
            this.model.IsGroup = false;
            this.verb = "Add";
            if (this.parentUid) {
                this.child = "Child";
                this.model.ParentUid = this.parentUid;
            }
            this.model.EffectiveDate = new Date();
            this.model.AllocationUid = this.allocation.uid;
        }
    }

    isEditBasedOnUid(): boolean {
        let mode: boolean = false; //false means this is an ADD.
        mode = (this.uid) ? true : false;
        return mode;
    }

    getCorrectedValueForRawByDataType(dataType: string, rawValue: string) {
        let correctedValue: string = rawValue;

        switch (dataType) {
            case 'Date':
            case 'DateTime':
                let d = new Date(rawValue);
                let condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
                condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
                correctedValue = condate.toUTCString();
                break;
        }

        return correctedValue;
    }

    doesSelectedOperatorAllowValues(operator: any): boolean {
        let allowed: boolean = true;
        switch (operator) {
            case Operator.Populated:
            case Operator.NotPopulated:
            case Operator.IsTrue:
            case Operator.IsFalse:
            case "Populated":
            case "NotPopulated":
            case "IsTrue":
            case "IsFalse":
                allowed = false;
                break;
        }
        return allowed;
    }

    saveMeasure() {
        
        this.model.ConditionGroups = this.conditionGroups;
        this.isSaving = true;
        let prevDate: string | Date = null;
        let previousConditions = [...this.model.ConditionGroups];

        if (this.allocation.isExternallyCalculated) {
            this.model.MatchConditionsOnly = false;
        }

        if (this.displayEffectiveDate !== null) {
            prevDate = this.displayEffectiveDate;
            let d = new Date(this.displayEffectiveDate);
            let condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
            condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
            this.model.EffectiveDate = condate;
        }
        
        this.matchType = (this.matchType == 'Any') ? 'Any' : 'All';
        
        if (this.allocation.isExternallyCalculated) {
            this.model.ConditionGroups = [];
        }
        else {
            this.conditionGroups.forEach(x => {
                const conditions = x.conditionItemFields.filter(x => x.field);
                x.Position = x.DisplayOrder;
                if (x.DisplayWeight) {
                    x.Weight = +(x.DisplayWeight / 100).toFixed(2);
                } else {
                    x.Weight = null;
                }
                conditions.forEach(c => {
                    let fieldCondition = new MetricAssetVersionConditionItemViewModel();
                    fieldCondition.ConditionFieldTypeName = c.field.split('.')[1]; // {assetTypeUid}.{FieldTypeName}
                    fieldCondition.Operator = c.operator;
                    fieldCondition.FieldType = this.screenReferences.fields.filter(x => x.ApiName == fieldCondition.ConditionFieldTypeName)[0];

                    if (!fieldCondition.Values) {
                        fieldCondition.Values = [];
                    }
                    if (fieldCondition.Values.length === 0) {
                        fieldCondition.Values.push('');
                    }
                    fieldCondition.Values[0] = this.getCorrectedValueForRawByDataType(fieldCondition.FieldType.Type, c.value);

                    if (!this.doesSelectedOperatorAllowValues(<any>c.operator)) {
                        fieldCondition.Values = [];
                    }

                    if (c['uid']) {
                        fieldCondition.Uid = c['uid'];
                    }

                    x.ConditionItems.push(fieldCondition);
                });
            });

            let weight = +this.displayWeight;
            this.model.Weight = +(weight / 100).toFixed(2);
        }

        this.metricsService.saveMetric(this.model)
            .subscribe(r => {
                if (r && r.type != 'error') {
                    this.isSaving = false;
                    this.showMessageForResult(this.messagesService, r);
                    this.onSave.emit(this.model.Name);
                    this.cdRef.markForCheck();
                }
                else {
                    this.displayEffectiveDate = prevDate as Date;
                    this.model.ConditionGroups = [...previousConditions];
                    this.isSaving = false;
                    this.cdRef.markForCheck();
                }
            });
    }

    getUTCDate(date: Date): Date {
        date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
        return date;
    }

    utcToLocal(date: Date): Date {
        return new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(), date.getUTCHours(), date.getUTCMinutes(), date.getUTCSeconds());
    }

    getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    @HostListener('window:resize', ['$event'])
    onResize(event) {
        this.maxHeight = window.innerHeight - 240;
        this.cdRef.markForCheck();
    }

    isEmptyString(): ValidatorFn {
        type NewType = AbstractControl;

        return (control: NewType): { [key: string]: any } | null => {
            if (control.value == null)
                return {};
            if ((control.value as string).trim() == '' && (control.value as string) != '')
                return {
                    empty: { value: control.value }
                };
            return null;
        };
    }

    isValidWeight(): ValidatorFn {
        type NewType = AbstractControl;
        return (control: NewType): { [key: string]: any } | null => {
            if (control.value == null || control.value == undefined)
                return {};
            if ((control.value as number) < 1 || (control.value as number) > 100)
                return {
                    outOfRange: { value: control.value }
                };
            return null;
        };
    }

    isValidWeightOptional(): ValidatorFn {
        type NewType = AbstractControl;
        return (control: NewType): { [key: string]: any } | null => {
            if (control.value == null || control.value == undefined || control.value == "")
                return {};
            if ((control.value as number) < 1 || (control.value as number) > 100)
                return {
                    outOfRange: { value: control.value }
                };
            return null;
        };
    }

    getFormattedEffectiveDate(effectiveDate: Date): Date {
        let d = new Date(effectiveDate);
        let condate = new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0, 0);
        condate.setMinutes(condate.getMinutes() - condate.getTimezoneOffset());
        return condate
    }

    haveConditionsChanged(updated: MetricAssetVersionConditionViewModel[], original: MetricAssetVersionConditionViewModel[]) {

        if (updated && !original) {
            return true;
        }

        if (updated && original) {
            let changeFound = updated.length != original.length;
            updated.forEach(x => {
                let originalMatch = original.find(y => y.Uid == x.Uid);
                if (originalMatch) {
                    if (x.MatchType !== originalMatch.MatchType
                        || x.Position !== originalMatch.Position
                        || x.Threshold !== originalMatch.Threshold
                        || +(x.Weight ?? 0) !== +(originalMatch.Weight ?? 0) 
                        || +(x.DisplayWeight ?? 0) !== +(originalMatch.DisplayWeight ?? 0)
                        || x.DisplayOrder !== originalMatch.DisplayOrder
                        || x.Position !== originalMatch.Position
                        || x.conditionItemFields.filter(x => x.field).length !== originalMatch.conditionItemFields.filter(x => x.field).length) {
                        changeFound = true;
                    } else if (!originalMatch.conditionItemFields.every((item) => {
                        return x.conditionItemFields.findIndex(x => x.field == item.field
                            && (x.operator == item.operator || Operator[x.operator] == <any>item.operator)
                            && (x.value ? x.value.toString() : "") == (item.value ? item.value.toString() : "")) > -1
                    })) {
                        changeFound = true;
                    }
                } else {
                    changeFound = true;
                }
            });
            return changeFound;
        }
    }

    haveRuleConditionsChanged(updated: FieldCondition[], original: FieldCondition[]) {
        if (updated && !original) {
            return true;
        }

        if (updated && original) {
            if (updated.length != original.length || !original.every((item) => {
                return updated.findIndex(x => x.field == item.field
                    && (x.operator == item.operator || Operator[x.operator] == <any>item.operator)
                    && (x.value ? x.value.toString() : "") == (item.value ? item.value.toString() : "")) > -1
            })) {
                return true;
            }
        }

        if (this.originalModel.ConditionGroups && this.originalModel.ConditionGroups.length > 0) {
            if (this.matchType && this.originalModel.ConditionGroups[0].MatchType.toString() !== this.matchType) {
                return true;
            }
        }

        return false;
    }
};