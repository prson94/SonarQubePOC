import { ElementRef } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup } from '@angular/forms';

export class FormFeedbackControl {
	element: HTMLElement;
	key: string;
}

//Static class to store cached html elements to avoid too many calls to querySelectorAll which has big performance impact
export abstract class FormFeedbackStorage {
	public static _cache: FormFeedbackControl[] = [];

	public static getCacheCount(): number{
		return this._cache.length;
	}

	public static add(key: string, element: HTMLElement) {
		if (!this._cache.some((x) => x.key === key)) {
			this._cache.push({ key, element });
		}
	}

	public static get(key: string): HTMLElement {
		return this._cache.find((x) => x.key === key)?.element;
	}

	public static clear() {
		this._cache = [];
	}
}

export function getFormControlDomElement({ formContainer, controlName }: { formContainer: ElementRef; controlName: string; }) {
	if (formContainer == null) {
		return null;
	}
	const fromCache = FormFeedbackStorage.get(controlName);
	if (fromCache) {
		return fromCache;
	}

	const controls = formContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "], [id=" + controlName + "]") as HTMLElement[];
	if (controls && controls.length > 0) {
		FormFeedbackStorage.add(controlName, controls[0]);
		return controls[0];
	}
	else {
		return null;
	}
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