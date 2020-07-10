import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-button',
    templateUrl: './gallery.button.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryButtonComponent implements OnInit {
    protected sampleUsage: string = '<button igButton icon="fa-ellipsis-v" tooltip="More..."></button>';
    ngOnInit(): void {
    }
}
