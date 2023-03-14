import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { escape } from "lodash-es";

@Component({
    selector: 'd3s-highlight-search-text',
	template: `<span  [innerHTML]="html | safeHtml"></span>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HighlightSearchTextComponent {
    @Input() text: string;
    @Input() highlight: string;

	public get html() {
		if (!this.highlight) {
			return escape(this.text);
		}

		const regexSafeHighlight = this.highlight.replace(/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g, "\\$&");
		let placeolder = this.text.replace(new RegExp(regexSafeHighlight, "gi"), (match) => {
            return `__HILITESTART__${match}__HILITEEND__`;
		});
		return escape(placeolder)
			.replace(new RegExp("__HILITESTART__", "gi"), "<span style=\"background: var(--navbarBackColorSelectedHover);\">")
			.replace(new RegExp("__HILITEEND__", "gi"), "</span>");
    }
}
