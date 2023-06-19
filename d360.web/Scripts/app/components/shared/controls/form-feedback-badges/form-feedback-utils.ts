import { ElementRef } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup } from '@angular/forms';

export function getFormControlDomElement({ formContainer, controlName }: { formContainer: ElementRef; controlName: string; }) {
	if (formContainer == null) {
		return null;
	}

	const controls = formContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "], [id=" + controlName + "]") as HTMLElement[];

	return controls.length > 0 ? controls[0] : null;
}

export function getRequiredCount({ formGroup, formContainer }: { formGroup: UntypedFormGroup; formContainer: ElementRef; }): number {
	if (formGroup == null) {
		return 0;
	}

	let reqCount = 0;
	Object.keys(formGroup.controls).forEach((x) => {
		const control = <UntypedFormControl>formGroup.get(x);
		const elem = getFormControlDomElement({ formContainer, controlName: x });

		if (elem && control && control.errors && control.errors["required"] === true) {
			reqCount++;
		}
	});

	return reqCount;
}

export function getInvalidCount({ formGroup, formContainer }: { formGroup: UntypedFormGroup; formContainer: ElementRef; }): number {
	if (formGroup == null) {
		return 0;
	}

	let invCount = 0;
	Object.keys(formGroup.controls).forEach((x) => {
		const control = <UntypedFormControl>formGroup.get(x);
		const elem = getFormControlDomElement({ formContainer, controlName: x });
		if (elem && control && control.errors) {
			invCount += Object.keys(control.errors).filter((x) => x !== "required").length > 0 ? 1 : 0;
		}
	});

	return invCount;
}

export function isFormContainerValid({ formGroup, formContainer }: { formGroup: UntypedFormGroup; formContainer: ElementRef; }): boolean {
	return getRequiredCount({ formGroup, formContainer }) === 0
		&& getInvalidCount({ formGroup, formContainer }) === 0;
}