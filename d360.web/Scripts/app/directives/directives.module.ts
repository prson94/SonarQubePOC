import { NgModule } from '@angular/core';
import { TextHighlightDirective } from './text-highlight-directive';
import { CopyClipboardDirective } from './copy-clipboard-directive';
import { ClickOutsideDirective } from './click-outside-directive';
import { ButtonDirective } from './ig-button-directive';
import { InputDirective } from './ig-input-directive';
import { AutocompleteDirective } from './ig-autocomplete-directive';


@NgModule({ 
    imports: [],
    declarations: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
        ButtonDirective,
        InputDirective,
        AutocompleteDirective
    ],
    exports: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
        ButtonDirective,
        InputDirective,
        AutocompleteDirective
    ]
})
export class DirectivesModule { }