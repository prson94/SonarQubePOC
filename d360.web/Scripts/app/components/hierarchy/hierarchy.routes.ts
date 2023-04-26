import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HierarchyComponent } from './hierarchy.component';
import { HierarchyItemComponent } from './hierarchy-item.component';
import { HierarchyItemStructureComponent } from './hierarchy-item-structure.component';

const routes: Routes = [
    {
        path: '',
        component: HierarchyComponent,
        children: [
            { path: ':typeId/structure', component: HierarchyItemStructureComponent },
            { path: 'structure/:uid', component: HierarchyItemStructureComponent },
            { path: ':typeId', component: HierarchyItemComponent },
            { path: ':typeId/id/:id', component: HierarchyItemComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class HierarchyRoutingModule { }