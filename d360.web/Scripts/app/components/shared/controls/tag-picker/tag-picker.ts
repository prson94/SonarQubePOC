import { Input, Component, Output, EventEmitter, OnInit, NgModule, ViewChild, ElementRef, forwardRef, ChangeDetectorRef, HostBinding, ViewEncapsulation, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NG_VALUE_ACCESSOR, ControlValueAccessor, FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { TagService } from '../../../../services/tag.service';
import { Subscription } from 'rxjs';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { TagType } from '../../../../models/tag.model';
import { BaseComponent } from '../../base.component';


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
    styleUrls: ['./tag-picker.css']
})
export class TagPicker extends BaseComponent implements ControlValueAccessor, OnDestroy {

    @Input() disabled = false;

    @Input() styleClass: any;

    @Input() style: any;

    @Input() tabindex: string;

    @Input() inputId: string;

    @Output() onChange: EventEmitter<any> = new EventEmitter();

    protected value: string = '';  // this is intentionally NOT public or an input you should be using ngModel..

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };

    private tagsArray: string[] = [];
    private tagAutocompleteValue: string = '';
    private savingTag: boolean = false;

    private searchSub: Subscription;
    private searchResults: any[] = [];

    constructor(protected changeDetectorRef: ChangeDetectorRef,
        private tagService: TagService,
        private messagesService: MessagesObservableService
    ) {
        super();
    }
    @ViewChild("tagPicker", { static: false }) _el: ElementRef;

    tryChangeValue(val: string) {
        if (!this.disabled) {
            this.writeValue(val);
        }
    }

    tryAddValue(val: string) {
        if (!this.disabled) {
            var newValue = '';

            if (this.tagsArray.some(x => x.trim().toLowerCase() == val.trim().toLowerCase()))
                return;

            if (this.value != '') {
                newValue = this.value + '|' + val;
            }
            else {
                newValue = val;
            }
            this.writeValue(newValue);
        }
    }

    writeValue(obj: string): void {
        if (this._el) this._el.nativeElement.focus();

        this.value = obj;
        if (this.value != undefined) {
            this.tagsArray = [];
            this.value.split('|')
                .forEach(tag => {
                    if (tag != '')
                        this.tagsArray.push(tag);
                })
        }
        this.onModelChange(this.value);
        this.onChange.emit(this.value);
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

    removeItem(tag: string) {
        this.tagsArray = this.tagsArray.filter(x => x != tag);
        this.tryChangeValue(this.tagsArray.join('|'));
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
            if (value) {
                event.Value = value.Value ? value.Value.trim() : value.trim();
                this.saveTag(event);
            }
        }
    }

    isTagValid(value: string) {
        var isAssigned = false;
        this.tagsArray.forEach(x => {
            if (x.trim().toLowerCase() == value.trim().toLowerCase()) {
                this.messagesService.showError('Error', 'Tag already assigned');
                isAssigned = true;
            }
        })
        if (isAssigned)
            return false;

        if (value.includes("|")) {
            this.messagesService.showError('Error', "Tag can't contain | character");
            return false;
        }
        if (value.length < 1) {
            this.messagesService.showError('Error', "Tag must be as least 1 character long in length");
            return false;
        }
        if (value.length > 100) {
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
                        this.searchResults = res.sort((a, b) => a.name.localeCompare(b.name));
                        this.changeDetectorRef.markForCheck();
                    }
                    else if (res && res.length == 0) {
                        this.searchResults = res;
                        this.changeDetectorRef.markForCheck();
                    }

                    this.searchResults.forEach(x => x.Value = x.name);

                }, err => { console.log(err) });

    }

    saveTag(event) {
        var tagValue = event.Value;

        if (!this.isTagValid(tagValue)) {
            return;
        }
        this.savingTag = true;

        var tagType = new TagType();
        tagType.Value = tagValue;
        this.tagService.doesTagExist(tagType)
            .subscribe(result => {
                if (result == null) {
                    this.tagService.saveTag(event)
                        .subscribe(result => {
                            let msg: string = '';
                            if (event.uid == undefined) {
                                msg = `${event.Value} succesfully created`;
                            }
                            this.showMessageForResult(this.messagesService, result, msg);
                            this.tryAddValue(tagValue);
                            this.tagAutocompleteValue = '';
                        });
                }
                else {
                    this.tryAddValue(tagValue);
                    this.tagAutocompleteValue = '';
                }
            })
        this.savingTag = false;
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

