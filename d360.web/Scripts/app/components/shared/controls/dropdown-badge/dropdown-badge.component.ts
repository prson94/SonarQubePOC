import { ContentChild, forwardRef, NgModule, TemplateRef, ViewEncapsulation } from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { ControlValueAccessor } from '@angular/forms';
import { DropdownBadgeOption } from './types/dropdown-bage-option.type';
import { CommonModule } from '@angular/common';
import { FilterDropdownBadgeOptionsPipe } from './pipes/filter-dropdown-bage-options.pipe';
import {
  ChangeDetectorRef,
  Component,
  ElementRef, HostListener,
  Input
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
  encapsulation: ViewEncapsulation.None,
  providers: [DROPDOWNBADGE_VALUE_ACCESSOR]
})
export class DropdownBadgeComponent<T = any> implements ControlValueAccessor {
  active: boolean = false;
  selected!: DropdownBadgeOption<T>;
  disabled: boolean = false;

  onModelChange: Function = () => { };
  onModelTouched: Function = () => { };

  @Input() options: DropdownBadgeOption<T>[] = [];

  @ContentChild(TemplateRef, { static: false })
  optionTemplate: TemplateRef<any>;

  @HostListener('document:click', ['$event'])
  onClick(event: MouseEvent) {
    if (!this.element.nativeElement.contains(event.target)) {
      this.setActive(false);
    }
  }

  constructor(private element: ElementRef, private cdr: ChangeDetectorRef) { }

  private setActive(state: boolean) {
    if (!this.disabled) {
      this.active = state;
    }
  }

  writeValue(value: T): void {
    this.selected = this.options.find((option) => option.value === value);
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
    this.disabled = isDisabled;
  }

  onSelectOption(option: DropdownBadgeOption<T>): void {
    if (!option.disabled) {
      this.writeValue(option.value);
      this.setActive(false);
    }
  }

}

@NgModule({
  declarations: [
      DropdownBadgeComponent,
      FilterDropdownBadgeOptionsPipe
  ],
  exports: [
      DropdownBadgeComponent
  ],
  imports: [
      CommonModule,
      FormsModule
  ]
})
export class DropdownBadgeModule { }