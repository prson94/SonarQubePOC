import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    Input,
    NgModule,
    OnChanges,
    OnDestroy,
    SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { UntypedFormControl, UntypedFormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import * as _ from 'lodash';
import { getFormControlDomElement, getInvalidCount, getRequiredCount } from './form-feedback-utils';
import { Subject } from 'rxjs';
import { startWith, takeUntil, tap } from 'rxjs/operators';
import { PropertyGroupsService } from '../property-group/property-groups.service';
import { PropertyGroupInstanceIdAttributeName } from '../property-group/property-group.component';

@Component({
    selector: 'ig-form-feedback-badges',
    templateUrl: './form-feedback-badges.component.html',
    styleUrls: ['./form-feedback-badges.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormFeedbackBadgesComponent implements OnChanges, OnDestroy {
    @Input() igformGroup: UntypedFormGroup;
    @Input() inputContainer: ElementRef;

    $destroy = new Subject<void>();

    invalidCount: number = 0;
    requiredCount: number = 0;

    private requiredPos: number = 0;
    private invalidPos: number = 0;

	delayedRefresh = _.throttle(() => {
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
        for (const { control, element } of this.getOrderedControls()) {
            if (control.errors && !found) {
                const invFound = Object.keys(control.errors).filter((x) => x !== "required").length > 0;
                if (invFound) {
                    idx++;
                    if ((idx > this.invalidPos)) {
                        this.invalidPos++;
                        if (this.invalidPos >= fcCount) {
                            this.invalidPos = 0;
                        }

                        this.expandAndActivateInput(element);

                        found = true;
                    }
                }
            }
        }
    }

    focusRequired(event) {
        event.stopPropagation();
        let found = false;
        const fcCount = this.getFormControlCount("required");
        let idx = 0;
        for (const { control, element } of this.getOrderedControls()) {
            if (control.errors && control.errors["required"] === true && !found) {
                idx++;
                if ((idx > this.requiredPos)) {
                    this.requiredPos++;
                    if (this.requiredPos >= fcCount) {
                        this.requiredPos = 0;
                    }

                    this.expandAndActivateInput(element);

                    found = true;
                }
            }
        }
    }

    getOrderedControls() {
        return Object.keys(this.igformGroup.controls)
            .map((controlName) => {
                const control = this.igformGroup.get(controlName) as UntypedFormControl;
                const element = this.getFormControlDomElement(controlName);
                return { controlName, control, element };
            })
            .filter((x) => x.control != null)
            .filter((x) => x.element != null)
            .sort((a, b) => {
                const position =  a.element.compareDocumentPosition(b.element);
                if (position === Node.DOCUMENT_POSITION_PRECEDING) {
                    return 1;
                }

                if (position === Node.DOCUMENT_POSITION_FOLLOWING) {
                    return -1;
                }

                throw new Error(`Unknown code returned by Node.compareDocumentPosition: ${position}`);
            });
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
        else if (inputElement.tagName === 'IG-REGEXP-INPUT') {
            inputElement.querySelector('input').focus();
        }
        else if (inputElement.tagName === 'P-DROPDOWN') {
            inputElement.scrollIntoView();
            inputElement.querySelector('input').focus();
            
            setTimeout(() => {
                (inputElement.querySelector('.p-dropdown-label') as HTMLElement).click();
            }, 10);

            return;
        }
        else if (inputElement.tagName === 'P-EDITOR') {
            (inputElement.querySelector('.ql-editor') as HTMLElement).focus();
        }

        inputElement.focus();
    }

    getFormControlDomElement(controlName: string) {
        return getFormControlDomElement({ formContainer: this.inputContainer, controlName });
    }

    getFormControlCount(type: string): number {
        let count = 0;
        Object.keys(this.igformGroup.controls).forEach((x) => {
            const control = <UntypedFormControl>this.igformGroup.get(x);
            if (control) {
                if (type === "required") {
                    if (control.errors && control.errors["required"] === true) {
                        const elem = <HTMLElement>this.getFormControlDomElement(x);
                        if (elem) {
                            count++;
                        }
                    }
                }
                if (type === "errors") {
                    if (control.errors) {
                        if (Object.keys(control.errors).filter((x) => x !== "required").length > 0) {
                            const elem = <HTMLElement>this.getFormControlDomElement(x);
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