import { Component, OnInit } from '@angular/core';
import { AssetDetailClickEvent, LinkClickInterceptor } from '../../../services/href-click-service';
import { Subscription } from 'rxjs';

@Component({
	selector: 'd3s-side-panel-switcher',
	templateUrl: './side-panel-switcher.component.html',
	styleUrls: ['./side-panel-switcher.component.less']
})
export class SidePanelSwitcherComponent implements OnInit {
	selectedTag: any;
	selectedReferenceItem: any;
	selectedAsset: any;
	sidePanelTab: string;
	dataProfile: any;
	selection: any;
	assetGrid: any;
	private linkInterceptorSubscription: Subscription;

	constructor(private linkClickInterceptor: LinkClickInterceptor) {
	}

	secondaryPanelOpen($event: any) {

	}

	ngOnInit(): void {
		// this.linkInterceptorSubscription = this.linkClickInterceptor.getEvents().subscribe((assetDetailClickEvent: AssetDetailClickEvent) => {
		// 	this.linkClickInterceptor.handleEvent(this, assetDetailClickEvent);
		// });
	}

	clear() {
		this.selectedTag = undefined;
		this.selectedReferenceItem = undefined;
		this.selectedAsset = undefined;
		this.selection = undefined;
	}
}
