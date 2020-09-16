import { NgModule } from '@angular/core';
import { TextHighlightDirective } from './text-highlight-directive';
import { CopyClipboardDirective } from './copy-clipboard-directive';
import { ClickOutsideDirective } from './click-outside-directive';
import { ButtonDirective } from './ig-button-directive';
import { InputDirective } from './ig-input-directive';
import { AutocompleteDirective } from './ig-autocomplete-directive';
import { TextAreaDirective } from './ig-textarea-directive';
import { AutoFocusDirective } from './ig-autofocus.directive';
import { CheckboxDirective } from './ig-checkbox-directive';


@NgModule({ 
    imports: [],
    declarations: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
        ButtonDirective,
        InputDirective,
        TextAreaDirective,
        AutocompleteDirective,
        AutoFocusDirective,
        CheckboxDirective,        
    ],
    exports: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
        ButtonDirective,
        InputDirective,
        TextAreaDirective,
        AutocompleteDirective,
        AutoFocusDirective,
        CheckboxDirective,
    ]
})
export class DirectivesModule { }