import { NgModule } from '@angular/core';
import { TextHighlightDirective } from './text-highlight-directive';
import { CopyClipboardDirective } from './copy-clipboard-directive';


@NgModule({ 
    imports: [],
    declarations: [
        TextHighlightDirective,
        CopyClipboardDirective,
    ],
    exports: [
        TextHighlightDirective,
        CopyClipboardDirective,
    ]
})
export class DirectivesModule { }