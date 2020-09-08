import { Component, OnInit, ChangeDetectionStrategy, AfterContentInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';


@Component({
    selector: 'gallery-number-field',
    templateUrl: './gallery.number-field.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryNumberFieldComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<input igNumberField type="number" name="Number" />';
    private val1: string;
    private val2: string;
    private formValue = new FormControl('', [Validators.min(4), Validators.max(10)]);
    private formValue2 = new FormControl('', [Validators.min(4), Validators.max(10)]);

    ngOnInit(): void {

        this.properties = new Array();
        
    }

}
