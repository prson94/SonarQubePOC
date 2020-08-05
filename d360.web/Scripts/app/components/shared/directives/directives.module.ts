import { NgModule } from '@angular/core';
import { DelayedInputDirective } from './delayed-input.directive';

@NgModule({
    declarations: [                           
        DelayedInputDirective
    ],
    exports: [                                                                                                                                        
        DelayedInputDirective
        ]
    , imports: [
    ],
    providers: []
})

export class DirectivesModule { }