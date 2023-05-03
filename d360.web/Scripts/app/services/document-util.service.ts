import { Injectable } from "@angular/core";

@Injectable({
	providedIn: "root",
})
export class DocumentUtilService {
	/**
	 * Utility method to append an element to the body of the current document.  If the supplied element is part of
	 * the shadow dom, it will instead be appended to the shadow root element.
	 */
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	appendToBody(element: any): void {
		const root = element.getRootNode();
		if (root instanceof ShadowRoot) {
			root.append(element);
		} else {
			document.body.append(element);
		}
	}
}