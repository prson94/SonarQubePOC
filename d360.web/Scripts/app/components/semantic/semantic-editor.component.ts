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
import { SemanticMatchType, SemanticSource, SemanticType } from '../../models/semantic-type.model';
import { DataProfileService } from '../../services/dataprofile.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CompanySettingsService } from '../../services/settings.service';
import { BaseComponent } from '../shared/base.component';
import { LocaleService } from '../../services/locale.service';
import { PropertyGroupComponent } from '../shared/controls/property-group/property-group.component';

@Component({
    selector: 'semantic-editor',
    templateUrl: './semantic-editor.component.html',
    providers: [DataProfileService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['semanticTypes.less']
})

export class SemanticEditorComponent extends BaseComponent implements OnChanges, OnInit, AfterViewChecked {
    @Input() semanticType: SemanticType;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    isBuiltIn: boolean = false;
    statuses: any[];
    baseTypes: any[];
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

    @ViewChildren(PropertyGroupComponent) propertyGroups: QueryList<PropertyGroupComponent>;

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
        this.semanticForm = this.formBuilder.group({
            name: ['', [Validators.required, this.isEmptyString()]],
            description: null,
            effectiveDate: null,
            threshold: ['', [Validators.required, this.isValidPercentage()]],
            priority: ['', [Validators.required, this.isValidNumber()]],          
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
        }
        this.cdRef.markForCheck();

        this.populateTypeLists();        
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
        
        if (this.isEdit) {
            this.dataProfileService.patchSemanticType(this.model)
                .subscribe((res) => {
                    if (!(res?.status)) {
                        let msg = 'Successfully updated';
                        this.showMessageForResult(this.messagesService, res, msg);                        
                        this.savingInProgress = false;
                        this.savingInProgressWithAddNew = false;     
                        this.saveClick.emit({ item: this.model, action: 'Edit', addAnother });
                    }
                    else {
                        this.savingInProgress = false;
                        this.savingInProgressWithAddNew = false;
                        if (res.status === 409) {
                            this.isDuplicateQualifier = true;
                        }                                        
                    }
                    this.cdRef.markForCheck();
                });
        } else {            
            this.dataProfileService.postSemanticType(this.model).subscribe((res) => {
                if (!(res?.status)) {
                    let msg = 'Successfully created';
                    this.showMessageForResult(this.messagesService, res, msg);
                    this.savingInProgress = false;
                    this.savingInProgressWithAddNew = false;
                    if (addAnother) {
                        this.model = new SemanticType();
                        this.semanticForm.reset();
                    }
                    this.saveClick.emit({ item: res[0], action: "new", addAnother });
                }
                else {
                    this.savingInProgress = false;
                    this.savingInProgressWithAddNew = false;
                    if (res.status === 409) {
                        this.isDuplicateQualifier = true;
                    }
                }
                this.cdRef.markForCheck();                
            });            
        }        
    }

    populateTypeLists() {
        if (!this.matchTypes) {
            this.isLoading = true;
            this.dataProfileService.getSemanticLookupList("matchtypes").subscribe((matchRes) => {
                this.matchTypes = matchRes.map((matchType) => { return { label: matchType.Name, value: matchType.Value, description: matchType.Description }; });
                this.dataProfileService.getSemanticLookupList("basetypes").subscribe((baseRes) => {
                    this.baseTypes = baseRes.map((baseType) => { return { label: baseType.Name, value: baseType.Value }; });
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
            if (control.value === null) {
                return {};
            }
            if ((control.value as string).trim() === '' && (control.value as string) != '') {
                return {
                    empty: { value: control.value }
                };
            }                
            return null;
        };
    }

    isValidPercentage(): ValidatorFn {
        type NewType = AbstractControl;
        return (control: NewType): { [key: string]: any } | null => {
            if (control.value === null || control.value === undefined) {
                return {};
            }                
            if ((control.value as number) < 1 || (control.value as number) > 100) {
                return {
                    outOfRange: { value: control.value }
                };
            }                
            return null;
        };
    }

    isValidNumber(): ValidatorFn {
        type NewType = AbstractControl;
        return (control: NewType): { [key: string]: any } | null => {
            if (control.value === null || control.value === undefined) {
                return {};
            }
            if ((control.value as number) < 1) {
                return {
                    outOfRange: { value: control.value }
                };
            }                
            return null;
        };
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

}