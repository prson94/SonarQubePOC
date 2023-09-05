import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";

import { UserAvatarComponent } from "./user-avatar.component";

@NgModule({
	imports: [
		CommonModule
	],
	declarations: [
		UserAvatarComponent
	],
	exports: [
		UserAvatarComponent
	],
	providers: [

	]
})
export class UserAvatarModule { }