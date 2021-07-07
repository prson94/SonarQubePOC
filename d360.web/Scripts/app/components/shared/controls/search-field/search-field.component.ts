import { Component, Input, Output, EventEmitter, NgModule, forwardRef, ViewEncapsulation, ViewChild, ElementRef, ChangeDetectorRef, HostListener, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../../base.component';

import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DirectivesModule } from '../../../../directives/directives.module';
import { TooltipModule } from 'primeng/tooltip';

import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';

import { Subject } from "rxjs";
import { debounceTime, distinctUntilChanged } from "rxjs/operators";

export const SEARCH_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => SearchFieldComponent),
    multi: true
};

@Component({
    selector: 'ig-search-field',
    templateUrl: 'search-field.component.html',
    providers: [SEARCH_VALUE_ACCESSOR],
    styleUrls: ['search-field.component.less'],
    encapsulation: ViewEncapsulation.None,
})

export class SearchFieldComponent implements ControlValueAccessor, OnInit, OnDestroy {
    @Input() mode: string = 'Enter';
    @Input() maxLength: number = 2500;
    @Input() placeholder: string = 'Search';
    @Input() tabindex: number = 0;
    @Input() disabled: boolean = false;
    @Input() debounce: number = 200;
    @Input() style: any;
    @Input() infoTooltip: string = "";
    @Input() darkMode: boolean = false;

    @Output() onSearch = new EventEmitter();

    hasValue: boolean = false;
    value: string;
    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };
    @ViewChild('iginput', { static: false }) el: ElementRef;

    valueChanged: Subject<string> = new Subject<string>();

    constructor(private ref: ChangeDetectorRef) { }

    ngOnInit() {
        this.valueChanged
            .pipe(debounceTime(this.debounce), distinctUntilChanged())
            .subscribe(obj => {
                this.performsearch();
            });
    }

    writeValue(obj: any): void {
        this.value = (obj != undefined && obj != null) ? obj : '';
        this.hasValue = this.value !== '';

        this.onModelChange(this.value);
        this.onModelTouched();
        this.ref.markForCheck();
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    isKeypress(): boolean {
        return (this.mode == 'Keypress');
    }
    isEnter(): boolean {
        return !this.isKeypress();
    }

    clearValue() {
        this.writeValue('');
        this.performsearch();
    }

    clicksearch() {
        this.performsearch();
    }

    performsearch() {
        this.onSearch.emit(this.value);
    }
    onInputKey(event: KeyboardEvent) {
        if (event.which == 13 && this.isEnter()) {
            event.preventDefault();
            event.stopImmediatePropagation();
            if (event.type == 'keydown') {
                this.performsearch();
            }
            return false;
        } else if (event.type == 'keyup' && this.isKeypress()) {
            this.valueChanged.next(this.value);
        }
    }

    focus() {
        this.el.nativeElement.focus();
    }

    @HostListener('focus')
    @HostListener('click')
    clickInside($event) {
        this.el.nativeElement.focus();
    }

    ngOnDestroy() {
        this.valueChanged.complete();
    }
}

@NgModule({
    declarations: [
        SearchFieldComponent
    ],
    exports: [
        SearchFieldComponent
    ]
    , imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        DirectivesModule,
        TooltipModule,
    ],
})
export class SearchFieldModule { }