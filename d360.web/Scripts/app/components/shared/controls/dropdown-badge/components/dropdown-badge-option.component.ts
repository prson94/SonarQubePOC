import { Component, Input, OnChanges, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { Subject } from 'rxjs';

@Component({
  selector: 'ig-dropdown-badge-option',
  template: `<ng-template> <ng-content></ng-content> </ng-template>`
})
export class DropdownBadgeOptionComponent implements OnChanges {
    /**
   * @description
   * Defines the template with content inside of
   * option component to be rendered in dropdown
   */
  @ViewChild(TemplateRef, {static: true})
  template!: TemplateRef<any>;

  /**
   * @description
   * Defines value, that will be emitted by
   * component, when option is selected
   */
  @Input() value: any | null = null;

  /**
   * @description
   * Defines the text representation of value,
   * that has been selected, shown in the badge
   */
  @Input() label: string = '';

  /**
   * @description
   * Defines whether it is possible to select the option
   */
  @Input() disabled: boolean = false;

  /**
   * @description
   * Defines whether content inside option 
   * or just label will be rendered
   */
  @Input() custom: boolean = false;
  changes$: Subject<SimpleChanges> = new Subject();

  constructor() { }

  ngOnChanges(changes: SimpleChanges): void {
    this.changes$.next(changes);
  }

}
