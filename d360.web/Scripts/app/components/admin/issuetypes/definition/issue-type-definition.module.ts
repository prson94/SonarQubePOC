import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TableModule } from 'primeng/table';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { IssueTypeDefinitionComponent } from './issue-type-definition.component';
import { CoreModule } from '../../../shared/core.module';
import { D3SSharedModule } from '../../../shared/shared.module';
import { PipesModule } from '../../../../pipes/pipes.module';
import { TilesModule } from '../../../shared/tiles/tiles.module';
import { PropertyGroupModule } from '../../../shared/controls/property-group/property-group.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //primeng
        TableModule,
        OverlayPanelModule,

        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
		TilesModule,
		PropertyGroupModule
    ],
	declarations: [
		IssueTypeDefinitionComponent
	],
	exports: [
		IssueTypeDefinitionComponent
	],
    providers: [
        
    ]
})
export class IssueTypeDefinitionModule { }