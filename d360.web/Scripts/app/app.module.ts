import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { AppRoutingModule }        from './app.routes';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { COMPILER_PROVIDERS } from '@angular/compiler';

import { ChartModule } from 'angular2-highcharts';

import { ColorPickerModule } from 'angular2-color-picker';


import { PipesModule } from './pipes/pipes.module';
import { CoreModule } from './components/shared/core.module';
import { SearchModule } from './components/search/search.module';
import { WorkflowModule } from './components/workflow/workflow.module';
import { D3SSharedModule } from './components/shared/shared.module';
import { SocialModule } from './components/social/social.module';
import { FusionModule } from './components/fusion/fusion.module';
import { GroupModule } from './components/group/group.module';
import { MonitorModule } from './components/monitor/monitor.module';
import { ReferenceModule } from './components/reference/reference.module';
import { PolicyModule } from './components/policy/policy.module';
import { HomeModule } from './components/home/home.module';


import { D3SFormsModule } from './components/forms/d3sforms.module'; // why are some forms in a separate module instead of by area?

import { AdminUserGuard } from './guards/admin-user.guard';

import { AuthenticationService } from './services/authentication.service';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService, RightSidebarService, WebAnalyticsService, StateService  } from './services/index';
import { DynamicTypeBuilder }     from './services/dynamic-type-builder';

import { AuthenticationConnectionBackend } from './authentication-connection-backend';

import {
    GrowlModule,    
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,
    EditorModule,
    TooltipModule,        
    PaginatorModule,
    DataListModule,
    TreeModule,
    OverlayPanelModule,
    SharedModule,
} from 'primeng/primeng';


import {
    ArtifactBaseComponent,
    ArtifactColumnFilterComponent,    
    ArtifactComponent,
    ArtifactDefnintionComponent,
    ArtifactGridComponent,
    ArtifactItemChildGridComponent,
    ArtifactItemChildrenComponent,
    ArtifactItemComponent,    
    ArtifactListComponent,
    ArtifactTopLevelListComponent,   
    ArtifactTypeMetricsComponent,
    ArtifactTypeWorkflowStatusComponent,     
} from './components/artifact/index';

import {
    HeaderActionsComponent,
    HeaderBreadcrumbComponent,
    HeaderBreadcrumbItemComponent,
    HeaderComponent,
    HeaderTypeaheadSearchComponent,
    HeaderFavoritesComponent,
    HeaderFollowComponent,
} from './components/header/index';

import {
    ModelComponent,
    ModelItemComponent,
    ModelListComponent,
    ModelItemStructureComponent,
} from './components/model/index';

import {
    ActionBar,        
    MenuPartItem,
    MenuPart,    
} from './components/parts/index';

import {
    ResourceComponent,
    ResourceItemComponent,
    ResourceApiComponent,
    ResourceListComponent,
    ResourceGroupsComponent,
} from './components/resource/index';

import { RightSidebarComponent } from './components/rightsidebar/right-sidebar.component';
import { RightSidebarItemComponent } from './components/rightsidebar/right-sidebar-item.component';

import {
    RuleComponent,
    RuleColumnFilterComponent,
    RuleItemComponent,
    RuleListComponent,
    RuleResultsGridComponent,
} from './components/rule/index';

import {
    AttributesTile,               
    MenuBarItem,    
    ObjectDefinitionTile,                  
    StructureTile,    
    SynonymsTile,            
    ResourceFollowingTile,    
    ResourceFollowingGridTile,    
} from './components/tiles/index';

@NgModule({
    declarations: [
        ActionBar,        
        AppComponent,
        ArtifactColumnFilterComponent,        
        ArtifactComponent,
        ArtifactDefnintionComponent,
        ArtifactGridComponent,
        ArtifactItemChildGridComponent,
        ArtifactItemChildrenComponent,
        ArtifactItemComponent,
        ArtifactListComponent,
        ArtifactTopLevelListComponent,        
        ArtifactTypeMetricsComponent,
        ArtifactTypeWorkflowStatusComponent,
        AttributesTile,                           
        HeaderActionsComponent,
        HeaderBreadcrumbComponent,
        HeaderBreadcrumbItemComponent,
        HeaderComponent,
        HeaderFavoritesComponent,
        HeaderFollowComponent,
        HeaderTypeaheadSearchComponent,                       
        MenuPart,    
        ModelComponent,
        ModelItemComponent,        
        ModelListComponent,
        ModelItemStructureComponent,        
        ObjectDefinitionTile,                                                                           
        ResourceApiComponent,
        ResourceComponent,
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourceItemComponent,                
        ResourceListComponent,     
        ResourceGroupsComponent,
        RightSidebarComponent,
        RightSidebarItemComponent,
        RuleColumnFilterComponent,
        RuleComponent,        
        RuleItemComponent,
        RuleListComponent,    
        RuleResultsGridComponent,            
        StructureTile,        
        SynonymsTile,                                  
    ],
    imports: [
        BrowserModule,
        FormsModule,
        ReactiveFormsModule,
        AppRoutingModule,
        HttpModule,

        //primeng
        GrowlModule,        
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,
        SpinnerModule,
        EditorModule,
        TooltipModule,                
        PaginatorModule,
        TreeModule,
        OverlayPanelModule,
        DataListModule,
        SharedModule,


        //highcharts
        ChartModule,

        ColorPickerModule,

        //d3s modules
        PipesModule,
        SearchModule,
        WorkflowModule,
        D3SSharedModule,  
        SocialModule,           
        FusionModule,
        D3SFormsModule,
        GroupModule,        
        CoreModule,
        MonitorModule,
        ReferenceModule,
        PolicyModule,      
        HomeModule,          
    ],
    bootstrap: [AppComponent],
    providers: [
        AdminUserGuard,
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
        AuthenticationService,
        COMPILER_PROVIDERS,
        DynamicTypeBuilder,
        Title,
        HeaderActionsService,
        HeaderBreadcrumbService,
        MessagesService,        
        RightSidebarService,
        WebAnalyticsService,
        StateService
    ],    
})
export class AppModule { }







