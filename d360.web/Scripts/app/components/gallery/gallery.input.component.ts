import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-input',
    templateUrl: './gallery.input.component.html',
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

export class GalleryInputComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<input igInput type="text" name="name" maxlength="250" />';
    protected loadingState: boolean = false;
    protected disabledState: boolean = false;
    protected showError: boolean = false;
    protected showError2: boolean = false;
    private formValue: string;
    private val1: string;
    private val2: string;

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "igSize", Type: "string", Description: "Sixe of the input. Options are small(150px), medium(308px), large(624px) and full(100%).", Default: "full" });
    }

    toggleDisabled() {
        this.disabledState = !this.disabledState;
    }

}
