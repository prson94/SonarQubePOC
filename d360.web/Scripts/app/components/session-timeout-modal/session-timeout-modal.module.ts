import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SiteModalModule } from '../shared/modal/gov-modal.module';
import { SessionTimeoutModalComponent } from './session-timeout-modal.component';
import { DirectivesModule } from '../../directives/directives.module';


@NgModule({
	imports: [
		CommonModule,
		SiteModalModule,
		DirectivesModule
	],
	declarations: [
		SessionTimeoutModalComponent,
	],
	exports: [
		SessionTimeoutModalComponent
	]
})
export class SessionTimeoutModalModule { }