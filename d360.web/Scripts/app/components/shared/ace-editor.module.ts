import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';


import { AceEditorDirective } from 'ng2-ace-editor';

@NgModule({
    declarations: [
        AceEditorDirective,
    ],
    exports: [
        AceEditorDirective,        
    ]    

})

export class AceEditorModule { }