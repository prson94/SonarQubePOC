import { Component, NgModule, Input, ChangeDetectorRef, ChangeDetectionStrategy, ElementRef, OnChanges, SimpleChanges, Output, EventEmitter, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup, FormControl } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import * as _ from 'lodash';
import { getFormControlDomElement, getInvalidCount, getRequiredCount } from './form-feedback-utils';
import { Subject } from 'rxjs';
import { takeUntil, tap, startWith } from 'rxjs/operators';
import { PropertyGroupsService } from '../property-group/property-groups.service';
import { PropertyGroupInstanceIdAttributeName } from '../property-group/property-group.component';

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

    invalidCount: number = 0;
    requiredCount: number = 0;

    private requiredPos: number = 0;
    private invalidPos: number = 0;

    delayedRefresh = _.debounce(() => {
        this.requiredCount = getRequiredCount({ formGroup: this.igformGroup, formContainer: this.inputContainer });
        this.invalidCount = getInvalidCount({ formGroup: this.igformGroup, formContainer: this.inputContainer });
        this.ref.markForCheck();
    }, 200);

    constructor(private ref: ChangeDetectorRef, private propertyGroups: PropertyGroupsService) {
    }

    ngOnChanges(changes: SimpleChanges) {
        const needReinit = 'igformGroup' in changes || 'inputContainer' in changes;
        if (!needReinit) {
            return;
        }

        this.$destroy.next();
        this.delayedRefresh.cancel();

        if (this.igformGroup) {
            this.igformGroup.valueChanges
                .pipe(
                    startWith(null),
                    takeUntil(this.$destroy),
                    tap(() => this.delayedRefresh())
                )
                .subscribe();

            this.igformGroup.statusChanges
                .pipe(
                    takeUntil(this.$destroy),
                    tap(() => this.delayedRefresh())
                ).subscribe();
        }
    }

    focusInvalid(event) {
        event.stopPropagation();
        let found = false;
        const fcCount = this.getFormControlCount("errors");
        let idx = 0;
        for (const x of Object.keys(this.igformGroup.controls)) {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && !found) {
                let invFound = Object.keys(control.errors).filter((x) => x !== "required").length > 0;
                if (invFound) {
                    let elem = this.getFormControlDomElement(x);

                    if (elem) {
                        idx++;
                        if ((idx > this.invalidPos)) {
                            this.invalidPos++;
                            if (this.invalidPos >= fcCount) {
                                this.invalidPos = 0;
                            }

                            this.expandAndActivateInput(elem);

                            found = true;
                        }
                    }
                }
            }
        }
    }

    focusRequired(event) {
        event.stopPropagation();
        let found = false;
        let fcCount = this.getFormControlCount("required");
        let idx = 0;
        for (const x of Object.keys(this.igformGroup.controls)) {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && control.errors["required"] === true && !found) {
                let elem = <HTMLElement>this.getFormControlDomElement(x);
                if (elem) {
                    idx++;
                    if ((idx > this.requiredPos)) {
                        this.requiredPos++;
                        if (this.requiredPos >= fcCount) {
                            this.requiredPos = 0;
                        }

                        this.expandAndActivateInput(elem);

                        found = true;
                    }
                }
            }
        }
    }

    private expandAndActivateInput(inputElement: HTMLElement) {
        this.expandPropertyGroup(inputElement);
        this.activateInputOnly(inputElement);
    }

    private expandPropertyGroup(inputElement: HTMLElement) {
        const propertyGroupElement = inputElement.closest('ig-property-group');
        if (propertyGroupElement == null) {
            return;
        }

        const propertyGroupInstanceId = propertyGroupElement.attributes.getNamedItem(PropertyGroupInstanceIdAttributeName)?.value;
        if (propertyGroupInstanceId == null) {
            throw new Error(`Property group doesn't have attribute ${PropertyGroupInstanceIdAttributeName}`);
        }

        const propertyGroup = this.propertyGroups.getById(propertyGroupInstanceId);
        if (propertyGroup == null) {
            throw new Error(`Failed to find registered property group with id ${propertyGroupInstanceId}`);
        }

        propertyGroup.forceExpand();
    }

    private activateInputOnly(inputElement: HTMLElement) {
        if (inputElement.tagName === 'IG-DATE' || inputElement.tagName === 'IG-NUMBER-INPUT') {
            inputElement.querySelector('input').click();
        }
        else if (inputElement.tagName === 'P-DROPDOWN') {
            (inputElement.querySelectorAll('.p-dropdown-trigger')[0] as HTMLElement).click();
        }
        inputElement.focus();
    }

    getFormControlDomElement(controlName: string) {
        return getFormControlDomElement({ formContainer: this.inputContainer, controlName });
    }

    getFormControlCount(type: string): number {
        let count = 0;
        Object.keys(this.igformGroup.controls).forEach((x) => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control) {
                if (type === "required") {
                    if (control.errors && control.errors["required"] === true) {
                        let elem = <HTMLElement>this.getFormControlDomElement(x);
                        if (elem) {
                            count++;
                        }
                    }
                }
                if (type === "errors") {
                    if (control.errors) {
                        if (Object.keys(control.errors).filter((x) => x !== "required").length > 0) {
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