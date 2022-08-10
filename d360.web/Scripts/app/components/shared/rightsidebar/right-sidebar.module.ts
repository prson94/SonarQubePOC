import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RightSidebarComponent } from './right-sidebar.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';
import { TagUsageInfoModule } from '../../admin/tags/tags-usage-info.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SiteModalModule } from '../modal/gov-modal.module';
import { TakeSurveyModule } from '../survey/take-survey.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { ScoreBadgeModule } from '../small-widgets/score-badge/score-badge.module';
import { InfoTooltipModule } from '../tooltip/info-tooltip.component';
import { SimpleBadgeModule } from '../small-widgets/simple-badge/simple-badge.module';
import { PortalsModule } from '../portals/portals.module';
import { DataCyModule } from '../../../directives/ig-data-cy.directive';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        SharedDynamicGridEditorModule,
        DataCyModule,
        TagUsageInfoModule,
        SharedDeleteFormModule,
        SiteModalModule,
        TakeSurveyModule,
        PipesModule,
        ScoreBadgeModule,
        InfoTooltipModule,
        SimpleBadgeModule,
        PortalsModule
    ],
    declarations: [
        RightSidebarComponent
    ],
    exports: [        
        RightSidebarComponent
    ],
    providers: [

    ]
})
export class RightsidebarModule { }