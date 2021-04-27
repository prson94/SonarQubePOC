import { Input, Component, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { DetailField, DetailFieldType } from '../../../models/object-detail.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';

@Component({
    selector: 'igx-asset-detail-field',
    templateUrl: './asset-detail-field.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetDetailFieldComponent {
    @Input() field: DetailField;

    readonly emptyValue: string = "---";
    readonly dateFormat: string = "d MMM yyyy";
    readonly dateTimeFormat: string = "d MMM yyyy h:mm:ss";


    constructor(private router: Router,
        private ref: ChangeDetectorRef)
    { }

    ngOnInit() {
        if ((this.field.DataType == 'date' || this.field.DataType == 'datetime') && isNaN(Date.parse(this.field.Value)))
            this.field.Value = null;
    }

    get shouldShowEmptyValue(): boolean {
        if (this.field == null) {
            return false;
        }

        return ((this.field.Value == null || this.field.Value === "") && this.field.ShowIfEmpty === true);

    }

    get fieldDataType(): string {
        if (this.field == null || this.field.DataType == null)
            return null;
        switch (this.field.DataType.toLowerCase()) {
            case 'text':
            case 'string':
                return 'text';
            case 'number':
            case 'decimal':
                return 'number';
            case 'bool':
            case 'boolean':
                return 'bool';
            default:
                return this.field.DataType.toLowerCase();
        }
    }

    get isUtcDate(): boolean {
        if (this.field
            && this.field.DataType
            && (this.field.DataType === "date" || this.field.DataType === "datetime")
            && this.field.Value
            && this.field.Value.endsWith('Z'))
            return true;
        return false;
    }

    //#region Formatted field values

    get formattedNumber(): string {
        return this.field.Value !== "" && this.field.Value != null ? Number(this.field.Value).toLocaleString() : "";
    }

    get linkUrl(): string {
        if (this.field == null || this.field.Value.indexOf("|") === -1)
            return null;
        let index = this.field.Value.indexOf("|");

        return this.field.Value.substring(index + 1);
    }

    get linkName(): string {
        if (this.field == null || this.field.Value.indexOf("|") === -1)
            return null;
        let index = this.field.Value.indexOf("|");
        if (index === 0) {
            return this.linkUrl;
        }
        else {
            return this.field.Value.split("|")[0];
        }
    }

    get json(): any {
        try {
            return JSON.parse(this.field.Value);
        } catch (err) {
            return "Error";
        }
    }



    //#endregion
}

