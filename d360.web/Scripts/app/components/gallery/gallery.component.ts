import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery',
    templateUrl: './gallery.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryComponent implements OnInit {
    protected activeControl: string = "boolean";
    protected controls = [
        { label: 'Boolean Input', key: 'boolean' },
        { label: 'Button Directive', key: 'button' },
        { label: 'Icon Picker', key: 'icon-picker' },
    ];

    ngOnInit(): void {        
    }
}
