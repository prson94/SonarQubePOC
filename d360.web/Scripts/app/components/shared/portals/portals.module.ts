import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { PortalRendererComponent } from './portal-renderer.component';
import { PortalContentProviderComponent } from './portal-content-provider.component';

@NgModule({
    imports: [
        CommonModule
    ],
    declarations: [
        PortalRendererComponent,
        PortalContentProviderComponent
    ],
    exports: [
        PortalRendererComponent,
        PortalContentProviderComponent
    ],
    providers: []
})
export class PortalsModule { }