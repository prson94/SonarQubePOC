import { Input, Component } from '@angular/core';
import { DetailField, DetailFieldType } from '../../../models/object-detail.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';

@Component({
    selector: 'object-detail-field',
    templateUrl: './object-detail-field.component.html'
})

export class ObjectDetailFieldComponent {
    @Input() field: DetailField;
    @Input() assetUID: string;
    DetailFieldType = DetailFieldType;


    constructor(private router: Router) { }
    ngOnInit() {
        if ((this.field.DataType == 'date' || this.field.DataType == 'datetime') && isNaN(Date.parse(this.field.Value)))
            this.field.Value = null;


    }

    private formatAsNumber(fieldValue): string {
        return fieldValue !== '' && fieldValue != null ? Number(fieldValue).toLocaleString() : "";
    }

    navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }
    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch (err) {
            return "Error";
        }
    }

    get isArrayValue(): boolean {
        return this.field != null
            && this.field.Values
            && this.field.Values.length > 0;
    }

    get isEmail(): boolean {
        return this.field != null
            && this.field.Name != null
            && this.field.Name.toLowerCase() == 'email'
            && this.fieldDataType == 'text';
    }

    get isName(): boolean {
        return this.field != null
            && this.field.Name != null
            && ['name', 'implementation name'].indexOf(this.field.Name.toLowerCase()) > -1;
    }

    get isAlreadyUTC(): boolean {
        if (this.field && this.field.Value && this.field.Value.endsWith('Z'))
            return true;
        return false;
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

    get linkData(): any {
        if (!this.field || !this.field.Value) {
            return null;
        }

        var value = this.field.Value.split('|');

        return { title: value[0], url: value[1] };
    }
}

