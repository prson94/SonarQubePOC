import { forwardRef, NgModule } from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { ControlValueAccessor } from '@angular/forms';
import { Subject, merge } from 'rxjs';
import { switchMap, takeUntil, startWith } from 'rxjs/operators';
import { DropdownBadgeOptionComponent } from './components/dropdown-badge-option.component';
import { DropdownBadgeOption } from './types/dropdown-bage-option.type';
import { CommonModule } from '@angular/common';
import { FilterDropdownBadgeOptionsPipe } from './pipes/filter-dropdown-bage-options.pipe';
import {
  AfterContentInit,
  ChangeDetectorRef,
  Component,
  ContentChildren,
  ElementRef, HostListener,
  Input,
  OnDestroy,
  QueryList
} from '@angular/core';

export const DROPDOWNBADGE_VALUE_ACCESSOR: any = {
  provide: NG_VALUE_ACCESSOR,
  useExisting: forwardRef(() => DropdownBadgeComponent),
  multi: true
};

@Component({
  selector: 'ig-dropdown-badge',
  templateUrl: './dropdown-badge.component.html',
  styleUrls: ['./dropdown-badge.component.less'],
  providers: [DROPDOWNBADGE_VALUE_ACCESSOR]
})
export class DropdownBadgeComponent<T = any> implements ControlValueAccessor, AfterContentInit, OnDestroy {
  private destroy$: Subject<boolean> = new Subject<boolean>();
  options: DropdownBadgeOption<T>[] = [];
  active: boolean = false;

  onModelChange: Function = () => { };
  onModelTouched: Function = () => { };

  @Input() placeholder: string = "Select an item...";
  @Input() editable: boolean = true;
  selected?: DropdownBadgeOption<T> | null;

  @ContentChildren(DropdownBadgeOptionComponent, { descendants: true })
  optionsComponents!: QueryList<DropdownBadgeOptionComponent>;

  @HostListener('document:click', ['$event'])
  onClick(event: MouseEvent) {
    if (!this.element.nativeElement.contains(event.target)) {
      this.setActive(false);
    }
  }

  constructor(private element: ElementRef, private cdr: ChangeDetectorRef) { }

  private setActive(state: boolean) {
    if (this.editable) {
      this.active = state;
    }
  }

  writeValue(value: T | null): void {
    this.selected = this.options.find(option => option.value === value);
    this.onModelChange(value);
    this.cdr.markForCheck();
  }

  registerOnChange(fn: any): void {
    this.onModelChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onModelTouched = fn;
  }

  onActiveStateSwitch(): void {
    this.setActive(!this.active);
  }

  setDisabledState(isDisabled: boolean): void {
    this.editable = !isDisabled;
  }

  onSelectOption(option: DropdownBadgeOption<T>): void {
    if (!option.disabled) {
      this.writeValue(option.value);
      this.setActive(false);
    }
  }

  ngAfterContentInit(): void {
    this.optionsComponents.changes
      .pipe(
        startWith(true),
        switchMap(() => {
          return merge(
            this.optionsComponents.changes,
            ...this.optionsComponents.map((option) => option.changes$)
          ).pipe(startWith(true));
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.options = this.optionsComponents.toArray().map((item) => {
          return {
            template: item.template,
            label: item.label,
            value: item.value,
            disabled: item.disabled,
            custom: item.custom
          };
        });
        this.selected = this.options.find(option => option.value === this.selected?.value);
        this.cdr.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next(true);
    this.destroy$.unsubscribe();
  }

}

@NgModule({
  declarations: [
      DropdownBadgeComponent,
      DropdownBadgeOptionComponent,
      FilterDropdownBadgeOptionsPipe
  ],
  exports: [
      DropdownBadgeComponent,
      DropdownBadgeOptionComponent
  ],
  imports: [
      CommonModule,
      FormsModule
  ]
})
export class DropdownBadgeModule { }