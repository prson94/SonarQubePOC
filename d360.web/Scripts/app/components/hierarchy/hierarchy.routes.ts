import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HierarchyComponent } from './hierarchy.component';
import { HierarchyListComponent } from './hierarchy-list.component';
import { HierarchyItemComponent } from './hierarchy-item.component';
import { HierarchyItemStructureComponent } from './hierarchy-item-structure.component';

const routes: Routes = [
    {
        path: '',
        component: HierarchyComponent,
        children: [
            { path: 'classification/:group', component: HierarchyListComponent },
            { path: 'classification', component: HierarchyListComponent },
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