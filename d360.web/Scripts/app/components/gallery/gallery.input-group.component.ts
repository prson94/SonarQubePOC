import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";


@Component({
    selector: "gallery-input-group",
    templateUrl: "./gallery.input-group.component.html",
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

export class GalleryInputGroupComponent implements OnInit {
    validValue = 50;
    inValidValue = 120;
    sampleUsage = `            <div class="ig-inputgroup">
                <span class="ig-input-addon">DQ-</span>
                <ig-number-input></ig-number-input>
                <span class="ig-input-addon">%</span>
            </div>`;
    form: FormGroup = null;
    constructor(private fb: FormBuilder) { }

    ngOnInit() {
        this.form = this.fb.group({
            myNumber: [null, { validators: [Validators.required, Validators.min(1), Validators.max(100)], updateOn: "blur" }]
        });
    }
}
