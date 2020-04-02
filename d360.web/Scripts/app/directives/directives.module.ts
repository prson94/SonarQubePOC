import { NgModule } from '@angular/core';
import { TextHighlightDirective } from './text-highlight-directive';
import { CopyClipboardDirective } from './copy-clipboard-directive';
import { ClickOutsideDirective } from './click-outside-directive';
import { ButtonDirective } from './ig-button-directive';


@NgModule({ 
    imports: [],
    declarations: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
        ButtonDirective,
    ],
    exports: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
        ButtonDirective,
    ]
})
export class DirectivesModule { }