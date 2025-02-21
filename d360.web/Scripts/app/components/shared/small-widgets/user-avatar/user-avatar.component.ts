import { Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange } from "@angular/core";

declare const __webpack_public_path__: string;

@Component({
	selector: "d3s-user-avatar",
	templateUrl: "./user-avatar.component.html",
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class UserAvatarComponent implements OnChanges {

	@Input() resourceUid: string;
	@Input() resourceName: string;
	@Input() showPicture: boolean = true;
	@Input() size: number;
	@Input() style: string;
	@Input() class: string;

	private availableSizes = [25, 35, 70, 200];
	private defaultSize: number = 70;
	private baseUrl: string = null;
	resourceInitials: string = "";

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
		this.resourceInitials = this.getUserInitials();
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

	getNameHash(str) {
		let hash = 5381;
		for (let i = 0; i < str.length; i++) {
			hash = ((hash << 5) + hash) + str.charCodeAt(i); /* hash * 33 + c */
		}
		return hash;
	}
	hashStringToColor(str, opacity) {
		const hash = this.getNameHash(str);
		const r = (hash & 0xFF0000) >> 16;
		const g = (hash & 0x00FF00) >> 8;
		const b = hash & 0x0000FF;
		return `rgba(${r}, ${g}, ${b}, ${opacity})`;
		//return "#" + ("0" + r.toString(16)).substr(-2) + ("0" + g.toString(16)).substr(-2) + ("0" + b.toString(16)).substr(-2);
	}

	getAvatarStyle() {
		if (this.resourceName) {
			const bcolor = this.hashStringToColor(this.resourceName, 0.35);
			const fcolor = this.hashStringToColor(this.resourceName, 1);
			let style = `background-color: ${bcolor}`;
			style += `; color: ${fcolor}`;
			style += `; border-radius: ${this.size / 2}px`;
			style += `; line-height: ${this.size}px`;
			style += `; vertical-align: middle`;
			style += `; text-align: center`;
			style += `; font-size: ${this.size * .4}px`;
			style += `; height: ${this.size}px`;
			style += `; width: ${this.size}px`;
			return style;
		}
		else {
			return "";
		}
	}

	private getUserInitials() {
		if (this.resourceName) {
			const nameSegments = this.resourceName.split(" ");
			while (nameSegments.length > 2) {
				nameSegments.splice(1, 1);
			}
			return nameSegments.map((n) => n[0]).join("");
		}
		else {
			return "";
		}
	}
}
