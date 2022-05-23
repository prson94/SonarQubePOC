import { ElementRef } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import * as _ from 'lodash';

export function getFormControlDomElement({ formContainer, controlName }: { formContainer: ElementRef; controlName: string; }) {
    if (formContainer == null) {
        return null;
    }

    return formContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "], [id=" + controlName + "]").length > 0
        ? formContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "], [id=" + controlName + "]")[0]
        : null;
}

export function getRequiredCount({ formGroup, formContainer }: { formGroup: FormGroup; formContainer: ElementRef; }): number {
    if (formGroup == null) {
        return 0;
    }

    let reqCount = 0;
    Object.keys(formGroup.controls).forEach((x) => {
        let control = <FormControl>formGroup.get(x);
        let elem = getFormControlDomElement({ formContainer, controlName: x });

        if (elem && control && control.errors && control.errors["required"] === true) {
            reqCount++;
        }
    });

    return reqCount;
}

export function getInvalidCount({ formGroup, formContainer }: { formGroup: FormGroup; formContainer: ElementRef; }): number {
    if (formGroup == null) {
        return 0;
    }

    let invCount = 0;
    Object.keys(formGroup.controls).forEach((x) => {
        let control = <FormControl>formGroup.get(x);
        let elem = getFormControlDomElement({ formContainer, controlName: x });
        if (elem && control && control.errors) {
            invCount += Object.keys(control.errors).filter(x => x !== "required").length > 0 ? 1 : 0;
        }
    });

    return invCount;
}

export function isFormContainerValid({ formGroup, formContainer }: { formGroup: FormGroup; formContainer: ElementRef; }): boolean {
    return getRequiredCount({ formGroup, formContainer }) === 0
        && getInvalidCount({ formGroup, formContainer }) === 0;
}