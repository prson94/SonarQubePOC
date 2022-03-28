import { ChangeDetectionStrategy, Component, Input, ViewEncapsulation } from '@angular/core';

@Component({
    selector: "theme-editor",
    templateUrl: "theme-editor.component.html",
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./theme-editor.component.less"]
})

export class ThemeEditorComponent {
    @Input() uid: string = '';
    @Input() isVisible: boolean = false;

    constructor() {
    }
}