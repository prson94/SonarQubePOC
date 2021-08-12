import { Component, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


@Component({
    selector: 'gallery-back-button',
    templateUrl: './gallery.back-button.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
        .event-label {
            width: 110px;
            font-weight: bold;
            display: inline-block;
            }`]
})

export class GalleryBackButtonComponent {
    m1State: boolean = false;
    m2State: boolean = false;
    m3State: boolean = false;


    constructor(private ref: ChangeDetectorRef) {
    }
}
