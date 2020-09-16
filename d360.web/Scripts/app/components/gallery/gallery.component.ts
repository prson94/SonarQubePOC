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
        { label: 'Tooltip', key: 'tooltip' },
        { label: 'Auto Focus Directive', key: 'auto-focus' },
        { label: 'Color Picker', key: 'color-picker' },
        { label: 'Color Variables', key: 'color-variables' },
        { label: 'Text Area', key: 'textarea' },
        { label: 'Date Picker', key: 'date-picker' },
        { label: 'Loading Component', key: 'loading' },
        { label: 'Accordion', key: 'accordion' },
        { label: 'Page Info', key: 'paging-info' },
        { label: 'Selection Info', key: 'selection-info' },
        { label: 'Number Field', key: 'number-field' },
        { label: 'Message Box', key: 'message-box' },
        { label: 'Badge', key: 'badge' },
    ];

    ngOnInit(): void {        
    }
}
