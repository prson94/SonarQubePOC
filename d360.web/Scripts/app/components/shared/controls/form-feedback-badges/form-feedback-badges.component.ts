import { Component, NgModule, Input, ChangeDetectorRef, ChangeDetectionStrategy, ElementRef, OnChanges, SimpleChanges, Output, EventEmitter, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup, FormControl } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import * as _ from 'lodash';
import { getFormControlDomElement, getInvalidCount, getRequiredCount } from './form-feedback-utils';
import { Subject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';

@Component({
    selector: 'ig-form-feedback-badges',
    templateUrl: './form-feedback-badges.component.html',
    styleUrls: ['./form-feedback-badges.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormFeedbackBadgesComponent implements OnChanges, OnDestroy {
    @Input() igformGroup: FormGroup;
    @Input() inputContainer: ElementRef;

    $destroy = new Subject();

    // TODO: check if we need this
    @Output() isValid = new EventEmitter();
    invalidCount: number = 0;
    requiredCount: number = 0;
    @Input() expanded: boolean = true;
    @Output() expandedChange = new EventEmitter();

    private requiredPos: number = 0;
    private invalidPos: number = 0;

    delayedRefresh = _.debounce(() => {
        this.requiredCount = getRequiredCount({ formGroup: this.igformGroup, formContainer: this.inputContainer });
        this.invalidCount = getInvalidCount({ formGroup: this.igformGroup, formContainer: this.inputContainer });
        this.isValid.emit(this.requiredCount === 0 && this.invalidCount === 0);
        this.ref.markForCheck();
    }, 200);

    constructor(private ref: ChangeDetectorRef) {
    }

    ngOnChanges(changes: SimpleChanges) {
        const needReinit = 'igformGroup' in changes;
        if (!needReinit) {
            return;
        }

        this.$destroy.next();
        this.delayedRefresh.cancel();

        if (this.igformGroup) {
            this.igformGroup.valueChanges
                .pipe(
                    takeUntil(this.$destroy),
                    tap(() => this.delayedRefresh())
                )
                .subscribe();
        }
    }

    focusInvalid(event) {
        event.stopPropagation();
        let found = false;
        if (!this.expanded) {
            this.expandedChange.next(true);
        }

        let fcCount = this.getFormControlCount("errors");
        let idx = 0;
        Object.keys(this.igformGroup.controls).forEach(x => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && !found) {
                let invFound = Object.keys(control.errors).filter(x => x != "required").length > 0;
                if (invFound) {
                    let elem = this.getFormControlDomElement(x);

                    if (elem) {
                        idx++;
                        if ((idx > this.invalidPos)) {
                            this.invalidPos++;
                            if (this.invalidPos >= fcCount) {
                                this.invalidPos = 0;
                            }
                            if (elem.tagName === 'IG-DATE' || elem.tagName === 'IG-NUMBER-INPUT') {
                                elem.querySelector('input').click();
                            }
                            elem.focus();
                            found = true;
                        }
                    }
                }
            }
        });
    }

    focusRequired(event) {
        event.stopPropagation();
        let found = false;
        if (!this.expanded) {
            this.expandedChange.next(true);
        }

        let fcCount = this.getFormControlCount("required");
        let idx = 0;
        Object.keys(this.igformGroup.controls).forEach((x) => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && control.errors["required"] == true && !found) {
                let elem = <HTMLElement>this.getFormControlDomElement(x);
                if (elem) {
                    idx++;
                    if ((idx > this.requiredPos)) {
                        this.requiredPos++;
                        if (this.requiredPos >= fcCount) {
                            this.requiredPos = 0;
                        }
                        if (elem.tagName === 'IG-DATE' || elem.tagName === 'IG-NUMBER-INPUT') {
                            elem.querySelector('input').click();
                        }
                        else if (elem.tagName === 'P-DROPDOWN') {
                            (elem.querySelectorAll('.p-dropdown-trigger')[0] as HTMLElement).click();
                        }
                        elem.focus();
                        found = true;
                    }
                }
            }
        });
    }

    getFormControlDomElement(controlName: string) {
        return getFormControlDomElement({ formContainer: this.inputContainer, controlName });
    }

    getFormControlCount(type: string): number {
        let count = 0;
        Object.keys(this.igformGroup.controls).forEach(x => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control) {
                if (type == "required") {
                    if (control.errors && control.errors["required"] == true) {
                        let elem = <HTMLElement>this.getFormControlDomElement(x);
                        if (elem) {
                            count++;
                        }
                    }
                }
                if (type == "errors") {
                    if (control.errors) {
                        if (Object.keys(control.errors).filter(x => x != "required").length > 0) {
                            let elem = <HTMLElement>this.getFormControlDomElement(x);
                            if (elem) {
                                count++;
                            }
                        }
                    }
                }

            }
        });
        return count;
    }


    onInputKeyUp(event) {
        event.preventDefault();
        event.stopPropagation();
        switch (event.which) {
            case 32:
                event.target.click();
                return false;
        }
    }

    ngOnDestroy(): void {
        this.$destroy.next();
    }
}

@NgModule({
    declarations: [
        FormFeedbackBadgesComponent
    ],
    exports: [
        FormFeedbackBadgesComponent
    ], imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        TooltipModule,
    ]
})
export class FormFeedbackBadgesModule { }