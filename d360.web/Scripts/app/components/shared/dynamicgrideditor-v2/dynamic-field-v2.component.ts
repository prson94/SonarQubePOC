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
    ViewChild,

    HostListener
} from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Editor } from 'primeng/editor';
import { Subject, Observable } from 'rxjs';

import { EditorDropDownItem, EditorField } from '../../../models/editor-field.model';

import { FormHelpers } from '../../../static/form-helpers';

import { CascadeService } from '../../../services/cascade.service';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';

import { BaseComponent } from '../base.component';
import { TagService } from '../../../services/tag.service';
import { SelectItem } from 'primeng/api/selectitem';
import { DynEditorService } from '../../../services/dyn-editor.service';
import { AssetService } from '../../../services/asset.service';

@Component({
    selector: 'd3s-dynamic-field-v2',
    templateUrl: './dynamic-field-v2.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService, TagService, AssetService]

})

export class DynamicFieldComponentV2 extends BaseComponent implements OnInit, OnDestroy, OnChanges, AfterViewChecked {
    @Input() field: EditorField;
    @Input() form: FormGroup;
    @Input() object: string;
    @Input() objectID: number = null;
    @Input() selectedObject: string;
    @Input() selectedObjectID: number;
    @Input() editorChange: Observable<any>;
    @Input() disallowedNames: string[] = [];
    @Input() assetUid: string;
    @Input() diagramNodeKey: string;

    @Input() useNewUI: boolean = false;
    private isDirty: boolean = false;

    @ViewChild('ed', { static: false }) ed: Editor;
    private quill;

    @Output() listItemChange = new EventEmitter();
    @Output() relationItemChange = new EventEmitter();

    private regexErrorMessage: string = "The field doesnt meet the required pattern.";
    private fieldTooltip: string;
    private cascadeSub: any;
    private relationSource$ = new Subject<any>();
    private relationSub: any;
    private relationItems = [];
    private excludedRelationitems = {};
    private relationItemsLoading = false;

    private typeAheadSource$ = new Subject<any>();
    private typeAheadSub: any;
    private typeAheadValue: EditorDropDownItem = null;
    private loadTypeAheadValue: boolean = false;
    private Increment: number = 1;
    private Min: number;
    private Max: number;
    private Precision: number;
    private colorValue: string = '#000';

    private filterException: string = '';
    private fieldChangeSub;
    private editorChangeSub;

    private isMenuVisible: boolean = false;
    private hasCascadeLoaded: boolean = false;

    //For a drop down search option
    private suggestionResults: string[] = [];
    private suggestionResultsArray: any[] = [];
    @Output() autoCompleteSelected = new EventEmitter();
    private doesAssetExists: boolean = false;

    private useColorMultiSelect: boolean = false;
    defaultColorOptions: SelectItem[] = [];

    private component_uid: string = '';

    constructor(
        private cascadeService: CascadeService,
        private fieldsService: FieldsObservableService,
        private assetService: AssetService,
        private ref: ChangeDetectorRef,
        private tagService: TagService,
        public dynEditorService: DynEditorService
    ) {
        super();
        this.component_uid = Math.random().toString(36).substring(2);
        this.dynEditorService.formUpdate.subscribe(res => {
            if (res) {
                var assetUid = this.assetUid;
                if (!assetUid)
                    assetUid = this.diagramNodeKey;
                if (assetUid && assetUid == res.assetUid) {
                    if (this.field.FieldName == res.fieldName) {
                        this.form.controls[res.fieldName].patchValue(res.fieldValue);
                    }
                }
            }
        });
    }

    searchTags(q: any) {
        this.autoCompleteSelected.emit(null);
        this.tagService.searchTags(q.query, this.objectID)
            .subscribe(response => {
                this.suggestionResultsArray = response;
                this.suggestionResults = [];
                this.suggestionResultsArray.forEach(x => this.suggestionResults.push(x.name));

                this.suggestionResultsArray.forEach(s => {
                    if (s.name.toLowerCase() == this.field.Value.toLowerCase()) {
                        this.autoCompleteSelected.emit(s);
                    }
                });

                this.ref.markForCheck();
            });
    }

    checkAssetExistance() {
        this.doesAssetExists = false;

        this.tagService.searchTags(this.field.Value, this.objectID, true)
            .subscribe(response => {

                response.forEach(s => {
                    if (s.name.toLowerCase() == this.field.Value.toLowerCase()) {
                        this.doesAssetExists = true;
                    }
                });

                this.ref.markForCheck();
            });
    }

    getColorItemsAsSelectItem(items: any[]): SelectItem[] {
        if (items.length > 0) {
            return items.filter(x => x.Text != "Choose...").map((x) => {
                try {

                    let colorobj = JSON.parse(x.Text);
                    if (colorobj)
                        return { label: colorobj.name, value: x.Value, title: colorobj.color };
                } catch (ex) {
                    return { label: x.Text, value: x.Value, title: 'transparent' };
                }
            });
        }
    }

    getColorItemsAsEditorItem(items: any[]): EditorDropDownItem[] {
        if (items.length > 0) {
            let its = items.filter(x => x.Text != "Choose...").map((x) => {
                try {
                    let colorobj = JSON.parse(x.Text);
                    if (colorobj)
                        return { Text: colorobj.name, Value: x.Value, Selected: x.Selected, Disabled: x.Disabled, Group: x.Group, Color: colorobj.color };
                } catch (ex) {
                    return { Text: x.Text, Value: x.Value, Selected: x.Selected, Disabled: x.Disabled, Group: x.Group, Color: 'transparent' };
                }
            });
            return its;
        }
    }

    getLabelByID(id) {
        if (id && this.field.Items.length > 0) {
            let filterItems = this.field.Items.filter(x => x.value == id);
            if (filterItems.length > 0) {
                return filterItems[0].label;
            }
        }
        return "";
    }

    getColorByID(id) {
        if (id && this.field.Items.length > 0) {
            let filterItems = this.field.Items.filter(x => x.value == id);
            if (filterItems.length > 0) {
                return filterItems[0].title;
            }
        }
        return "";
    }
    selectTag(event) {
        var obj = this.suggestionResultsArray.filter(x => x.name == event)[0];
        this.autoCompleteSelected.emit(obj);
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

        this.ref.markForCheck();
    }

    ngOnInit() {
        if (this.field.FieldType != 'Link') {
            this.fieldChangeSub = this.form.controls[this.field.FieldName].valueChanges.subscribe(data => {
                this.onFieldChanges(data);
            });
        }

        if (this.editorChange != null) {
            this.editorChangeSub = this.editorChange.subscribe(e => this.onEditorChange(e));
        }

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
                                this.listItemChange.emit({ field: this.field, value: this.field.Value });
                                this.ref.markForCheck();
                            }
                        )
                    } else {
                        this.field.Value = null;
                        this.field.Items = [];
                        this.form.controls[this.field.FieldName].setValue(null);

                        if (this.field.DelayedLoadType == 'FieldFilter') {
                            this.form.controls[this.field.FieldName].disable();
                        }

                        this.listItemChange.emit({ field: this.field, value: null });
                    }
                }
            });

        this.relationSub = this.fieldsService.getRelationshipFieldItems(this.relationSource$)
            .subscribe(res => {
                this.relationItemsLoading = false;
                this.field.Items = res.results["items"];
                this.selectRelationItems(this.relationItems);

                //When setting count we need to take into calculation items that are disregarded in cardinality check but still presend in already selected items
                let hasCardinalityOne: boolean = res.results["hasCardinalityOne"] ? res.results["hasCardinalityOne"] : false;
                if ((res.event.globalFilter != null && res.event.globalFilter != "") || res.event.first == 0) {
                    if (hasCardinalityOne) {
                        var selectedCount = this.relationItems ? this.relationItems.length : 0;
                        this.field.RecordCount = selectedCount + res.results["count"];
                    }
                    else {
                        this.field.RecordCount = res.results["count"];
                    }
                }
                this.ref.markForCheck();
            });

        this.typeAheadSub = (this.field.DelayedLoadType == 'Predicate') ?
            this.fieldsService.getTypeaheadFilteredByPredicateItems(this.typeAheadSource$, this.selectedObject, this.selectedObjectID)
                .subscribe(res => {
                    this.field.Items = <any[]>res;
                    this.ref.markForCheck();
                })
            : this.fieldsService.getTypeaheadItems(this.typeAheadSource$, this.field.UseColorControl)
                .subscribe(res => {
                    if (this.field.UseColorControl)
                        this.field.Items = this.getColorItemsAsEditorItem(res);
                    else
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
                    this.regexErrorMessage = validation.message ? String(validation.message).replace(/<[^>]+>/gm, '') : 'Value does not match the required pattern.';
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


        if (this.field.FieldType == 'Color') {
            this.assetService.getAllColors().subscribe(x => {
                this.defaultColorOptions = x;
            });
            this.colorValue = this.field.Value;
        }

        if (this.field.FieldType == 'Relationship') {
            this.selectRelationItems(this.field.Value);
        }

        if ((this.field.FieldType == 'Date' || this.field.FieldType == 'DateTime') && isNaN(Date.parse(this.field.Value))) {
            this.field.Value = null;
            this.form.controls[this.field.FieldName].setValue(this.field.Value);
        }


        if (this.field.FieldType == 'Lookup' && this.field.ParentFieldTypeID <= 0) {
            if (this.field.Value == null && this.field.Items.some(x => x.Selected == true)) {
                this.field.Value = this.field.Items.filter(x => x.Selected == true).map(x => x.Value)
            }
            this.form.controls[this.field.FieldName].setValue(this.field.Value);
            window.setTimeout(() => {
                this.listItemChange.emit({ field: this.field, value: this.field.Value });
                this.ref.markForCheck();
            }, 250);
        }

        if (this.field.FieldType == 'Lookup' && this.field.UseTypeahead) {
            if (this.field.Items != null && this.field.Items.length > 0) {
                let sel: EditorDropDownItem = this.field.Items.find(i => i.Selected == true);

                this.loadTypeAheadValue = true;
                this.typeAheadValue = sel;
                this.onSelect(sel);
            }
        }
        if (this.field.UseColorControl) {
            this.field.Items = this.getColorItemsAsSelectItem(this.field.Items);
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

        //set input text on typeahead to current value if applicable, avoids using ngModel binding
        if (this.loadTypeAheadValue) {
            this.loadTypeAheadValue = false;
            if (this.field.UseTypeahead) {
                let el: any = document.getElementById(this.field.FieldName + '_input_' + this.component_uid);
                if (el != null && this.typeAheadValue != null)
                    el.value = this.typeAheadValue.Text;
            }
        }
    }

    ngOnDestroy() {
        if (this.cascadeSub) {
            this.cascadeSub.unsubscribe();
        }
        if (this.relationSub) {
            this.relationSub.unsubscribe();
        }
        if (this.fieldChangeSub != null) {
            this.fieldChangeSub.unsubscribe();
        }
        this.quill = null;
        this.ed = null;
    }

    onFieldChanges(data: any) {
        this.isDirty = true;
        if (this.field.FieldType == 'Lookup') {
            if (this.field.UseTypeahead) {
                if (this.typeAheadValue != null)
                    this.field.Value = this.typeAheadValue.Value;
                else
                    this.field.Value = null;
            } else {
                this.field.Value = data;
            }
            this.listItemChange.emit({ field: this.field, value: data });
        }
        else if (this.field.FieldType == 'Relationship') {
            this.listItemChange.emit({ field: this.field, value: data });

        } else if (this.field.FieldType == 'Html') {
            this.setEditorContent(data);
            this.field.Value = data;
        } else {
            this.field.Value = data;
        }

        if (this.object == 'Tag' && !this.objectID) {
            this.checkAssetExistance();
        }
    }

    get isValid() {

        if (this.doesAssetExists) {
            this.form.controls[this.field.FieldName].setErrors({ alreadyExists: true });
            return false;
        }

        if (this.object == 'Tag' && this.field.Value) {
            if (this.field.Value.includes('|')) {
                this.form.controls[this.field.FieldName].setErrors({ hasPipe: true });
                return false;
            }
        }

        if (this.selectedObject == 'TaskType' && this.field.Name == 'Name' && this.field.Value) {

            if (this.disallowedNames.filter(x => x.toLowerCase().trim() == this.field.Value.toString().toLowerCase().trim()).length > 1) {
                this.form.controls[this.field.FieldName].setErrors({ alreadyExistsProcess: true });
                return false;
            }

        }

        if (this.field.FieldType == "Link") {
            if (this.form.controls[this.field.FieldName + '_Name'] == undefined
                || this.form.controls[this.field.FieldName + '_Name'].disabled
                || this.form.controls[this.field.FieldName + '_Url'] == undefined
                || this.form.controls[this.field.FieldName + '_Url'].disabled
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
                    this.form.controls[this.field.FieldName].setErrors({ integer: true });
                }
                if (this.field.FieldType == 'Decimal' && this.field.FieldName == elem.name) {
                    this.form.controls[this.field.FieldName].setErrors({ number: true });
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
                        this.form.controls[this.field.FieldName].setErrors({ integer: true });
                    }
                }
                else if (elem.name == "ValidForDays") {
                    if (+elem.value < 1 || +elem.value > 365)
                        this.form.controls[this.field.FieldName].setErrors({ validDay: true });
                }
            } else if (this.field.FieldType == 'Decimal') {
                if (elem.value.split('.').length > 2
                    || elem.value.split('+').length > 1
                    || (elem.value.indexOf('-') != 0 && elem.value.split('-').length > 1)
                    || elem.value.split('e').length > 1
                    || elem.value.split('E').length > 1
                ) {
                    if (this.field.FieldName == elem.name) {
                        this.form.controls[this.field.FieldName].setErrors({ number: true });
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

    get currentFieldName() {
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

        if (errors["validDay"]) {
            message += "Value cannot be less than 1 or greater than 365. ";
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

        if (errors["alreadyExists"]) {
            message += `A ${this.object.toLowerCase()} with this name already exists, please enter a unique name.`;
        }

        if (errors["hasPipe"]) {
            message += `Tag name should not have pipe symbol '|' in name!`;
        }

        if (errors["alreadyExistsProcess"]) {
            message += `Please enter a unique name.`;
        }

        return message;
    }

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch (err) {
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
        var object = this.object;
        var objectId = this.objectID;

        if (this.selectedObject && this.selectedObjectID) {
            object = this.selectedObject;
            objectId = this.selectedObjectID;
        }

        this.relationSource$.next({
            fieldTypeID: this.field.FieldTypeID,
            object: object,
            objectID: objectId,
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

        this.relationItemChange.emit({ field: this.field, value: null });
        this.form.controls[this.field.FieldName].setValue(this.field.Value);
        this.ref.markForCheck();
    }

    private search(e: any) {
        this.typeAheadSource$.next({ fieldTypeID: this.field.FieldTypeID, value: this.field.Value, event: e });
    }

    private onSelect(e: EditorDropDownItem) {
        if (e != null) {
            this.field.Value = e.Value;
        } else {
            this.field.Value = null;
        }

        //Typeahead is a technically a list field, so we should emit an itemchange
        this.listItemChange.emit({ field: this.field, value: this.field.Value });
    }

    private onColorSelect(item) {
        this.form.controls[this.field.FieldName].setValue(item);
        this.field.Value = item;
    }

    private clearTypeahead(e: any) {
        this.typeAheadValue = null;
        this.field.Value = null;
        this.ref.markForCheck();
    }

    private onEditorChange(event: any) {
        if (event == null || event.field == null)
            return;

        let field = event.field;


        if (this.field.FieldType == 'Relationship') {
            this.filterSemanticRelationItems(field);
        }
    }

    private filterSemanticRelationItems(field: any) {

        if (field.FieldName == this.field.FieldName)
            return;

        if (this.field.FieldType == 'Relationship' && this.field.IsSemantic === true) {
            if (field.FieldType == 'Relationship' && field.IsSemantic === true) {
                if (field.Items == null)
                    return;

                let selectedItems = field.Value.split(',');

                field.Items.forEach(i => {
                    let selected = selectedItems.findIndex(s => s == i.Value) > -1;
                    if (selected) {
                        let ix = this.field.Items.findIndex(r => r.Value == i.Value);

                        if (ix > -1) {
                            let item = this.field.Items.slice()[ix];
                            this.field.Items.splice(ix, 1);
                            if (this.excludedRelationitems[field.FieldName] == null)
                                this.excludedRelationitems[field.FieldName] = [];
                            this.excludedRelationitems[field.FieldName].push(item);
                        }

                    } else {
                        if (this.excludedRelationitems[field.FieldName] == null)
                            return;
                        let ix = this.excludedRelationitems[field.FieldName].findIndex(r => r.Value == i.Value);
                        if (ix > -1) {
                            let item = this.excludedRelationitems[field.FieldName].slice()[ix];
                            this.excludedRelationitems[field.FieldName].splice(ix, 1)
                            this.field.Items.push(item);
                        }
                    }
                })
            }
            this.ref.markForCheck();
        }
    }

    isRequired() {
        return this.field.Validations && this.field.Validations.some(x => x.rule == 'required') == true;
    }

    getPlaceholder() {
        if (this.isRequired())
            return 'Value required';
        else return 'Optional';
    }

    getfilterplaceholder() {
        var strfiltePH = 'Search colors';
        if (this.field) {
            if (this.field.Name != null) {
                if (this.selectedObject == 'TaskType' && this.field.FieldName == 'GovernanceRole') {
                    strfiltePH = 'Search roles';
                }
            }
        }
        return strfiltePH;
    }
}
