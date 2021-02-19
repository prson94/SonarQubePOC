import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { DirectivesModule } from '../../../../directives/directives.module';

import { Component, forwardRef, Input } from '@angular/core';
import { WeekDay, getLocaleFirstDayOfWeek } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
    selector: 'd3s-dayofweek-input',
    template: `
		        <div class="dayofweek-input">
                    <div *ngIf="label.length > 0" class="dayofweek-label">{{label}}</div>
                    <ul class="dayofweek-days">
                        <li *ngFor="let i of week"><p-checkbox
                            igCheckbox
                            [disabled]="disabled"
                            [(ngModel)]="days[i]"
                            (click)="recalc()"
                            [binary]="true"
                            [label]="displayDayName(i)">
                        </p-checkbox></li>
                    </ul>
                </div>
			  `,
    styles: [`
        .dayofweek-label {
            margin-right: 2em;
            float: left;
        }
        ul.dayofweek-days {
            column-width: 6em;
            list-style-type: none;
            padding: 0;
            margin: 0;
        }
    `],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DayOfWeekInputComponent),
            multi: true
        }
    ]
})

/**
 * Input component that allow selection of weekdays.
 * The value is a bitmask representing selected days.
 * DayOfWeek functions in C# and TypeScript/JavaScript has 0=Sunday, 1=Monday etc.
 * The bitmask value for any given day is calculated as 2^dayOfWeek so we get
 *  1 = Sunday
 *  2 = Monday
 *  4 = Tuesday
 *  8 = Wednesday
 * 16 = Thurday
 * 32 = Friday
 * 64 = Saturday
 * 
 * A value of 65 is Saturday and Sunday selected (64 + 1)
 * A value of 62 Monday through Friday selected (2 + 4 + 8 + 16 + 32)
 * All days selected is 127
*/
export class DayOfWeekInputComponent implements ControlValueAccessor {
    @Input() disabled: boolean = false;
    @Input() label: string = '';

    week: number[]; //Order of the days in the week according to localization
    days: boolean[]; //Boolean array of selected days. Tndexed by DayOfWeek 0=Sunday, 1=Monday, .. 6=Saturday

    constructor(
    ) {
        let offset = 0;
        try {
            offset = getLocaleFirstDayOfWeek(navigator.language);
        } catch (e) {
            offset = 0;
        }
        this.week = [].constructor(7).fill().map((x, i) => (offset + i) % 7);
        this.days = [].constructor(7).fill(false);
    }

    onChange = (value: number) => { };

    onTouched = () => { };

    get value(): number {
        return this.calculateValue();
    }

    writeValue(daysOfWeek: number): void {
        this.days = this.days.map((_, i) => (daysOfWeek & Math.pow(2, i)) != 0);
        this.onChange(this.value)
    }

    recalc() {
        if (!this.disabled) {
            this.writeValue(this.calculateValue());
        }
    }

    private calculateValue(): number {
        return this.days.reduce((total, checked, i) => {
            return total + (checked ? Math.pow(2, i) : 0);
        }, 0);
    }

    private displayDayName(day: number): string {
        return WeekDay[day];
    }

    registerOnChange(fn: (daysOfWeek: number) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }
}

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CheckboxModule,
        DirectivesModule,
    ],
    declarations: [
        DayOfWeekInputComponent
    ],
    exports: [
        DayOfWeekInputComponent
    ],
    providers: [
    ]
})
export class DayOfWeekInputModule { }