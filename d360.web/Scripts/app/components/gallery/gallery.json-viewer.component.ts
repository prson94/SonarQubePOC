import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';

@Component({
	selector: 'gallery-json-viewer',
	templateUrl: './gallery.json-viewer.component.html',
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
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryJsonViewerComponent implements OnInit {
	properties = [
		{
			Name: "data",
			Type: "Object",
			Description: "JSON data to display",
			Default: "false"
		}
	];
	events = [
	];

	sampleUsage: string = '<ig-json-viewer [data]="value"></ig-json-viewer>';

	exampleString = "{\r\n\t\"name\": \"ig-json-viewer\",\r\n\t\"url\": \"https://precisely.com\",\r\n\t\"string\": \"precisely\",\r\n\t\"number\": 1234,\r\n\t\"boolean\": true,\r\n\t\"object\": {\r\n\t\t\"obj1\": \"obj1\",\r\n\t\t\"obj2\": \"obj2\",\r\n\t\t\"object\": {\r\n\t\t\t\"obj11\": \"obj11\",\r\n\t\t\t\"obj22\": \"obj22\"\r\n\t\t},\r\n\t\t\"emptyArray\": []\r\n\t},\r\n\t\"array\": [\r\n\t\t1,\r\n\t\t2,\r\n\t\t3\r\n\t],\r\n\t\"null\": null\r\n}";
	exampleJson: object = null;

	ngOnInit() {
		this.showExample();
	}

	get isExampleValid() {
		try {
			JSON.parse(this.exampleString);
			return true;
		} catch (e) {
			return false;
		}
	}

	showExample() {
		try {
			this.exampleJson = JSON.parse(this.exampleString);
		} catch (e) {
			this.exampleJson = null;
		}
	}
}
