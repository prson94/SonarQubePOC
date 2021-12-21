import { Input, Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'd3s-highlight-search-text',
    template: `<span [innerHTML]="html | safeHtml"></span>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HighlightSearchTextComponent {
    @Input() text: string;
    @Input() highlight: string;

    public get html() {
        if (!this.highlight) {
            return this.text;
        }

        return this.text.replace(new RegExp(this.highlight, "gi"), match => {
            return '<span style="background: #f5eeff;">' + match + '</span>';
        });
    }
}