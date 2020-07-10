import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-button',
    templateUrl: './gallery.button.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryButtonComponent implements OnInit {
    ngOnInit(): void {
    }
}
