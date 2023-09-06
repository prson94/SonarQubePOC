import { Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange } from "@angular/core";

declare const __webpack_public_path__: string;

@Component({
	selector: "d3s-user-avatar",
	templateUrl: "./user-avatar.component.html",
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class UserAvatarComponent implements OnChanges {

	@Input() resourceUid: string;
	@Input() size: number;
	@Input() style: string;
	@Input() class: string;

	private availableSizes = [25, 35, 70, 200];
	private defaultSize: number = 70;
	private baseUrl: string = null;

	constructor(
		private ref: ChangeDetectorRef,
	) {
		const baseUrl = new URL(__webpack_public_path__);
		const port = baseUrl.port.length === 0 ? "" : ":" + baseUrl.port;
		this.baseUrl = `${baseUrl.protocol}//${baseUrl.host}${port}`;
	}

	get src() {
		return this.getAbsoluteAssetUrl(`/api/v2/membership/users/${this.resourceUid}/image?size=${this.size}`);
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (changes["size"]) {
			this.defaultSize = this.closestNumber(changes["size"].currentValue, this.availableSizes);
		}
	}

	getAbsoluteAssetUrl(url: string): string {
		if (!url.startsWith("/")) {
			url = "/" + url;
		}
		return `${this.baseUrl}${url}`;
	}

	onImgError(event) {
		const defaultImage = this.getAbsoluteAssetUrl(`/Content/images/user${this.defaultSize}.png`);
		if (event.target.src !== defaultImage) {
			event.target.src = defaultImage;
		}
	}

	closestNumber(num: number, numbers: number[]): number {
		const arr = numbers.sort((a, b) => a - b);
		let mid;
		let lo = 0;
		let hi = arr.length - 1;

		while (hi - lo > 1) {
			mid = Math.floor((lo + hi) / 2);
			if (arr[mid] < num) {
				lo = mid;
			} else {
				hi = mid;
			}
		}
		if (num - arr[lo] <= arr[hi] - num) {
			return arr[lo];
		}
		return arr[hi];
	}
}
