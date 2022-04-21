import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule }    from '@angular/forms';

import { SemanticsComponent } from './semantics.component';
import { SemanticDetailComponent } from "./semantic-detail.component";
import { PopupMenuModule } from "../shared/controls/popup-menu/popup-menu.component";
import { SemanticsRoutingModule } from './semantics.routes';
import { SemanticTypeListComponent } from './semantic-type-list.component';
import { SemanticDefinitionComponent } from './semantic-type-definition.component';
import { AdvancedFiltersModule } from "../assets-grid/advanced-filtering/advanced-filtering.module";
import { SearchFieldModule } from "../shared/controls/search-field/search-field.component";
import { TableModule } from 'primeng/table';
import { DirectivesModule } from '../../directives/directives.module';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { CoreModule } from '../shared/core.module';
import { PropertyGroupModule } from "../shared/controls/property-group/property-group.component";
import { CodeAreaModule } from '../shared/controls/codearea/codearea.component';
import { TooltipModule } from 'primeng/tooltip';
import { SemanticAssetListGridComponent } from './semantic-asset-list-grid.component';
import { SemanticTypeAssetListComponent } from './semantic-asset-list.component';
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module';
import { DataProfileModule } from '../shared/dataprofile/dataprofile.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SemanticStatusDetailComponent } from './semantic-status-detail.component';
import { SharedDeleteFormModule } from '../shared/delete.form';

import { SiteModalModule } from '../shared/modal/gov-modal.module';
import { SemanticEditorModule } from '../shared/semantic-editor/semantic-editor.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        SemanticsRoutingModule,
        AdvancedFiltersModule,
        SearchFieldModule,
        TableModule,
        PopupMenuModule,
        DirectivesModule,
        SidePanelModule,
        CoreModule,
        PropertyGroupModule,
        CodeAreaModule,
        TooltipModule,
        AssetDetailModule,
        DataProfileModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        SiteModalModule,
        SemanticEditorModule
    ],
    declarations: [
        SemanticsComponent,
        SemanticTypeListComponent,
        SemanticDetailComponent,
        SemanticDefinitionComponent,
        SemanticAssetListGridComponent,
        SemanticTypeAssetListComponent,
        SemanticStatusDetailComponent
    ],
    exports: [
        SemanticDetailComponent,
        SemanticStatusDetailComponent,
    ],
    providers: [
        
    ]
})

export class SemanticsModule { }
