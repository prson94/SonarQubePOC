import { Input, Component, OnInit, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { DetailRow, DetailField, DetailModel, DetailFieldType, DetailSubField } from '../../../models/object-detail.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';

@Component({
    selector: 'object-detail-field',
    templateUrl: './object-detail-field.component.html'
})

export class ObjectDetailFieldComponent {
    @Input() field: DetailField;
    DetailFieldType = DetailFieldType;

    constructor(private router: Router) { }
    ngOnInit() {
        if ((this.field.DataType == 'date' || this.field.DataType == 'datetime') && isNaN(Date.parse(this.field.Value)))
            this.field.Value = null;

        if (this.field.Value == null && this.field.Type == DetailFieldType.Lookup)
            this.field.Value = "LookupField";

        console.log(this.field);

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
        } catch{
            return "Error";
        }
    }
}

