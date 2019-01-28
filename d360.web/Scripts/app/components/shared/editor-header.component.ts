import { Component, Input, Output, EventEmitter, NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'd3s-editor-header',
    templateUrl: './editor-header.component.html'
})

export class EditorHeaderComponent {
    //contains the default header html for the prime p-editor control. Due to
    //limitations in prime this has to be manually added in when modifying the header.
    //Accessing the quill header API directly is not possible

    constructor() { }

}

@NgModule({
    declarations: [
        EditorHeaderComponent
    ],
    exports: [
        EditorHeaderComponent
    ]
    , imports: [
        CommonModule,
    ],
})
export class D3SEditorHeaderModule { }
