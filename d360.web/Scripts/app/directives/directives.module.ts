import { NgModule } from '@angular/core';
import { TextHighlightDirective } from './text-highlight-directive';
import { CopyClipboardDirective } from './copy-clipboard-directive';
import { ClickOutsideDirective } from './click-outside-directive';
import { ButtonDirective, ButtonModule } from './ig-button-directive';
import { InputDirective, InputModule } from './ig-input-directive';
import { AutocompleteDirective, AutocompleteModule } from './ig-autocomplete-directive';
import { TextAreaDirective, TextAreaModule } from './ig-textarea-directive';
import { AutoFocusDirective } from './ig-autofocus.directive';
import { CheckboxDirective, IgCheckboxModule } from './ig-checkbox-directive';
import { DropdownModule, DropdownDirective } from './ig-dropdown.directive';
import { DataCyDirective, DataCyModule } from './ig-data-cy.directive';
import { RadioButtonDirective, IgRadioButtonModule } from './ig-radio-button-directive';
import { NgLetDirective } from './ng-let-directive';
import { LinkWithContextDirective } from './link-with-context-menu-directive';


@NgModule({ 
    imports: [
        ButtonModule,
        InputModule,
        TextAreaModule,
        AutocompleteModule,
        DropdownModule,
        DataCyModule,
        IgCheckboxModule,
        IgRadioButtonModule
    ],
    declarations: [
        TextHighlightDirective,
        CopyClipboardDirective,
        ClickOutsideDirective,  
        AutoFocusDirective,
        NgLetDirective,
        LinkWithContextDirective
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
        DropdownDirective,
        DataCyDirective,
        RadioButtonDirective,
        CheckboxDirective,
        NgLetDirective,
        LinkWithContextDirective
    ]
})
export class DirectivesModule { }