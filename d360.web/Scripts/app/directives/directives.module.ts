import { NgModule } from '@angular/core';
import { TextHighlightDirective } from './text-highlight-directive';
import { CopyClipboardDirective } from './copy-clipboard-directive';
import { ClickOutsideDirective } from './click-outside-directive';


@NgModule({ 
    imports: [],
    declarations: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
    ],
    exports: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
    ]
})
export class DirectivesModule { }