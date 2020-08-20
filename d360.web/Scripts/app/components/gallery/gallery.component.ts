import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery',
    templateUrl: './gallery.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryComponent implements OnInit {
    protected activeControl: string = "switch";
    protected controls = [
        { label: 'Switch Input', key: 'switch' },
        { label: 'Button Directive', key: 'button' },
        { label: 'Icon Picker', key: 'icon-picker' },
        { label: 'Tag Picker', key: 'tag-picker' },
        { label: 'Input Directive', key: 'input' },
        { label: 'Auto Complete', key: 'auto-complete' },
        { label: 'Color Picker', key: 'color-picker' },
        { label: 'Color Variables', key: 'color-variables' },
    ];

    ngOnInit(): void {        
    }
}
