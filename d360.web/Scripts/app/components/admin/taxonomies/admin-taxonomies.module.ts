import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

//import { ColorPickerModule } from 'ngx-color-picker';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';

import { AdminModule } from '../admin.module';

import { AdminTaxonomiesComponent } from './admin-taxonomies.component';
import { AdminTaxonomyDetailComponent } from './admin-taxonomy-detail.component';

import { AdminTaxonomiesRoutingModule } from './admin-taxonomies.routes';

import {
    ButtonModule,
    ColorPickerModule,
    EditorModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
    DataTableModule,
} from 'primeng/primeng';



@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminTaxonomiesRoutingModule,

        //prime
        ButtonModule,
        EditorModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        DataTableModule,

        // color picker
        ColorPickerModule,

        //d3s       
        AdminModule,
        CoreModule,
        PipesModule,
        
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SharedResponsibilitiesModule,
        SharedFieldDefinitionModule,
        SharedDynamicGridEditorModule,
        TilesModule,
    ],
    declarations: [
        AdminTaxonomiesComponent,
        AdminTaxonomyDetailComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminTaxonomiesModule { }