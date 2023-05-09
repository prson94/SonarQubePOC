import { Component, Input, OnInit } from "@angular/core";
import { isObject, isArray, isDate, isString, isNumber, isBoolean, isUndefined } from "lodash-es";

@Component({
	selector: "ig-json-viewer-node",
	templateUrl: "./json-viewer-node.component.html",
	styleUrls: ["./json-viewer-node.component.less"]
})
export class JsonViewerNodeComponent implements OnInit {
	private _data: object;
	@Input()
	set data(data: object) {
		this._data = data;
		this.isInit && this.init();
	}
	get data(): object { return this._data; }
	@Input() key: string;
	@Input() level: number = 0;

	isOpen: boolean = false;
	childrenKeys: string[];
	hasChildren: boolean = false;
	dataType: string;
	value: unknown;
	valueType: string;
	isObject: boolean = false;
	isArray: boolean = false;
	isDate: boolean = false;
	isInit: boolean = false;

	ngOnInit() {
		this.init();
		this.isInit = true;
	}

	init() {
		if (isObject(this.data)) {
			this.isObject = true;
			this.dataType = "Object";

			if (isArray(this.data)) {
				this.isArray = true;
				this.dataType = "Array";
				this.value = this.data.length;
			} else if (isDate(this.data)) {
				this.isDate = true;
				this.dataType = "Date";
				this.value = this.data.toString();
			}

			this.childrenKeys = Object.keys(this.data);
			if (this.childrenKeys && this.childrenKeys.length > 0) {
				this.hasChildren = true;
			}
		} else {
			this.value = this.data;
			if (isString(this.data)) {
				this.dataType = "String";
			} else if (isNumber(this.data)) {
				this.dataType = "Number";
			} else if (isBoolean(this.data)) {
				this.dataType = "Boolean";
			} else if (isUndefined(this.data)) {
				this.dataType = "Undefined";
				this.value = "undefined";
			} else if (null === this.data) {
				this.dataType = "Null";
				this.value = "null";
			}
		}
	}

	typeClass(): string {
		return this.dataType.toLowerCase();
	}

	toggle() {
		if (!(this.childrenKeys && this.childrenKeys.length)) {
			return;
		}
		this.isOpen = !this.isOpen;
	}

}
