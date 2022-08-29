import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AssetRoutingModule } from './asset.routes';
import { AssetComponent } from './asset.component';
import { ArtifactModule } from '../artifact/artifact.module';
import { HierarchyModule } from '../hierarchy/hierarchy.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

		AssetRoutingModule,
		ArtifactModule,
		HierarchyModule
    ],
    declarations: [        
        AssetComponent
    ],
    providers: [
        
    ]
})

export class AssetModule { }
