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
    sampleUsage: string = ` <p-radioButton igRadioButton name="groupName" [(ngModel)]="val" value="Of Course!" label="Yes"></p-radioButton>`;

    val: any;
    val2: any;
    val3: any;
    formValue: any;

    constructor(private cdRef: ChangeDetectorRef) {}

    test() {
        alert(this.formValue);
    }

    ngOnInit(): void {
        this.properties = new Array();
    }
}
