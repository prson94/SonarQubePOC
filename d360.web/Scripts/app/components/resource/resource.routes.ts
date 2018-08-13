import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ResourceComponent } from './resource.component';
import { ResourceItemComponent } from './resource-item.component';
import { ResourceListComponent } from './resource-list.component';
import { ResourceKeyComponent } from './resource-key.component';
import { ResourceChangePwdComponent } from './resource-change-pwd.component';
import { ApiKeyUsersGuard } from '../../guards/api-key-users.gurard';

const routes: Routes = [
    {
        path: '',
        component: ResourceComponent,
        children: [
            { path: '', component: ResourceListComponent },
            { path: ':resourceId', component: ResourceItemComponent },
            { path: 'my/apikey', component: ResourceKeyComponent, canActivate: [ApiKeyUsersGuard] },
            { path: ':resourceId/changepassword', component: ResourceChangePwdComponent }
        ]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ResourceRoutingModule { }

