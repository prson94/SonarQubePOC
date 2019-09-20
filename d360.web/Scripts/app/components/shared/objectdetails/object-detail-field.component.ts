import { Input, Component, OnInit, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { DetailRow, DetailField, DetailModel, DetailFieldType, DetailSubField } from '../../../models/object-detail.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';
import { TagService } from '../../../services/tag.service';
import { TagType, TagDetail, TagItem } from '../../../models/tag.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'object-detail-field',
    templateUrl:'./object-detail-field.component.html'
})

export class ObjectDetailFieldComponent {
    @Input() field: DetailField;
    @Input() assetUID: string;
    @Input() assetID: number;
    DetailFieldType = DetailFieldType;
    

    constructor(private router: Router) {}
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
        } catch{
            return "Error";
        }
    }
    private getJSONString(obj: any): string {
        return JSON.stringify(obj)
    }

    private get isArrayValue(): boolean {
        return this.field != null
            && this.field.Values
            && this.field.Values.length > 0;
    }

    private get isEmail(): boolean {
        return this.field != null
            && this.field.Name != null
            && this.field.Name.toLowerCase() == 'email'
            && this.fieldDataType == 'text';
    }

    private get isName(): boolean {
        return this.field != null
            && this.field.Name != null
            && ['name', 'implementation name'].indexOf(this.field.Name.toLowerCase()) > -1;
    }

    private get fieldDataType(): string {
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
}

