import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { SelectItem } from '../../models/form.model';


@Component({
    selector: 'gallery-tag-picker',
    templateUrl: './gallery.tag-picker.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
        .event-label {
            width: 110px;
            font-weight: bold;
            display: inline-block;
            }`]
})

export class GalleryTagPickerComponent implements OnInit {
    properties: Array<any>;
    events: Array<any>;
    sampleUsage: string = '<ig-tag-picker [(ngModel)]="model.Tags"></ig-tag-picker>';
    sampleUsage2: string = '<ig-tag-picker [formControlName]="field.FieldName"></ig-tag-picker>';


    value: SelectItem[] = [];
    copyOfValue: SelectItem[] = [];
    formValue;
    tagOnChange;
    tagOnSelect;
    tagOnUnselect;

    valueEvents: string = '';

    constructor(private ref: ChangeDetectorRef) { }

    ngOnInit(): void {
        
        this.properties = new Array();
        this.properties.push({ Name: "ngModel", Type: "string", Description: "Model representing the value of the tag picker control. Tag values are separated by '|'.", Default: "null" });
        this.properties.push({ Name: "tabindex", Type: "string", Description: "Index of the element in tabbing order.", Default: "null" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "readOnly", Type: "boolean", Description: "Used to set the control to read only state where the user cannot add or remove tags", Default: "false" });
        this.properties.push({ Name: "style", Type: "string", Description: "Inline style of the component.", Default: "" });
        this.properties.push({ Name: "assetUid", Type: "guid/string", Description: "Asset UID needed to resolve tag permissions", Default: "00000000-0000-0000-0000-000000000000" });

        this.events = new Array();
        this.events.push({ Name: "ngModelChange", Description: "Fired when the selection changes" });
        this.events.push({ Name: "onSelect", Description: "Callback to invoke when a tag suggestion is selected or new tag is added." });
        this.events.push({ Name: "onUnselect", Description: "Callback to invoke when a tag is removed from selection." });
    }

    basicValueChanged($event) {
        this.copyOfValue = JSON.parse(JSON.stringify(this.value));
    }
}
