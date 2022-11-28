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
	ElementRef
} from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Editor } from 'primeng/editor';
import { Observable, Subscription } from 'rxjs';

import { EditorDropDownItem, EditorField } from '../../../models/editor-field.model';

import { FormHelpers } from '../../../static/form-helpers';

import { FieldsObservableService } from '../../../services/fieldsObservable.service';

import { BaseComponent } from '../base.component';
import { TagService } from '../../../services/tag.service';
import { SelectItem } from 'primeng/api/selectitem';
import { DynEditorService } from '../../../services/dyn-editor.service';
import { AssetService } from '../../../services/asset.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { Dropdown } from 'primeng/dropdown';
import { OverlayPanel } from 'primeng/overlaypanel';
import { Table } from 'primeng/table';
import { MultiSelect } from "primeng/multiselect";

@Component({
    selector: 'asset-editor-field',
    templateUrl: './asset-editor-field.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService, TagService, AssetService]

})
export class AssetEditorFieldComponent extends BaseComponent implements OnInit, OnDestroy, OnChanges, AfterViewChecked {
    @Input() field: EditorField;
    @Input() form: FormGroup;
    @Input() object: string;
    @Input() objectID: number = null;
    @Input() selectedObject: string;
    @Input() selectedObjectID: number;
    @Input() editorChange: Observable<any>;
    @Input() disallowedNames: string[] = [];
    @Input() assetUid: string;
    @Input() assetTypeUid: string;
    @Input() diagramNodeKey: string;
    selectionScrollHeight: string = "320px";

    @Input() useNewUI: boolean = false;
	@Input() useSidePanel: boolean = false;
    private isDirty: boolean = false;
	
	@Input() sidePanelSelection: { objectID: string, fieldName: string };
	@Output() sidePanelSelectionChange: EventEmitter<{ objectID: string, fieldName: string }> = new EventEmitter<any>();

    @ViewChild('ed', { static: false }) ed: Editor;
    private quill;

    @Output() listItemChange = new EventEmitter();
    @Output() relationItemChange = new EventEmitter();

    private regexErrorMessage: string = $localize`The field doesnt meet the required pattern.`;

    private excludedRelationitems = {};
    private relationItemsLoading = false;

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

    componentUid: string = '';

    isLookupValuesLoading: boolean = false;

    linkFieldOptionalPlaceholder: string = $localize`Optional: you should start the URL with a protocol prefix eg. http:// or https://`;
    linkFieldRequiredPlaceholder: string = $localize`Value required: you should start the URL with a protocol prefix eg. http:// or https://`;

    showLookupSearchField: boolean = false;
    hadInitialLazyLoad: boolean = false;

    @ViewChild('dropdown', { static: false }) dropdown: Dropdown & MultiSelect;
    @ViewChild('overlayPanel', { static: false }) overlayPanel: OverlayPanel;
    @ViewChild("dataTable", { static: false }) dataTable: Table;

    constructor(
        private fieldsService: FieldsObservableService,
        private assetService: AssetService,
        private ref: ChangeDetectorRef,
        public dynEditorService: DynEditorService,
        protected settingsService: CompanySettingsService,
        private elRef: ElementRef
    ) {
        super(settingsService);
        this.componentUid = Math.random().toString(36).substring(2);
        this.dynEditorService.formUpdate.subscribe((res) => {
            if (res) {
                var assetUid = this.assetUid;
                if (!assetUid) {
                    assetUid = this.diagramNodeKey;
                }
                if (assetUid && assetUid === res.assetUid) {
                    if (this.field.FieldName === res.fieldName) {
                        this.form.controls[res.fieldName].patchValue(res.fieldValue);
                    }
                }
            }
        });

        this.dynEditorService.lookupFieldUpdated.subscribe((res) => {
            if (this.field && this.field.ParentFieldTypeName && res.fieldName === this.field.ParentFieldTypeName) {
                this.form.controls[this.field.FieldName].setValue(null);
                this.field.Items = [];
                this.lookupSelectedValue = [];
                this.lookupValues = [];
            }
        });
        setInterval(() => {
            this.setSelectionVirtualScrollHeight();
        }, 25);
    }

    get hasKeyFieldError () {
        const formControl = this.form.get(this.field.FieldName);
        if (formControl?.errors == null) {
            return false;
        }
        
        return 'nonUniqueField' in formControl.errors || 'nonUniqueFieldCombination' in formControl.errors;
    }

    //we do not want key fields error to disable submit button so we are handing this error differently
    public setKeyFieldsErrorMessage(isSingle: boolean) {
        const formControl = this.form.get(this.field.FieldName);
        if (this.field.IsPartOfKey) {
            if (isSingle) {
                formControl.setErrors({ nonUniqueField: true });
            }
            else {
                formControl.setErrors({ nonUniqueFieldCombination: true });
            }
            this.ref.markForCheck();
        }
    }

    setEditorContent(e: any) {
        //workaround for GOV-5287, bug with primeng see JIRA for issue details

        if (this.ed == null) {
            return;
        }

        const quill = this.ed.getQuill();

        if (e == null && quill != null) {
            const contents = quill.getContents();

            if (contents != null && contents.ops != null) {
                const content = contents.ops.find((i) => i.insert !== null && i.insert !== '\n');

                if (content != null) {
                    this.field.Value = quill.container.querySelector('.ql-editor').innerHTML;

                    return;
                }
            }
        }

        this.ref.markForCheck();
    }

    ngOnInit() {
        if (this.field.FieldType !== 'Link') {
            this.fieldChangeSub = this.form.controls[this.field.FieldName].valueChanges.subscribe((data) => {
                this.onFieldChanges(data);
            });
        }
        else {
            this.fieldChangeSub = this.form.controls[this.field.FieldName + "_Url"].valueChanges.subscribe((data) => {
                this.onFieldChanges(data);
            });
            this.fieldChangeSub = this.form.controls[this.field.FieldName + "_Name"].valueChanges.subscribe((data) => {
                this.onFieldChanges(data);
            });
        }

        if (this.editorChange != null) {
            this.editorChangeSub = this.editorChange.subscribe((e) => this.onEditorChange(e));
        }

        if (this.field.DelayedLoadType === 'Predicate') {
            this.fieldsService.getLookupFilteredByPredicate(this.field.FieldTypeID, this.selectedObject, this.selectedObjectID).subscribe(
                (res) => {
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
            for (const validation of this.field.Validations) {
                if (validation.regex) {
                    this.regexErrorMessage = validation.message ? String(validation.message).replace(/<[^>]+>/gm, '') : $localize`Value does not match the required pattern.`;
                } else if (validation.rule && validation.rule.startsWith('increment')) {
                    this.Increment = +validation.rule.split("increment=")[1];
                } else if (validation.rule && validation.rule.startsWith('min')) {
                    this.Min = +validation.rule.split("minLength=")[1];
                } else if (validation.rule && validation.rule.startsWith('max')) {
                    this.Max = +validation.rule.split("maxLength=")[1];
                } else if (validation.rule && validation.rule.startsWith('length')) {
                    const vals = validation.rule.split("length=")[1];

                    this.Min = +vals.split(",")[0];
                    this.Max = +vals.split(",")[1];
                } else if (validation.rule && validation.rule.startsWith('precision')) {
                    this.Precision = +validation.rule.split("precision=")[1];
                }
            }
        }

        if (this.field.FieldType === 'Color') {
            this.assetService.getAllColors().subscribe((x) => {
                this.defaultColorOptions = x;
            });
            this.colorValue = this.field.Value;
        }

        if ((this.field.FieldType === 'Date' || this.field.FieldType === 'DateTime') && isNaN(Date.parse(this.field.Value))) {
            this.field.Value = null;
            this.form.controls[this.field.FieldName].setValue(this.field.Value);
        }

        if (this.field.FieldType === 'Relationship' && this.field.Value) {
            this.field.Items = this.field.Value;
            let value: any;
			if (this.field.MultiSelect) {
				value = this.field.Value.filter((x) => x.Selected === true).map((x) => x.Value);
			} else {
				value = this.field.Value.filter((x) => x.Selected === true).map((x) => x.Value)[0];
			}
			
			this.lookupSelectedValue = [];
			this.field.Items.forEach((item) => {
				item['label'] = item['Text'];
				item['value'] = item['Value'];
				this.lookupSelectedValue.push({ label: item.Text, value: item.Value });
			});
			this.selectSingleItem(null, { value: null });
			
			this.form.controls[this.field.FieldName].setValue(value);
        }

        if (this.field.FieldType === 'Relationship') {
            //dropdown with showClear attribute shows clear button even if value is empty string
            var hasNoValue = false;
            if (!this.field.Value) {
                hasNoValue = true;
            }
            else if (Array.isArray(this.field.Value) && (this.field.Value as []).length === 0) {
                hasNoValue = true;
            }

            if (hasNoValue) {
                this.form.controls[this.field.FieldName].setValue(null);
            }
        }

        if (this.field.FieldType === 'Lookup') {
            this.field.Items.forEach((item) => {
                if (this.isJson(item.Text)) {
                    var obj = JSON.parse(item.Text);
                    item.Text = obj.name;
                    item.color = obj.color;
                    item.label = obj.name;
                }
				item.value = item.Value;
            });

			let value: any = this.field.Value;

            if (!value && this.field.Items.some((x) => x.Selected === true)) {
				if (this.field.MultiSelect) {
					value = this.field.Items.filter((x) => x.Selected === true).map((x) => x.Value);
				} else {
					value = this.field.Items.filter((x) => x.Selected === true).map((x) => x.Value)[0];
				}
            }

            if (value) {
                this.lookupSelectedValue = [];
                this.field.Items.forEach((item) => {
                    this.lookupSelectedValue.push({ label: item.Text, value: item.Value });
                });
                this.selectSingleItem(null, { value: null });
                if (this.field.MultiSelect) {
                    this.field.Items = this.field.Items.filter((item) => value.includes(item.value));
                } else {
                    this.field.Items = this.field.Items.filter((item) => item.value === value);
                }
            } else {
				this.field.Items = [];
            }
			this.form.controls[this.field.FieldName].setValue(value);

            window.setTimeout(() => {
                this.listItemChange.emit({ field: this.field, value });
                this.ref.markForCheck();
            }, 250);
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

        if (this.dropdown && this.overlayPanel) {
            const width = this.dropdown.el.nativeElement.offsetWidth;
            if (this.overlayPanel.overlayVisible && this.overlayPanel.container) {
                this.overlayPanel.container.style.width = width + "px";
				this.overlayPanel.align();
            }
        }

    }

    ngOnDestroy() {
        if (this.fieldChangeSub != null) {
            this.fieldChangeSub.unsubscribe();
        }
        this.quill = null;
        this.ed = null;
    }

    onFieldChanges(data: any) {
        this.isDirty = true;
        if (this.field.FieldType === 'Lookup') {
            this.field.Value = data;
            this.listItemChange.emit({ field: this.field, value: data });
        }
        else if (this.field.FieldType === 'Relationship') {
            this.listItemChange.emit({ field: this.field, value: data });

        } else if (this.field.FieldType === 'Html') {
            this.setEditorContent(data);
            this.field.Value = data;
        } else {
            this.field.Value = data;
        }
    }

    get isValid() {

        if (this.doesAssetExists) {
            this.form.controls[this.field.FieldName].setErrors({ alreadyExists: true });
            return false;
        }

        if (this.object === 'Tag' && this.field.Value) {
            if (this.field.Value.includes('|')) {
                this.form.controls[this.field.FieldName].setErrors({ hasPipe: true });
                return false;
            }
        }

        if (this.selectedObject === 'TaskType' && this.field.Name === 'Name' && this.field.Value) {

            if (this.disallowedNames.filter((x) => x.toLowerCase().trim() === this.field.Value.toString().toLowerCase().trim()).length > 1) {
                this.form.controls[this.field.FieldName].setErrors({ alreadyExistsProcess: true });
                return false;
            }

        }

        if (this.field.FieldType === 'Link') {
            var control = this.form.controls[this.field.FieldName + '_Url'];
            if (control.value) {
                var value = control.value as string;
                if (!value.toLowerCase().startsWith("http://")
                    && !value.toLowerCase().startsWith("https://")) {
                    control.setErrors({ invalidUrlStart: true });
                    return false;
                }
            }
        }

        if (this.field.FieldType === "Link") {
            if (this.form.controls[this.field.FieldName + '_Name'] === undefined
                || this.form.controls[this.field.FieldName + '_Name'].disabled
                || this.form.controls[this.field.FieldName + '_Url'] === undefined
                || this.form.controls[this.field.FieldName + '_Url'].disabled
            ) {
                return true;
            }

            return this.form.controls[this.field.FieldName + '_Url'].valid;
        }


        const numInputs = document.querySelectorAll('input[type=number]');

        for (let i = 0; i < numInputs.length; i++) {
            const elem = numInputs[i] as HTMLInputElement;

            if (elem.validity.badInput && elem.validationMessage === $localize`Please enter a number.`) {
                if (this.field.FieldType === 'Number' && this.field.FieldName === elem.name) {
                    this.form.controls[this.field.FieldName].setErrors({ integer: true });
                }
                if (this.field.FieldType === 'Decimal' && this.field.FieldName === elem.name) {
                    this.form.controls[this.field.FieldName].setErrors({ number: true });
                }
            }

            if (this.field.FieldType === 'Number') {
                if (elem.value.split('.').length > 1
                    || elem.value.split('+').length > 1
                    || (elem.value.indexOf('-') !== 0 && elem.value.split('-').length > 1)
                    || elem.value.split('e').length > 1
                    || elem.value.split('E').length > 1
                ) {
                    if (this.field.FieldName === elem.name) {
                        this.form.controls[this.field.FieldName].setErrors({ integer: true });
                    }
                }
                else if (elem.name === "ValidForDays") {
                    if (+elem.value < 1 || +elem.value > 365) {
                        this.form.controls[this.field.FieldName].setErrors({ validDay: true });
                    }
                }
            } else if (this.field.FieldType === 'Decimal') {
                if (elem.value.split('.').length > 2
                    || elem.value.split('+').length > 1
                    || (elem.value.indexOf('-') !== 0 && elem.value.split('-').length > 1)
                    || elem.value.split('e').length > 1
                    || elem.value.split('E').length > 1
                ) {
                    if (this.field.FieldName === elem.name) {
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
        const errors = this.form.controls[field].errors;

        if (typeof this.form.controls[field] === "undefined") {
            return '';
        }

        if (!errors) {
            return '';
        }

        if (errors["pattern"]) {
            message += this.regexErrorMessage;
        }

        if (errors["number"]) {
            message += $localize`Please enter a valid number.`;
        }

        if (errors["integer"]) {
            message += $localize`Please enter a valid integer. `;
        }

        if (errors["maxlength"]) {
            message += $localize`${this.currentFieldName} maximum length of ${errors["maxlength"].requiredLength} characters exceeded.  Current length is [${errors["maxlength"].actualLength}] `;
        }

        if (errors["minlength"]) {
            message += $localize`${this.currentFieldName} minimum length of ${errors["minlength"].requiredLength} characters not met.  Current length is [${errors["minlength"].actualLength}] `;
        }

        if (errors["validDay"]) {
            message += $localize`Value cannot be less than 1 or greater than 365.`;
        }

        if (errors["max"]) {
            message += $localize`Please enter a maximum value of ${errors["max"].max} `;
        }

        if (errors["min"]) {
            message += $localize`Please enter a minimum value of ${errors["min"].min} `;
        }

        if (errors["alreadyExists"]) {
            message += $localize`A ${this.object.toLowerCase()} with this name already exists, please enter a unique name.`;
        }

        if (errors["hasPipe"]) {
            message += $localize`Tag name should not have pipe symbol '|' in name!`;
        }

        if (errors["alreadyExistsProcess"]) {
            message += $localize`Please enter a unique name.`;
        }

        if (errors["invalidUrlStart"]) {
            message += $localize`Please start the URL with a protocol prefix eg.http:// or https://`;
        }

        if (errors["nonUniqueField"]) {
            message += $localize`Please enter a unique value`;
        }

        if (errors["nonUniqueFieldCombination"]) {
            message += $localize`Please enter a unique combination of key field values`;
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
        const asString = '' + e.target.value;
        const val = +e.target.value;
        const newVal = +val.toFixed(precision);

        if (e === null || e.target === null || precision === null || typeof precision === "undefined") {
            return;
        }

        if (asString.split('.').length > 1 && asString.split('.')[1].length < precision) {
            return;
        }

        if (newVal !== null && (newVal !== 0 || newVal !== +val) && !isNaN(newVal)) {
            this.form.controls[this.field.FieldName].setValue(newVal);
            this.field.Value = newVal;
        }
    }

    private clamp(e: any, min: number, max: number, precision: number) {
        const val = e.target.value;
        const newVal = FormHelpers.clamp(val, min, max, precision);

        if (e === null || e.target === null || min === null || max === null) {
            return;
        }

        if (newVal !== null && (newVal !== 0 || newVal !== +val) && !isNaN(newVal)) {
            this.form.controls[this.field.FieldName].setValue(newVal);
            this.field.Value = newVal;
        }
    }

    OnBlurTrim() {
        const value: string = this.form.controls[this.field.FieldName].value;

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
    }

    private onSelect(e: EditorDropDownItem) {
        if (e !== null) {
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

    private onEditorChange(event: any) {
        if (event === null || event.field === null) { return; }

        const field = event.field;


        if (this.field.FieldType === 'Relationship') {
            this.filterSemanticRelationItems(field);
        }
    }

    private filterSemanticRelationItems(field: any) {

        if (field.FieldName === this.field.FieldName)
            {return;}

        if (this.field.FieldType === 'Relationship' && this.field.IsSemantic === true) {
            if (field.FieldType === 'Relationship' && field.IsSemantic === true) {
                if (field.Items === null) {
                    return;
                }

                const selectedItems = field.Value.split(',');

                field.Items.forEach((i) => {
                    const selected = selectedItems.findIndex((s) => s === i.Value) > -1;
                    if (selected) {
                        const ix = this.field.Items.findIndex((r) => r.Value === i.Value);

                        if (ix > -1) {
                            const item = this.field.Items.slice()[ix];
                            this.field.Items.splice(ix, 1);
                            if (this.excludedRelationitems[field.FieldName] === null) {
                                this.excludedRelationitems[field.FieldName] = [];
                            }
                            this.excludedRelationitems[field.FieldName].push(item);
                        }

                    } else {
                        if (this.excludedRelationitems[field.FieldName] === null) {
                            return;
                        }
                        const ix = this.excludedRelationitems[field.FieldName].findIndex((r) => r.Value === i.Value);
                        if (ix > -1) {
                            const item = this.excludedRelationitems[field.FieldName].slice()[ix];
                            this.excludedRelationitems[field.FieldName].splice(ix, 1);
                            this.field.Items.push(item);
                        }
                    }
                });
            }
            this.ref.markForCheck();
        }
    }

    isRequired() {

        if (this.field.FieldType === "Link") {
            var nameControl = this.form.controls[this.field.FieldName + '_Name'];
            if (nameControl.value) {
                return true;
            }
        }

        return this.field.Validations && this.field.Validations.some((x) => x.rule === 'required') === true;
    }

    getPlaceholder() {
        if (this.isRequired()) {
            return $localize`Value required`;
        }
        else {
            return $localize`Optional`;
        }
    }

    getfilterplaceholder() {
        var strfiltePH = $localize`Search colors`;
        if (this.field) {
            if (this.field.Name !== null) {
                if (this.selectedObject === 'TaskType' && this.field.FieldName === 'GovernanceRole') {
                    strfiltePH = $localize`Search roles`;
                }
            }
        }
        return strfiltePH;
    }


    lookupSelectedValue: any[] = [];
    lookupValues: any[] = [];
    lookupSub: Subscription;

    get lookupSelectPlaceholder(): string {
        if (this.field && this.field.ParentFieldTypeName && this.field.ParentFieldTypeName.length > 0) {
            return $localize`Select a ${this.field.ParentFieldTypeName}`;
        }
        return this.field.Required ? $localize`Value Required` : $localize`Optional`;
    }

    get isLookupFieldDisabled(): boolean {
        if (this.field && this.field.ParentFieldTypeName && this.field.ParentFieldTypeName.length > 0) {
            return !this.lookupParentValue;
        }
        return false;
    }

    lookupFieldClicked($event) {
        if (this.isLookupFieldDisabled) {
            return;
        }
        this.overlayPanel.toggle($event);
    }

    get lookupParentValue(): string {
        if (this.field && this.field.ParentFieldTypeName && this.field.ParentFieldTypeName.length > 0) {
            var pField = this.form.controls[this.field.ParentFieldTypeName];
            if (pField && pField.value) {
                if (Array.isArray(pField.value)) {
                    return (pField.value as string[]).join(",");
                }
                else {
                    return pField.value as string;
                }
            }
        }
        return null;
    }

    lastParams: any;
    loadListLazy($params) {
        var loadParams: any = { skip: $params.first, take: $params.rows, filter: $params.globalFilter ?? "" };
        loadParams["isForAssetForm"] = true;
        loadParams["assetUid"] = this.assetUid;
        this.hadInitialLazyLoad = true;

        if ($params.globalFilter) {
            loadParams["filter"] = $params.globalFilter;
        }

        if (this.lookupParentValue) {
            loadParams["lookupParentValue"] = this.lookupParentValue;
        }

        this.isLookupValuesLoading = true;

        if (this.lookupSub) {
            this.lookupSub.unsubscribe();
        }

        this.lookupSub = this.fieldsService.getLookupValues(this.assetTypeUid, this.field.FieldName, loadParams).subscribe((res) => {
            if (!this.lookupValues || this.lookupValues.length === 0) {
				this.lookupValues = Array.from({ length: res.count }, () => { return { label: null, value: null, color: null }; });
            }

            if (this.lookupValues.length > 10 || loadParams["filter"]) {
                this.showLookupSearchField = true;
            }
            else {
                this.showLookupSearchField = false;
            }

            const loadedData = [];

            res.items.forEach((str) => {
				const color = str.color;
                loadedData.push({ label: str.text, value: str.value, ...(color && { color }) });
            });

            Array.prototype.splice.apply(this.lookupValues, [...[loadParams.skip, loadParams.take], ...loadedData]);

            this.lookupValues = [...this.lookupValues];

            if (this.lookupValues.length > res.count) {
                this.lookupValues = this.lookupValues.slice(0, res.count);
            }
            this.isLookupValuesLoading = false;
            this.lastParams = loadParams;
			
			if (!this.field.MultiSelect) {
				const selectedItem = this.lookupValues.find((lookup) => {
					return lookup.value?.toLowerCase() === this.dropdown.value?.toLowerCase();
				});
				if (selectedItem?.value) {
					this.selectSingleItem(null, selectedItem);
				}
			}

			// Until we can use the PrimeNG dropdown filter template https://github.com/primefaces/primeng/issues/11628
			// The Virtual scroll will shrink the dropdown container if there is less than 10 items in the list, and will not
			// take the filter input field into account, so the area shrinks too much.
			// By adding an empty element and hiding it, the area size is usable.
			// This should be redone once PrimeNG has been updated to 14.x
			if (this.showLookupSearchField && this.lookupValues.length < 10) {
				this.lookupValues.push({label:null, value: null});
			}
			
            this.lookupValues = JSON.parse(JSON.stringify(this.lookupValues));
            this.setSelectionVirtualScrollHeight();
            this.ref.detectChanges();
        });
    }

    onItemSelected(event) {
    }

    onDropdownChange($event: {originalEvent: PointerEvent; value: any}): void {
        if($event.value === null) {
            this.lookupSelectedValue = [];
        }
    }

    //table extensions
    selectSingleItem(event: MouseEvent, item: SelectItem) {
        if (this.field?.MultiSelect) {
            this.field.Items = [...this.lookupSelectedValue];
            const value = this.lookupSelectedValue.map((s) => s.value);
            this.form.controls[this.field.FieldName].setValue(value);
        } else {
            this.lookupSelectedValue = [item];
			if (this.dropdown) {
				this.dropdown.options = [item];
			}
            this.form.controls[this.field.FieldName].setValue(item.value);
        }
        if (event) {
			if (!this.field.MultiSelect) {
				this.overlayPanel.hide();
			}
            this.dynEditorService.updateLookupValue({ assetUid: this.assetUid, fieldName: this.field.FieldName, fieldValue: this.field.Value });
        }
    }

    hexToRgb(hex: string): string {
        if (!hex) {
            return "";
        }

        if (!hex.startsWith("#")) {
            return hex;
        }
        // Expand shorthand form (e.g. "03F") to full form (e.g. "0033FF")
        var shorthandRegex = /^#?([a-f\d])([a-f\d])([a-f\d])$/i;
        hex = hex.replace(shorthandRegex, function (m, r, g, b) {
            return r + r + g + g + b + b;
        });

        var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        var data = result ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        } : null;

        return data ? `rgb(${data.r},${data.g},${data.b})` : '';
    }

    onChangeMultiselect($event) {
        var values = this.form.controls[this.field.FieldName].value as any[];
        var newValues = [];
        this.lookupSelectedValue.forEach((item) => {
            if (values.indexOf(item.value) !== -1) {
                newValues.push(item);
            }
        });
        this.lookupSelectedValue = newValues;

        if (event) {
            this.dynEditorService.updateLookupValue({ assetUid: this.assetUid, fieldName: this.field.FieldName, fieldValue: this.field.Value });
        }
    }

    setSelectionVirtualScrollHeight() {
        try {
            let count: number = 0;
            let res = [];

            if (!this.dataTable || !this.dataTable.value) {
                return;
            }

            var filter = this.dataTable?.filters?.global ? (this.dataTable?.filters?.global["value"] as string) : "";
            if (!filter || !this.dataTable.filteredValue) {
                res = new Array(this.dataTable.value.length);
            }
            else {
                res = new Array(this.dataTable.filteredValue.length);
            }
            if (res.length) {
                count = res.length;
            }

            let calculatedHeight: number = 0;
            const maxHeight: number = 320;
            const minHeight: number = 50;

            if (count < 10) {
                calculatedHeight = count * 32;
                if (calculatedHeight < 32) {
                    calculatedHeight = 32;
                }

            }
            else {
                calculatedHeight = maxHeight;
            }

            if (calculatedHeight > maxHeight) {
                calculatedHeight = maxHeight;
            }
            if (calculatedHeight < minHeight) {
                calculatedHeight = minHeight;
            }
            this.selectionScrollHeight = calculatedHeight + "px";
        }
        catch (ex) {
            this.selectionScrollHeight = "320px";
        }
        this.ref.markForCheck();
    }

    getFieldTypeForSwitch(type: string) {
        if (type === 'Relationship' || type === 'Lookup') {
            return 'LazyLookup';
        }
        return type;
    }

    isJson(str) {
        try {
            JSON.parse(str);
        } catch (e) {
            return false;
        }

        //number will pass as a json so need to handle that case
        if (!isNaN(parseInt(str))) {
            return false;
        }

        return true;
    }
	
	onItemInfoClick($event: MouseEvent, objectID: string): void {
		$event.stopPropagation();
		this.sidePanelSelectionChange.emit({ objectID: objectID.toLowerCase(), fieldName: this.field.FieldName });
	}
}
