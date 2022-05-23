import { Pipe, PipeTransform } from '@angular/core';
import { DropdownBadgeOption } from '../types/dropdown-bage-option.type';
/*
 * Transforms array of options by removing
 * selected option from it
 * Usage:
 *   options[] | filterDropdownBadgeOptions:selected
*/
@Pipe({name: 'filterDropdownBadgeOptions'})
export class FilterDropdownBadgeOptionsPipe<T> implements PipeTransform {
  transform(value: DropdownBadgeOption<T>[], selected: DropdownBadgeOption<T>): DropdownBadgeOption<T>[] {
    return value.filter(option => option !== selected);
  }
}