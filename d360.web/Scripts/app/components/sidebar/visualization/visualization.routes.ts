import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BrowserComponent } from './browser.component';
import { DeactivateGuard } from '../../../guards/deactivate.guard';

const routes: Routes = [
	{ path: '', component: BrowserComponent, canDeactivate: [DeactivateGuard] }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class VisualizationRoutingModule { }