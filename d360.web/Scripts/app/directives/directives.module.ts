import { NgModule } from '@angular/core';
import { TextHighlightDirective } from './text-highlight-directive';
import { CopyClipboardDirective } from './copy-clipboard-directive';
import { ClickOutsideDirective } from './click-outside-directive';
import { ButtonDirective, ButtonModule } from './ig-button-directive';
import { InputDirective, InputModule } from './ig-input-directive';
import { AutocompleteDirective, AutocompleteModule } from './ig-autocomplete-directive';
import { TextAreaDirective, TextAreaModule } from './ig-textarea-directive';
import { AutoFocusDirective } from './ig-autofocus.directive';
import { CheckboxDirective } from './ig-checkbox-directive';
import { DropdownModule, DropdownDirective } from './ig-dropdown.directive';


@NgModule({ 
    imports: [
        ButtonModule,
        InputModule,
        TextAreaModule,
        AutocompleteModule,
        DropdownModule
    ],
    declarations: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,
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
        DropdownDirective
    ]
})
export class DirectivesModule { }