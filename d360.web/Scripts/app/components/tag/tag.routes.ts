import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { TagComponent } from './tag.component';
import { TagItemComponent } from './tag-item.component';

const routes: Routes = [
    {
        path: '',
        component: TagComponent,
        children: [            
            { path: ':tagUid', component: TagItemComponent }          
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class TagRoutingModule { }

