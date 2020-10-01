import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-radio-button',
    templateUrl: './gallery.radio-button.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        .row{
            padding: 5px
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryRadioButtonComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = ` <p-radioButton igRadioButton name="groupName" [(ngModel)]="val" value="Of Course!" label="Yes"></p-radioButton>`;

    private val: any;
    private val2: any;
    private val3: any;
    private formValue: any;

    constructor(private cdRef: ChangeDetectorRef) {}

    test() {
        alert(this.formValue);
    }

    ngOnInit(): void {
        this.properties = new Array();
    }
}
