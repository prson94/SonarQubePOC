import { Input, Component, ChangeDetectionStrategy, ViewEncapsulation } from '@angular/core';

@Component({
    selector: 'ig-field-value',
    template: `<div class="row" *ngIf="value">
                <div class="row-header">
                        <label class="ig-label">
                            <span class="ng-star-inserted">{{field}}</span>
                        </label>
                    </div>
                    <div class="ig-value" [innerHTML]="value"></div>
                </div>`,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`p {padding:0, margin:0}`],
    encapsulation: ViewEncapsulation.None
})


export class FieldValueComponent {
    @Input() field: string;
    @Input() value: string;

    constructor() { }

}
