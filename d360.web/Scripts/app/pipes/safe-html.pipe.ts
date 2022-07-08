import { Pipe, PipeTransform, SecurityContext } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';


@Pipe({ name: 'safeHtml' })
export class SafeHtmlPipe implements PipeTransform {
    constructor(private sanitized:DomSanitizer) {}

	/* Angular sanitizer strips the `style` attribute from tags, but Quill relies on that attribute for coloring
	 * https://github.com/angular/angular/issues/45270
	 * To get around this, we will rename the style attribute to itemprop (a supported attribute), sanitize and swap back.
	 * Because we are modifying the output from sanitize() before returning it, we will have to mark it as SafeHtml again manually
	 */
	private stylePattern = /(<[^>]+)\sstyle="([^>]*>)/gi;
	private styleReplace = `$1 itemprop="$2`;
	private itempropPattern = /(<[^>]+)\sitemprop="([^>]*>)/gi;
	private itempropReplace = `$1 style="$2`;

	transform(value: string): any {
		if (!value) return "";

		var sanitizedValue = this.sanitized.sanitize(SecurityContext.HTML, value.replace(this.stylePattern, this.styleReplace));
		return this.sanitized.bypassSecurityTrustHtml(sanitizedValue.replace(this.itempropPattern, this.itempropReplace));
	}
}