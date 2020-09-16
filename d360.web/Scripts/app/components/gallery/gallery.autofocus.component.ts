import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-autofocus',
    templateUrl: './gallery.autofocus.component.html',
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

export class GalleryAutoFocusComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<button igAutoFocus igButton></button>';
    showModal = false;

    ngOnInit(): void {
        this.properties = new Array();
    }
}
