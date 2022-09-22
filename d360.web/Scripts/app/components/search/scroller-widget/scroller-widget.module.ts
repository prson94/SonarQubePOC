import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ScrollerWidgetComponent } from './scroller-widget.component';

@NgModule({
	imports: [
		CommonModule
	],
	declarations: [
		ScrollerWidgetComponent
	],
	exports: [
		ScrollerWidgetComponent
	]
})
export class ScrollerWidgetModule { }
