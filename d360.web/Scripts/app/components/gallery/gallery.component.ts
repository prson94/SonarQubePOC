import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";


@Component({
    selector: "gallery",
    templateUrl: "./gallery.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
           .gallery-label {
                font-size: 14px;
                color: black;
                padding-left: 10px;
                margin: 10px 0px;
                display: block;
            }

            .collection-item {
                cursor:pointer;
             }
    `]
})

export class GalleryComponent implements OnInit {
    activeControl: string = "regexp-input";
    controls = [
        { label: "Switch Input", key: "switch", type: "Form" },
        { label: "Button Directive", key: "button", type: "Form" },
        { label: "Icon Picker", key: "icon-picker", type: "Form" },
        { label: "Tag Picker", key: "tag-picker", type: "Govern Components" },
        { label: "Text Field", key: "input", type: "Form" },
        { label: "Auto Complete", key: "auto-complete", type: "Form" },
        { label: "Tooltip", key: "tooltip", type: "Overlay" },
        { label: "Auto Focus Directive", key: "auto-focus", type: "Misc" },
        { label: "Color Picker", key: "color-picker", type: "Form" },
        { label: "Color Variables", key: "color-variables", type: "Govern Components" },
        { label: "Text Area", key: "textarea", type: "Form" },
        { label: "Date Picker", key: "date-picker", type: "Form" },
        { label: "Loading Component", key: "loading", type: "Misc" },
        { label: "Accordion", key: "accordion", type: "Data" },
        { label: "Page Info", key: "paging-info", type: "Data" },
        { label: "Selection Info", key: "selection-info", type: "Data" },
        { label: "Number Field", key: "number-field", type: "Form" },
        { label: "Message Box", key: "message-box", type: "Data" },
        { label: "Badge", key: "badge", type: "Misc" },
        { label: "Checkbox", key: "checkbox", type: "Form" },
        { label: "Popup Menu", key: "popup-menu", type: "Overlay" },
        { label: "Select", key: "select", type: "Form" },
        { label: "Property Group", key: "propery-group", type: "Form" },
        { label: "Radio Button", key: "radio-button", type: "Form" },
        { label: "Field Condition Grid", key: "field-condition-grid", type: "Govern Components" },
        { label: "Search Field", key: "search-field", type: "Govern Components"},
        { label: "Multi Input Field", key: "multi-input-field", type: "Govern Components"},
        { label: "Input Group", key: "input-group", type: "Form"},
        { label: "Modal", key: "modal", type: "Overlay" },
        { label: "Modal Drawer", key: "modal-drawer", type: "Overlay" },
        { label: "Code Area", key: "codearea", type: "Form" },
        { label: "Localization", key: "locale", type: "Misc" },
        { label: "File Picker", key: "image-picker", type: "Form" },
        { label: "Regexp Input", key: "regexp-input", type: "Form" }
    ];

    categories: any[] = [];

    ngOnInit(): void {

        this.controls.forEach(x => {
            if (!this.categories.some(c => c.type == x.type)) {
                this.categories.push({
                    type: x.type,
                    controls: this.controls.sort((a, b) => { return a.label > b.label ? 1 : -1 }).filter(ct => ct.type == x.type)
                });
            }
        });
    }
}
