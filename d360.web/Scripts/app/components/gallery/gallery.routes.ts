import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { GalleryComponent } from './gallery.component';
import { GalleryGuard } from '../../guards/gallery.guard';

const routes: Routes = [
    {
        path: '',
        canActivate: [GalleryGuard],
        component: GalleryComponent        
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class GalleryRoutingModule { }