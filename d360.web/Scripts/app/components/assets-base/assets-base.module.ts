import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';
import { AssetsBaseComponent } from './assets-base.component';
import { AssetsBaseRoutingModule } from './assets-base.routes';
import { ArtifactModule } from '../artifact/artifact.module';
import { RuleModule } from '../rule/rule.module';
import { HierarchyModule } from '../hierarchy/hierarchy.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

		AssetsBaseRoutingModule,
		ArtifactModule,
		RuleModule,
		HierarchyModule
    ],
    declarations: [        
		AssetsBaseComponent
	],
    providers: [
        
    ]
})

export class AssetsBaseModule { }
