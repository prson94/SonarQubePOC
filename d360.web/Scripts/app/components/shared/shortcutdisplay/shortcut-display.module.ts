import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { RouterModule } from '@angular/router';


import { CoreModule } from '../core.module';
import { ShortcutDisplayComponent } from './shortcut-display.component';

@NgModule({
    imports: [CommonModule,        

        RouterModule,
        CoreModule,        
    ],
    declarations: [        
        ShortcutDisplayComponent,
    ],
    exports: [        
        ShortcutDisplayComponent,
    ],
    providers: [
        
    ]
})
export class ShortcutDisplayModule { }