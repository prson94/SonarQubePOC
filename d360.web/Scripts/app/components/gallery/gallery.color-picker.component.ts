import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { AssetService } from '../../services/asset.service';


@Component({
    selector: 'gallery-color-picker',
    templateUrl: './gallery.color-picker.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ],
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryColorPickerComponent implements OnInit {
    protected properties: Array<any>;
    protected events: Array<any>;
    protected sampleUsage: string = '<ig-color-picker (selectedColorChange)="onColorSelect($event)"></ig-color-picker>';

    private selectedColorBasic = "no color selected";
    private selectedColorCustom = "no color selected";
    private chosenColor: string = "Sky";
    private selectedColorBasic2 = this.chosenColor;

    private invalidOptions = ["Sky", "Blush"];

    private formVal: string;

    private customColors: SelectItem[] = [
        { label: "custom label 1", value: "unique value 1", title: "#169b62" },
        { label: "custom label 2", value: "unique value 2", title: "#ffffff" },
        { label: "custom label 3", value: "unique value 3", title: "#ff883e" }
    ];

    private defaultColors: SelectItem[] = [];
    

    constructor(private assetService: AssetService) {}

    ngOnInit(): void {
        this.assetService.getAllColors().subscribe(x => { this.defaultColors = x; });

        this.properties = new Array();
        this.properties.push({ Name: "colors", Type: "array", Description: "An array of select list items that have label, title and value properties. title is used for the color value, label for display and value for the desired value from the select list.", Default: "" });
        this.properties.push({ Name: "placeholder", Type: "string", Description: "shows in the dropdown unitl an item is selected .", Default: "Optional" });
        this.properties.push({ Name: "selectedColor", Type: "string", Description: "The value of the desired item to be selected in the list.", Default: "" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "style", Type: "string", Description: "Inline style of the component.", Default: "" });
        this.properties.push({ Name: "invalidOptions", Type: "String Array", Description: "Allows the user to define options that can be disabled. If the selected color passed into the component matches any invalid option the value is cleared.", Default: "" });
        this.properties.push({ Name: "styleClass", Type: "string", Description: "Style class of the component.", Default: "" });
        this.properties.push({ Name: "tabindex", Type: "number", Description: "Index of the element in tabbing order.", Default: "0" });

        this.events = new Array();
        this.events.push({ Name: "selectedColorChange", Type: "string", Description: "Function that outputs the selected value from the lsit of colors.", Default: "false" });
    }

    onColorSelect(color, option) {
        if(option == 1)
            this.selectedColorBasic = color;
        if (option == 2)
            this.selectedColorCustom = color
        if (option == 3)
            this.selectedColorBasic2 = color
    }


}
