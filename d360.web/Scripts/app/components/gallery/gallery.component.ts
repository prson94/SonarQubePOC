import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery',
    templateUrl: './gallery.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryComponent implements OnInit {
    activeControl: string = "radio-button";
    controls = [
        { label: 'Switch Input', key: 'switch', type: 'form' },
        { label: 'Button Directive', key: 'button', type: 'form' },
        { label: 'Icon Picker', key: 'icon-picker', type: 'form' },
        { label: 'Tag Picker', key: 'tag-picker', type: 'govern' },
        { label: 'Text Field', key: 'input', type: 'form' },
        { label: 'Auto Complete', key: 'auto-complete', type: 'form' },
        { label: 'Tooltip', key: 'tooltip', type: 'overlay' },
        { label: 'Auto Focus Directive', key: 'auto-focus', type: 'misc' },
        { label: 'Color Picker', key: 'color-picker', type: 'form' },
        { label: 'Color Variables', key: 'color-variables', type: 'govern' },
        { label: 'Text Area', key: 'textarea', type: 'form' },
        { label: 'Date Picker', key: 'date-picker', type: 'form' },
        { label: 'Loading Component', key: 'loading', type: 'misc' },
        { label: 'Accordion', key: 'accordion', type: 'data' },
        { label: 'Page Info', key: 'paging-info', type: 'data' },
        { label: 'Selection Info', key: 'selection-info', type: 'data' },
        { label: 'Number Field', key: 'number-field', type: 'form' },
        { label: 'Message Box', key: 'message-box', type: 'data' },
        { label: 'Badge', key: 'badge', type: 'misc' },
        { label: 'Checkbox', key: 'checkbox', type: 'form' },
        { label: 'Popup Menu', key: 'popup-menu', type: 'overlay' },
        { label: 'Select', key: 'select', type: 'form' },
        { label: 'Property Group', key: 'propery-group', type: 'form' },
        { label: 'Radio Button', key: 'radio-button', type: 'form' },
        { label: 'Field Condition Grid', key: 'field-condition-grid', type: 'govern' },
    ];

    ngOnInit(): void {
        this.controls = this.controls.sort((a, b) => { return a.label > b.label ? 1 : -1 });
    }
}
