import {
    AfterViewChecked,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
    Output,
    ViewChild
} from '@angular/core';
import {FormGroup} from '@angular/forms';
import {Editor} from 'primeng/primeng';
import {Subject} from 'rxjs';

import {EditorDropDownItem, EditorField} from '../../../models/editor-field.model';

import {FormHelpers} from '../../../static/form-helpers';

import {CascadeService} from '../../../services/cascade.service';
import {FieldsObservableService} from '../../../services/fieldsObservable.service';

import {BaseComponent} from '../base.component';

declare var CompanySettings;

@Component({
    selector: 'd3s-dynamic-field',
    templateUrl: './dynamic-field.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService]
})

export class DynamicFieldComponent extends BaseComponent implements OnInit, OnDestroy, OnChanges, AfterViewChecked {
    @Input() field: EditorField;
    @Input() form: FormGroup;
    @Input() object: string;
    @Input() objectID: number = null;
    @Input() selectedObject: string;
    @Input() selectedObjectID: number;

    @ViewChild('ed') ed: Editor;
    private quill;

    @Output() listItemChange = new EventEmitter();

    private regexErrorMessage: string = "The field doesnt meet the required pattern.";
    private fieldTooltip: string;
    private cascadeSub: any;
    private relationSource$ = new Subject<any>();
    private relationSub: any;
    private relationItems = [];
    private relationItemsLoading = false;

    private typeAheadSource$ = new Subject<any>();
    private typeAheadSub: any;
    private typeAheadValue: EditorDropDownItem = null;
    private Increment: number = 1;
    private Min: number;
    private Max: number;
    private Precision: number;
    private colorValue: string = '#000';

    private filterException: string = '';

    private isTaxonomyType: boolean = false; // taxonomy type requires its name be mapped to whatever the setting is set to.
    private hasCascadeLoaded: boolean = false;

    constructor(
        private cascadeService: CascadeService,
        private fieldsService: FieldsObservableService,
        private ref: ChangeDetectorRef
    ) {
        super();
    }

    setEditorContent(e: any) {
        //workaround for GOV-5287, bug with primeng see JIRA for issue details

        if (this.ed == null) {
            return;
        }

        let quill = this.ed.getQuill();

        if (e == null && quill != null) {
            let contents = quill.getContents();

            if (contents != null && contents.ops != null) {
                let content = contents.ops.find(i => i.insert != null && i.insert != '\n');

                if (content != null) {
                    this.field.Value = quill.container.querySelector('.ql-editor').innerHTML;

                    return;
                }
            }
        }

        //fallback to default behavior
        this.field.Value = e;
    }

    ngOnInit() {
        this.cascadeSub = this.cascadeService.cascadeMessage$.subscribe(
            casc => {
                if (this.field.ParentFieldTypeID > 0 && casc.fieldTypeId == this.field.FieldTypeID) {
                    if (casc.parentListItemId != null && casc.parentListItemId.length > 0) {
                        //load the values for the list that is a child                    
                        this.field.Items = [];

                        return this.fieldsService.getCascadingListFieldValues(casc.fieldTypeId, casc.parentListItemId).subscribe(
                            res => {
                                this.field.Items = res;

                                if (((this.field.Items == null || this.field.Items.length == 0) && this.field.Value != null) || this.hasCascadeLoaded) {
                                    this.field.Value = null;
                                }

                                if (this.field.DelayedLoadType == 'FieldFilter') {
                                    if (this.field.Items == null || this.field.Items.length == 0) {
                                        this.form.controls[this.field.FieldName].disable();
                                    } else if (!this.field.ReadOnly) {
                                        this.form.controls[this.field.FieldName].enable();
                                    }
                                }

                                this.hasCascadeLoaded = true;

                                this.listItemChange.emit({field: this.field, value: this.field.Value});
                                this.ref.markForCheck();
                            }
                        )
                    } else {
                        this.field.Value = null;
                        this.field.Items = [];

                        if (this.field.DelayedLoadType == 'FieldFilter') {
                            this.form.controls[this.field.FieldName].disable();
                        }

                        this.listItemChange.emit({field: this.field, value: null});
                    }
                }
            });

        this.relationSub = this.fieldsService.getRelationshipFieldItems(this.relationSource$)
            .subscribe(res => {
                this.relationItemsLoading = false;
                this.field.Items = res.results["items"];
                this.selectRelationItems(this.relationItems);

                if ((res.event.globalFilter != null && res.event.globalFilter != "") || res.event.first == 0)
                    this.field.RecordCount = res.results["count"];
                this.ref.markForCheck();
            });

        this.typeAheadSub = (this.field.DelayedLoadType == 'Predicate') ?
            this.fieldsService.getTypeaheadFilteredByPredicateItems(this.typeAheadSource$, this.selectedObject, this.selectedObjectID)
                .subscribe(res => {
                    this.field.Items = <any[]>res;
                    this.ref.markForCheck();
                })
            : this.fieldsService.getTypeaheadItems(this.typeAheadSource$)
            .subscribe(res => {
                this.field.Items = <EditorDropDownItem[]>res;
                this.ref.markForCheck();
            });

        if (this.field.DelayedLoadType == 'Predicate') {
            this.fieldsService.getLookupFilteredByPredicate(this.field.FieldTypeID, this.selectedObject, this.selectedObjectID).subscribe(
                res => {
                    this.field.Items = res.items;
                    this.filterException = res.exceptionMessage;
                    this.ref.markForCheck();

                    if (res.useTypeahead && !this.field.MultiSelect) {
                        //Switch to typeahead. We do not switch back
                        this.field.UseTypeahead = true;
                    }
                }
            );
        }

        if (this.field && this.field.Validations) {
            for (let validation of this.field.Validations) {
                if (validation.regex) {
                    this.regexErrorMessage = validation.message ? String(validation.message).replace(/<[^>]+>/gm, '') : '';
                } else if (validation.rule && validation.rule.startsWith('increment')) {
                    this.Increment = +validation.rule.split("increment=")[1];
                } else if (validation.rule && validation.rule.startsWith('min')) {
                    this.Min = +validation.rule.split("minLength=")[1];
                } else if (validation.rule && validation.rule.startsWith('max')) {
                    this.Max = +validation.rule.split("maxLength=")[1];
                } else if (validation.rule && validation.rule.startsWith('length')) {
                    let vals = validation.rule.split("length=")[1];

                    this.Min = +vals.split(",")[0];
                    this.Max = +vals.split(",")[1];
                } else if (validation.rule && validation.rule.startsWith('precision')) {
                    this.Precision = +validation.rule.split("precision=")[1];
                }
            }
        }

        if (this.field && this.field.FieldDescription) {
            this.fieldTooltip = this.field.FieldDescription ? String(this.field.FieldDescription).replace(/<[^>]+>/gm, '') : '';
        }

        if (this.field && this.field.FieldName == 'TaxonomyTypeID') {
            this.isTaxonomyType = true;
        }

        if (this.field.FieldType == 'Color') {
            this.colorValue = this.field.Value;
        }

        if (this.field.FieldType == 'Relationship') {
            this.selectRelationItems(this.field.Value);
        }

        if (this.field.FieldType == 'Lookup' && this.field.ParentFieldTypeID <= 0) {
            window.setTimeout(() => {
                this.listItemChange.emit({field: this.field, value: this.field.Value});
            }, 250);
        }

        if (this.field.FieldType == 'Lookup' && this.field.UseTypeahead) {
            if (this.field.Items != null && this.field.Items.length > 0) {
                let sel: EditorDropDownItem = this.field.Items.find(i => i.Selected == true);

                this.typeAheadValue = sel;
                this.onSelect(sel);
            }
        }

    }

    ngOnChanges() {
        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        } else {
            this.quill = null;
        }
    }

    ngAfterViewChecked() {
        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        }
    }

    ngOnDestroy() {
        this.cascadeSub.unsubscribe();
        this.relationSub.unsubscribe();
        this.quill = null;
        this.ed = null;
    }

    get isValid() {
        if (this.field.FieldType == "Link") {
            if (this.form.controls[this.field.FieldName + '_Name'] == undefined
                || this.form.controls[this.field.FieldName + '_Name'].disabled
                || this.form.controls[this.field.FieldName + '_Url'] == undefined
                || this.form.controls[this.field.FieldName + '_Url'].disabled
                || this.form.controls[this.field.FieldName] == undefined
                || this.form.controls[this.field.FieldName].disabled
            ) {
                return true;
            }

            return this.form.controls[this.field.FieldName + '_Url'].valid
        }

        const numInputs = document.querySelectorAll('input[type=number]');

        for (let i = 0; i < numInputs.length; i++) {
            let elem = numInputs[i] as HTMLInputElement;

            if (elem.validity.badInput && elem.validationMessage == "Please enter a number.") {
                if (this.field.FieldType == 'Number' && this.field.FieldName == elem.name) {
                    this.form.controls[this.field.FieldName].setErrors({integer: true});
                }
                if (this.field.FieldType == 'Decimal' && this.field.FieldName == elem.name) {
                    this.form.controls[this.field.FieldName].setErrors({number: true});
                }
            }

            if (this.field.FieldType == 'Number') {
                if (elem.value.split('.').length > 1
                    || elem.value.split('+').length > 1
                    || (elem.value.indexOf('-') != 0 && elem.value.split('-').length > 1)
                    || elem.value.split('e').length > 1
                    || elem.value.split('E').length > 1
                ) {
                    if (this.field.FieldName == elem.name) {
                        this.form.controls[this.field.FieldName].setErrors({integer: true});
                    }
                }
            } else if (this.field.FieldType == 'Decimal') {
                if (elem.value.split('.').length > 2
                    || elem.value.split('+').length > 1
                    || (elem.value.indexOf('-') != 0 && elem.value.split('-').length > 1)
                    || elem.value.split('e').length > 1
                    || elem.value.split('E').length > 1
                ) {
                    if (this.field.FieldName == elem.name) {
                        this.form.controls[this.field.FieldName].setErrors({number: true});
                    }
                }
            }
        }

        return this.form.controls[this.field.FieldName].valid;
    }

    get errorMessage() {
        switch (this.field.FieldType) {
            case "Link":
                return this.fieldMessage(this.field.FieldName + '_Url');
            default:
                return this.fieldMessage(this.field.FieldName);
        }
    }

    get taxonomyName() {
        return CompanySettings.ArtifactType_TaxonomyTypeID || '';
    }

    get currentFieldName() {
        if (this.isTaxonomyType) return this.taxonomyName;
        return this.field ? this.field.Name : '';
    }

    private fieldMessage(field: string) {
        let message = "";
        let errors = this.form.controls[field].errors;

        if (this.form.controls[field] == undefined) {
            return '';
        }

        if (!errors) {
            return '';
        }

        if (errors["pattern"]) {
            message += this.regexErrorMessage;
        }

        if (errors["number"]) {
            message += "Please enter a valid number. ";
        }

        if (errors["integer"]) {
            message += "Please enter a valid integer. ";
        }

        if (errors["maxlength"]) {
            message += `${this.currentFieldName} maximum length of ${errors["maxlength"].requiredLength} characters exceeded.  Current length is [${errors["maxlength"].actualLength}] `;
        }

        if (errors["minlength"]) {
            message += `${this.currentFieldName} minimum length of ${errors["minlength"].requiredLength} characters not met.  Current length is [${errors["minlength"].actualLength}] `;
        }

        if (errors["required"]) {
            message += `${this.currentFieldName} is required. `;
        }

        if (errors["max"]) {
            message += ` Please enter a maximum value of ${errors["max"].max} `;
        }

        if (errors["min"]) {
            message += ` Please enter a minimum value of ${errors["min"].min} `;
        }

        return message;
    }

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch {
            return "Error";
        }
    }

    setColorPickerValue(e: any) {
        this.form.controls[this.field.FieldName].setValue(e);
        this.field.Value = e;
    }

    private toDecimalPlaces(e: any, precision: number) {
        let asString = '' + e.target.value;
        let val = +e.target.value;
        let newVal = +val.toFixed(precision);

        if (e == null || e.target == null || precision == null) {
            return;
        }

        if (asString.split('.').length > 1 && asString.split('.')[1].length < precision) {
            return;
        }

        if (newVal != null && (newVal != 0 || newVal != +val) && !isNaN(newVal)) {
            this.form.controls[this.field.FieldName].setValue(newVal);
            this.field.Value = newVal;
        }
    }

    private clamp(e: any, min: number, max: number, precision: number) {
        let val = e.target.value;
        let newVal = FormHelpers.clamp(val, min, max, precision);

        if (e == null || e.target == null || min == null || max == null) {
            return;
        }

        if (newVal != null && (newVal != 0 || newVal != +val) && !isNaN(newVal)) {
            this.form.controls[this.field.FieldName].setValue(newVal);
            this.field.Value = newVal;
        }
    }

    multiselectLabel(): string {
        if (this.field && this.field.ParentFieldTypeName && this.field.ParentFieldTypeName.length > 0 && (this.field.Items == null || this.field.Items.length == 0))
            return `Select a ${this.field.ParentFieldTypeName}`;
        return "Choose";
    }

    OnBlurTrim() {
        let value: string = this.form.controls[this.field.FieldName].value;

        this.form.controls[this.field.FieldName].setValue(value.trim());
    }

    private lazyLoad(e: any) {
        this.relationItemsLoading = true;
        this.relationSource$.next({
            fieldTypeID: this.field.FieldTypeID,
            object: this.object,
            objectID: this.objectID,
            event: e
        });
    }

    selectRelationItems(e: any) {
        if (e === '[]') {
            this.relationItems = [];
        } else {
            this.relationItems = e;
        }

        if (this.relationItems != null) {
            if (!Array.isArray(this.relationItems)) {
                this.relationItems = [this.relationItems];
            }

            for (let i = 0; i < this.relationItems.length; i++) { //associate the selection with the item in the table
                let x = this.field.Items.findIndex(f => f.Value == this.relationItems[i].Value);

                if (x > -1) {
                    this.relationItems[i] = this.field.Items[x];
                }
            }

            this.relationItems = this.relationItems.slice();
            this.field.Value = this.relationItems.map(i => i.Value).join(',');
        } else {
            this.field.Value = null;
        }

        this.form.controls[this.field.FieldName].setValue(this.field.Value);
        this.ref.markForCheck();
    }

    private search(e: any) {
        this.typeAheadSource$.next({fieldTypeID: this.field.FieldTypeID, value: this.field.Value, event: e});
    }

    private onSelect(e: EditorDropDownItem) {
        if (e != null) {
            this.field.Value = e.Value;
        } else {
            this.field.Value = null;
        }

        //Typeahead is a technically a list field, so we should emit an itemchange
        this.listItemChange.emit({field: this.field, value: this.field.Value});
    }

    private clearTypeahead(e: any) {
        this.typeAheadValue = null;
        this.field.Value = null;
        this.ref.markForCheck();
    }
}
