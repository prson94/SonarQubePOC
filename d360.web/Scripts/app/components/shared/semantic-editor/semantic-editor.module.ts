import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SemanticEditorComponent } from './semantic-editor.component';
import { CoreModule } from '../core.module';
import { DropdownModule } from 'primeng/dropdown';
import { IgNumberFieldModule } from '../controls/number-picker/number-input.component';
import { InputTextModule } from "primeng/inputtext";
import { IgMessageBoxModule } from '../controls/message-box/message-box.module';
import { MultiInputFieldModule } from "../controls/multi-input-field/multi-input-field.component";
import { RegexpInputModule } from '../controls/regexp/regexp-input.component';
import { SwitchModule } from '../controls/switch/switch';
import { MultiSelectModule } from 'primeng/multiselect';
import { CodeAreaModule } from '../controls/codearea/codearea.component';
import { TooltipModule } from 'primeng/tooltip';
import { PropertyGroupModule } from "../controls/property-group/property-group.component";

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        CoreModule,
        DropdownModule,
        IgNumberFieldModule,
        ReactiveFormsModule,
        InputTextModule,
        IgMessageBoxModule,
        MultiInputFieldModule,
        RegexpInputModule,
        SwitchModule,
        MultiSelectModule,
        CodeAreaModule,
        TooltipModule,
        PropertyGroupModule
    ],
    declarations: [        
        SemanticEditorComponent
    ],
    exports: [
        SemanticEditorComponent
    ],
    providers: [

    ]
})

export class SemanticEditorModule { }