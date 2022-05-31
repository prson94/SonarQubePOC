import * as _ from 'lodash';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    ElementRef,
    ViewEncapsulation,
    ViewChildren,
    QueryList,
    HostListener,
    AfterViewChecked,
    SimpleChanges
} from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { SemanticMatchType, SemanticSource, SemanticType } from '../../../models/semantic-type.model';
import { DataProfileService } from '../../../services/dataprofile.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../base.component';
import { LocaleService } from '../../../services/locale.service';
import { PropertyGroupComponent } from '../controls/property-group/property-group.component';
import { AppSettingsEnum } from '../../../models/settings.model';

@Component({
    selector: 'semantic-editor',
    templateUrl: './semantic-editor.component.html',
    providers: [DataProfileService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['semantic-editor.less']
})

export class SemanticEditorComponent extends BaseComponent implements OnChanges, OnInit, AfterViewChecked {
    @Input() semanticType: SemanticType;
    @Input() dataProfile: any = null;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    isBuiltIn: boolean = false;
    statuses: any[];
    baseTypes: any;
    baseTypeOptions: any[];
    matchTypes: any[];
    savingInProgress: boolean = false;
    hasHeader: boolean = false;
    isInError: boolean = false;
    model: SemanticType;
    semanticForm: FormGroup;
    hasFormChanged: boolean = false;
    isInErrorMessage: string = "";
    modalFormMaxHeight = 400;
    advancedJson: string = "";

    locales: any[];
    isEdit: boolean = false;
    savingInProgressWithAddNew: boolean = false;
    isDuplicateQualifier: boolean = false;
    isJsonValid: boolean = true;
    semanticHelpURL: string;

    @ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

    get editorTitle(): string {
        return !this.semanticType ? $localize`Create` : $localize`Edit` + $localize`Semantic Type`;
    }

    get submitButtonLabel(): string {
        return this.semanticType ? $localize`Save Changes` : $localize`Create`;
    }

    constructor(
        private cdRef: ChangeDetectorRef,
        private formBuilder: FormBuilder,
        private messagesService: MessagesObservableService,
        private dataProfileService: DataProfileService,
        protected settingsService: CompanySettingsService,
        private localService: LocaleService,
        private elRef: ElementRef
    ) {
        super(settingsService);
    }

    ngOnInit(): void {
        this.isLoading = true;

        this.semanticHelpURL = `${this.settingsService.getAppSetting(AppSettingsEnum.HelpBaseUri)}Default.htm#c-user-guide/create-semantic-types.htm#Data_Profiler`;

        this.semanticForm = this.formBuilder.group({
            name: ['', [Validators.required, this.isEmptyString()]],
            description: null,
            effectiveDate: null,
            threshold: ['', [Validators.required]],
            priority: ['', [Validators.required]],
            matchType: null,
            baseType: null,
            qualifier: null,
            headerConfidence: null,
            minSamples: null,
            validValues: null,
            invalidValues: null,
            advancedJson: null,
            statuses: null,
            minMaxPresent: null,
            minimum: null,
            maximum: null,
            headerRegExp: null,
            regExpReturned: null,
            validLocales: null
        });
        setTimeout(() => {
            this.semanticForm.valueChanges.subscribe((change) => {
                this.isEdit = this.semanticType?.qualifier?.length > 0;
                if ((this.semanticType && JSON.stringify(this.semanticType) !== JSON.stringify(change))
                    ||
                    (!this.semanticType && JSON.stringify(new SemanticType()) !== JSON.stringify(change))) {
                    this.hasFormChanged = true;
                } else {
                    this.hasFormChanged = false;
                }
            });
        }, 500);

        this.populateTypeLists();
    }
    ngOnChanges(changes: SimpleChanges): void {
        let c = changes;
        if (this.semanticType) {
            this.model = _.cloneDeep(this.semanticType);
            this.isBuiltIn = this.semanticType.source.toString() === SemanticSource[SemanticSource.BuiltIn];
            this.isEdit = true;

            if (this.semanticType.matchType.toString() === SemanticMatchType[SemanticMatchType.Advanced]) {
                this.advancedJson = JSON.stringify(this.semanticType.advanced, null, 2);
            }
        } else {
            this.isEdit = false;
            this.model = new SemanticType();
            if (this.dataProfile) {
                this.populateModelFromDataProfile();
            }
        }
        this.cdRef.markForCheck();

        this.populateTypeLists();
    }
    populateModelFromDataProfile() {
        this.model.qualifier = this.dataProfile?.typeQualifier;
        this.model.threshold = Math.floor(this.dataProfile?.confidence * 100);
        this.model.baseType = this.dataProfile?.type;
    }

    public isFormValid(): boolean {
        if (!this.semanticForm) {
            return false;
        }
        return this.semanticForm.valid;
    }

    onSubmit(addAnother: boolean = false) {
        this.savingInProgress = true;

        if (addAnother) {
            this.savingInProgressWithAddNew = true;
        }

        if (this.model.matchType.toString() === SemanticMatchType[SemanticMatchType.Advanced]) {
            this.model.advanced = JSON.parse(this.advancedJson);
        }

        this.clearInvalidFields();

        if (this.isEdit) {
            if (this.isBuiltIn) {
                this.dataProfileService.patchSemanticType(this.model)
                    .subscribe((res) => {
                        this.handleSaveComplete(res, addAnother);
                    });
            } else {
                this.dataProfileService.putSemanticType(this.model)
                    .subscribe((res) => {
                        this.handleSaveComplete(res, addAnother);
                    },
                        (err) => {
                            this.savingInProgress = false;
                            this.savingInProgressWithAddNew = false;
                        }
                    );
            }
        } else {
            this.dataProfileService.postSemanticType(this.model).subscribe((res) => {
                this.handleSaveComplete(res, addAnother);
            });
        }
    }

    handleSaveComplete(res: any, addAnother: boolean = false) {
        if (!(res?.status)) {
            let msg = this.isEdit ? $localize`Successfully updated` : $localize`Successfully created'}`;
            this.showMessageForResult(this.messagesService, res, msg);
            this.savingInProgress = false;
            this.savingInProgressWithAddNew = false;
            if (addAnother) {
                this.model = new SemanticType();
                this.semanticForm.reset();
            }
            this.saveClick.emit({ item: res[0], action: `${this.isEdit ? $localize`Edit` : $localize`New`}`, addAnother });
        }
        else {
            this.savingInProgress = false;
            this.savingInProgressWithAddNew = false;
            if (res?.status === 409) {
                this.isDuplicateQualifier = true;
            }
        }
        this.cdRef.markForCheck();
    }

    populateTypeLists() {
        if (!this.matchTypes || !this.baseTypes || !this.statuses || !this.locales) {
            this.isLoading = true;
            this.dataProfileService.getSemanticLookupList("matchtypes", false, null, "none").subscribe((matchRes) => {
                this.matchTypes = matchRes.map((matchType) => { return { label: matchType.Name, value: matchType.Value, description: matchType.Description }; });
                this.dataProfileService.getSemanticLookupList("basetypes").subscribe((baseRes) => {
                    this.baseTypes = baseRes;
                    this.getBaseTypeOptions();
                    this.dataProfileService.getSemanticLookupList("statuses").subscribe((statusRes) => {
                        this.statuses = statusRes.map((status) => { return { label: status.Name, value: status.Value }; });
                        this.localService.getLocales().subscribe((locales) => {

                            this.locales = locales;

                            this.locales.forEach((i) => {
                                i.label = i.locale;
                                i.value = i.locale;
                            });
                            this.isLoading = false;
                        });
                    });
                });
            });
        }
        this.isLoading = false;
    }

    isEmptyString(): ValidatorFn {
        type NewType = AbstractControl;

        return (control: NewType): { [key: string]: any } | null => {
            if (control.value === null || control.value === undefined) {
                return {};
            }
            if ((control.value as string).trim() === '' && (control.value as string) !== '') {
                return {
                    empty: { value: control.value }
                };
            }
            return null;
        };
    }

    isValid(): boolean {

        if (this.model?.matchType?.toString() === SemanticMatchType[SemanticMatchType.Advanced]) {
            return this.isJsonValid;
        }
        if (this.model?.matchType?.toString() === SemanticMatchType[SemanticMatchType.Number]) {
            return this.validateMinMax();
        }
        return true;
    }

    getMatchTypeDescription(matchType: any) {
        return this.matchTypes.filter((m) => (m.label === matchType.label))[0].description;
    }

    @HostListener('window:resize', ['$event'])
    onResize(event) {
        this.setFormHeight();
    }

    ngAfterViewChecked() {
        this.setFormHeight();
    }

    private setFormHeight() {
        var groupsHeight = 0;
        var topPos = 260;
        if (this.elRef.nativeElement) {
            var els = this.elRef.nativeElement.getElementsByClassName('form-wrapper');
            if (els[0]) {
                var rect = els[0].getBoundingClientRect();
                topPos = rect.top + 120;
            }
        }
        var maxHeight = window.innerHeight - topPos;
        if (this.propertyGroups) {
            var a = this.propertyGroups.first;
            this.propertyGroups.forEach((pg) => {
                var height = pg.inputContainer.nativeElement.offsetHeight;
                groupsHeight += height !== 0 ? (height + 34) : 34;
            });
        }

        this.modalFormMaxHeight = groupsHeight > maxHeight ? maxHeight : groupsHeight;
        this.cdRef.markForCheck();
    }

    expandChanged() {
        setTimeout(() => this.setFormHeight(), 10);
    }

    onMatchTypeChanged() {
        this.getBaseTypeOptions();
        if (this.dataProfile) {
            switch (this.model.matchType.toString().toLocaleLowerCase()) {
                case "advanced":
                    break;
                case "list":
                    this.model.invalidList = this.dataProfile?.outlierDetail.map(({ key }) => key);
                    break;
                case "number":
                    this.model.regExpReturned = this.dataProfile?.regExp;
                    this.model.minimum = this.dataProfile?.min;
                    this.model.maximum = this.dataProfile?.max;
                    break;
                case "pattern":
                    this.model.invalidList = this.dataProfile?.outlierDetail.map(({ key }) => key);
                    this.model.regExpReturned = this.dataProfile?.regExp;
                    break;
            }
        }
    }

    get cancelButtonText(): string {
        if (!this.isEdit) {
            return $localize`Cancel`;
        }

        if (this.hasFormChanged && this.isEdit) {
            return $localize`Discard Changes`;
        }

        return $localize`Close`;
    }

    getBaseTypeOptions() {
        if (this.model?.matchType?.toString() === SemanticMatchType[SemanticMatchType.Number]) {
            this.baseTypeOptions = this.baseTypes.filter((b) => b.Value === "Double" || b.Value === "Long").map((baseType) => { return { label: baseType.Name, value: baseType.Value }; });
        } else {
            this.baseTypeOptions = this.baseTypes.map((baseType) => { return { label: baseType.Name, value: baseType.Value }; });
        }
    }

    clearInvalidFields() {
        let allowedFields = ["name", "qualifier", "description", "threshold", "priority", "status", "matchType", "baseType", "source"];
        if (this.model.matchType.toString() === SemanticMatchType[SemanticMatchType.List]) {
            allowedFields = [
                ...allowedFields,
                ...["validList", "invalidList", "minSamples", "validLocales", "headerRegExps", "headerRegExpConfidence"]
            ];
        }
        if (this.model.matchType.toString() === SemanticMatchType[SemanticMatchType.Number]) {
            allowedFields = [
                ...allowedFields,
                ...["minimum", "maximum", "minSamples", "minMaxPresent", "regExpReturned", "headerRegExps", "headerRegExpConfidence"]
            ];
        }
        if (this.model.matchType.toString() === SemanticMatchType[SemanticMatchType.Pattern]) {
            allowedFields = [
                ...allowedFields,
                ...["regExpReturned", "invalidList", "minSamples", "validLocales", "headerRegExps", "headerRegExpConfidence"]
            ];
        }
        if (this.model.matchType.toString() === SemanticMatchType[SemanticMatchType.Advanced]) {
            allowedFields.push("advanced");
        }

        //clear invalid fields to prevent request 
        Object.keys(this.model).forEach((key) => {
            if (!allowedFields.find((x) => x === key)) {
                this.model[key] = null;
            }
        });

    }

    validateMinMax() {
        if (this.model?.minimum && this.model?.maximum && this.model.minimum > this.model.maximum) {
            return false;
        }
        return true;
    }
}