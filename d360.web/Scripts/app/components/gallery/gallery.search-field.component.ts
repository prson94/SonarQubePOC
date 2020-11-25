import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-search-field',
    templateUrl: './gallery.search-field.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        .searchoutput {
            display: inline-block;
            margin-top: 10px;
            padding: 10px;
            min-height: 1em;
            min-width: 200px;
            font-family: monospace
        }
        .flexccontainer {
            display: flex;
            min-width: 500px;
            padding: 10px;
        }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GallerySearchFieldComponent implements OnInit {
    properties: Array<any>;
    events: Array<any>;
    sampleUsage: string = '<ig-search-field></ig-search-field>';

    value:string = 'Test search';
    formValue;

    constructor(private ref: ChangeDetectorRef) { }

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "mode", Type: "string", Description: "Mode that determines when search takes place. Valid values: Keypress, Enter", Default: "Enter" });
        this.properties.push({ Name: "placeholder", Type: "string", Description: "Placeholder text, shown when there is no input", Default: "Search" });
        this.properties.push({ Name: "tabindex", Type: "number", Description: "Index of the element in tabbing order.", Default: "0" });
        this.properties.push({ Name: "debounce", Type: "number", Description: "Debounce time on keypress detection.", Default: "200" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "style", Type: "string", Description: "Inline style of the component.", Default: "" });

        this.events = new Array();
        this.events.push({ Name: "onSearch", Description: "Fired when a search is invoked according to the set mode" });

    }

    doneSearch(e, elem) {
        let el = document.getElementById(elem);
        let child = document.createElement('div');
        child.className = 'searchexpression';
        child.innerText = 'onSearch fired for "' + e+ '"';
        el.appendChild(child);
        setTimeout(function () {
            child.remove();
        }, 2000); 
        console.log(e, elem);
    }
}
