import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from '@angular/core';
import { AuthenticationService } from '../../../services/authentication.service';
import { Data } from '@angular/router';

@Component({
	selector: 'd3s-session-timeout-modal',
	templateUrl: './session-timeout-modal.component.html',
	styleUrls: ['./session-timeout-modal.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SessionTimeoutModalComponent {
	sessionExpiresOn: Date = null

	sessionTimeoutCheckInterval: any
	isModalVisible: boolean = false;
	constructor(private cdRef: ChangeDetectorRef,
		private authService: AuthenticationService) {

		this.authService.getSessionTimeout().subscribe((res) => {
			if (res) {
				this.sessionExpiresOn = new Date(res)
				if (this.sessionExpiresOn) {
					if (this.sessionTimeoutCheckInterval) {
						clearInterval(this.sessionTimeoutCheckInterval)
					}
					this.sessionTimeoutCheckInterval = setInterval(this.checkSessionTimeout.bind(this), 5000)
				}
			}
		})		
	}

	checkSessionTimeout() {
		const tmLoc = new Date();
		const currentDate = tmLoc.getTime() + tmLoc.getTimezoneOffset() * 60000;
		const time = (this.sessionExpiresOn.getTime() - currentDate) / 1000;

		this.isModalVisible = true;
		console.log("Current date", time)
		this.cdRef.markForCheck();
	}
}
