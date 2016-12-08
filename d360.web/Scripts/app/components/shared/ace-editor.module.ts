import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { AceEditorComponent } from 'ng2-ace-editor';

@NgModule({
    declarations: [
        AceEditorComponent,
    ],
    exports: [
        AceEditorComponent,        
    ]    

})

export class AceEditorModule { }