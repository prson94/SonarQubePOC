import { Injectable } from '@angular/core';
import * as DOMPurify from "dompurify";

@Injectable({
    providedIn: 'root'
})

export class DOMPurifyService {
    constructor() {
		DOMPurify.addHook("afterSanitizeAttributes", (node) => {
			const { attributes } = node;
			if (!attributes || attributes.length < 2)
				return;
			// No need to switch the last one.
			for (let l = attributes.length - 2; l >= 0; l--) {
				const attr = attributes[l];
				const { name, value } = attr;
				node.removeAttribute(name);
				node.setAttribute(name, value);
			}
		});
    }

	sanitize(source: string | Node): string {
		let orgval = source;
		let sanitizevalue = DOMPurify.sanitize(source);
		if (sanitizevalue.length === String(orgval).length) {
			sanitizevalue = String(orgval);
		}
		return sanitizevalue;
	}
}
