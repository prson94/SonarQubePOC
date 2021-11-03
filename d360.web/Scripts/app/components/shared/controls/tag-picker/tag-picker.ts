import { Input, Component, Output, EventEmitter, NgModule, ViewChild, ElementRef, forwardRef, ChangeDetectorRef, ViewEncapsulation, OnDestroy, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NG_VALUE_ACCESSOR, ControlValueAccessor, FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { TagService } from '../../../../services/tag.service';
import { Subscription } from 'rxjs';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { TagType, TagPermissionItem } from '../../../../models/tag.model';
import { BaseComponent } from '../../base.component';
import { SelectItem } from 'primeng/api';
import { CompanySettingsService } from '../../../../services/settings.service';


export const SWITCH_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => TagPicker),
    multi: true
};


@Component({
    selector: 'ig-tag-picker',
    templateUrl: 'tag-picker.html',
    providers: [SWITCH_VALUE_ACCESSOR],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ['./tag-picker.less']
})
export class TagPicker extends BaseComponent implements ControlValueAccessor, OnDestroy {

    @Input() disabled = false;

    @Input() readOnly = false;

    @Input() styleClass: string;

    @Input() style: any;

    @Input() tabindex: number = 0;

    @Input() inputId: string;

    @Output() onChange: EventEmitter<any> = new EventEmitter();

    @Output() onSelect: EventEmitter<any> = new EventEmitter();

    @Output() onUnselect: EventEmitter<any> = new EventEmitter();

    @Input() assetUid: string = '00000000-0000-0000-0000-000000000000';

    value: Array<SelectItem> = [];  // this is intentionally NOT an input you should be using ngModel..

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };

    private tagAutocompleteValue: string = '';
    private savingTag: boolean = false;

    private searchSub: Subscription;
    private searchResults: SelectItem[] = [];

    private tagTooltip: TagType;
    private isTooltipLoaded: boolean = false;

    private arePermissionsLoaded: boolean = false;
    private tagPermissions: TagPermissionItem[] = [];

    constructor(protected changeDetectorRef: ChangeDetectorRef,
        private tagService: TagService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }
    @ViewChild("tagPicker", { static: false }) _el: ElementRef;

    tryChangeValue(val: SelectItem[]) {
        if (!this.disabled) {
            this.writeValue(val);
        }
    }

    tryAddValue(val: SelectItem) {
        if (!this.disabled) {
            var newValue: SelectItem[] = [];
            if (this.value)
                newValue = this.value;

            if (this.value && this.value.some(x => x.title.trim().toLowerCase() == val.title.trim().toLowerCase()))
                return;

            newValue.push(val);

            this.tagPermissions.push({ Value: val.title, Uid: val.value, CanDelete: true });
            this.writeValue(newValue);
        }
    }

    writeValue(obj: SelectItem[]): void {
        if (this._el) this._el.nativeElement.focus();

        this.value = obj;
        this.onModelChange(this.value);
        this.onChange.emit(this.value);
        this.checkPermissions();
        this.changeDetectorRef.markForCheck();
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled
    }

    removeItem(tag: SelectItem) {
        var newValue = this.value.filter(x => x.title != tag.title);
        this.tryChangeValue(newValue);
        this.onUnselect.emit(tag);
    }

    ngOnDestroy() {
        if (this.searchSub)
            this.searchSub.unsubscribe();
    }

    private highlight(item, input) {
        if (!input) {
            return item;
        }
        return item.replace(new RegExp(input, "gi"), match => {
            return '<span style="background: #F5FF57;">' + match + '</span>';
        });
    }

    resetValue() {
        this.tagAutocompleteValue = null;
    }

    checkKey(event, value) {
        if (event.key == "Enter" && !this.savingTag) {
            if (typeof value == 'string') {
                this.saveTag({ title: value, value: '' });
            }
            else {
                this.saveTag(value);
            }
        }
    }

    isTagValid(item: SelectItem) {
        var isAssigned = false;
        if (this.value) {
            this.value.forEach(x => {
                if (x.title.trim().toLowerCase() == item.title.trim().toLowerCase()) {
                    this.messagesService.showError('Error', 'Tag already assigned');
                    isAssigned = true;
                }
            })
        }
        if (isAssigned)
            return false;

        if (item.title.includes("|")) {
            this.messagesService.showError('Error', "Tag can't contain | character");
            return false;
        }
        if (item.title.length < 1) {
            this.messagesService.showError('Error', "Tag must be as least 1 character long in length");
            return false;
        }
        if (item.title.length > 100) {
            this.messagesService.showError('Error', "Tag must be less then 100 characters in length");
            return false;
        }
        return true;
    }

    search(event, searchValue) {
        if (this.searchSub)
            this.searchSub.unsubscribe();

        this.searchSub =
            this.tagService.searchTagsTypeAhead(searchValue, 10)
                .subscribe(res => {
                    if (res && res.length > 0) {
                        var sorted = res.sort((a, b) => a.name.localeCompare(b.name));
                        this.searchResults = [];
                        sorted.forEach(tag => {
                            this.searchResults.push({ value: tag.code, title: tag.name })
                        });
                    }
                    else if (res && res.length == 0) {
                        this.searchResults = [];
                    }
                    this.changeDetectorRef.markForCheck();


                }, err => { console.log(err) });

    }

    saveTag(event: SelectItem) {
        if (!this.isTagValid(event)) {
            return;
        }
        this.savingTag = true;

        var tagType = new TagType();
        tagType.Value = event.title;
        this.tagService.doesTagExist(tagType)
            .subscribe((result) => {
                if (result == 200) {
                    this.tryAddValue(event);
                    this.onSelect.emit(event);
                    this.tagAutocompleteValue = '';
                }
            },
                (error) => {
                    if (error.status == 404) {
                        this.tagService.saveTag(tagType)
                            .subscribe(result => {
                                let msg: string = '';
                                if (result.Value != undefined) {
                                    result.message = `${result.Value} succesfully created`;
                                }
                                this.showMessageForResult(this.messagesService, result, msg);
                                this.tryAddValue({ value: result.uid, title: result.Value });
                                this.onSelect.emit({ value: result.uid, title: result.Value });
                                this.tagAutocompleteValue = '';
                            });
                    }
                },
                () => {
                })
        this.savingTag = false;
    }

    enter(tag: SelectItem, element: HTMLElement) {
        if (this.disabled) return;

        var box = element.getBoundingClientRect();
        var el = this._el.nativeElement as HTMLElement;
        var tooltip = el.getElementsByClassName('tooltip-wrapper')[0] as HTMLElement;

        tooltip.style.display = 'block';
        tooltip.style.top = (box.top - 42) + 'px';
        tooltip.style.left = (box.left - 8) + 'px';

        this.isTooltipLoaded = false;
        this.tagService.getTagTooltip(tag.value, '', tag.title)
            .subscribe(t => {
                this.tagTooltip = t[0];
                this.isTooltipLoaded = true;

                this.changeDetectorRef.markForCheck();

                setTimeout(() => {
                    var tooltip = el.getElementsByClassName('tooltip-wrapper')[0] as HTMLElement;

                    var size = tooltip.getBoundingClientRect();
                    tooltip.style.top = (box.top - size.height - 6) + 'px';

                    this.changeDetectorRef.markForCheck();

                }, 1);


            });
    }

    leave() {
        if (this.disabled) return;

        var el = this._el.nativeElement as HTMLElement;
        var tooltip = el.getElementsByClassName('tooltip-wrapper')[0] as HTMLElement;
        tooltip.style.display = 'none';
        this.changeDetectorRef.markForCheck();
    }

    checkPermissions() {
        if (this.arePermissionsLoaded)
            return;


        //If there is no assetUid, asset id in creation so no permission check needed
        if (!this.assetUid) {
            this.arePermissionsLoaded = true;
            this.tagPermissions = [];
            return;
        }

        this.tagService.getTagPermissions(this.assetUid)
            .subscribe(permissions => {
                this.arePermissionsLoaded = true;
                this.tagPermissions = permissions;
                this.changeDetectorRef.markForCheck();
            })
    }

    canDeleteTag(tagValue: string) {
        if (!this.arePermissionsLoaded) return false;
        return this.tagPermissions.some(x => x.Value == tagValue && x.CanDelete == true);
    }

    get getStyleClasses(): string {
        let classes = 'tag-picker';
        classes += this.disabled ? ' disabled' : '';
        classes += this.styleClass ? this.styleClass + ' ' : '';
        return classes;
    }
}



@NgModule({
    imports: [
        CommonModule,
        TooltipModule,
        FormsModule,
        AutoCompleteModule
    ],
    declarations: [TagPicker],
    exports: [TagPicker],
    providers: [TagService]
})

export class TagPickerModule { }

