export class StringHelpers {

    static isNullOrEmpty(value: string): boolean {
        return (value == null || value === '');
	}

	static formatAsPathString(value: string, replaceWithAngle: boolean = true): string {
		let replacement = (value !== '' && value !== null ? value : "");
		if (replaceWithAngle) {
			replacement = replacement.split(" > ").join("#pathSegmentDelimiter");
		}
		return replacement.split("<").join("&lt;").split(">").join("&gt;").split("#pathSegmentDelimiter").join(" <i class='fa fa-angle-right'></i> ");
	}
}