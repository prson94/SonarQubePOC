import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { AuthenticationService } from '../../services/authentication.service';

@Component({
	selector: 'd3s-session-timeout-modal',
	templateUrl: './session-timeout-modal.component.html',
	styleUrls: ['./session-timeout-modal.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})
export class SessionTimeoutModalComponent implements OnDestroy, OnInit {
	sessionExpiresOn: Date = null;
	modalPopupTimeInSeconds: number = 30;

	intervalCheckTime: number = 1000;

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	sessionTimeoutCheckInterval: any;

	isModalVisible: boolean = false;
	constructor(private cdRef: ChangeDetectorRef,
		private authService: AuthenticationService) {
	}

	ngOnInit() {
		this.initializeWatch();
	}


	initializeWatch() {
		this.authService.getSessionTimeout().subscribe((res) => {
			if (res) {
				this.sessionExpiresOn = new Date(res)
				if (this.sessionTimeoutCheckInterval) {
					clearInterval(this.sessionTimeoutCheckInterval)
				}

				if (this.sessionExpiresOn) {
					this.sessionTimeoutCheckInterval = setInterval(this.checkSessionTimeout.bind(this), this.intervalCheckTime)
				}

				console.info("Session expiration:", this.sessionExpiresOn)
			}
		})
	}

	clearWatch = () => {
		if (this.sessionTimeoutCheckInterval) {
			clearInterval(this.sessionTimeoutCheckInterval)
		}
	}

	ngOnDestroy() {
		this.clearWatch();
	}

	checkSessionTimeout() {
		if (this.timeUntilLogout < this.modalPopupTimeInSeconds) {
			this.isModalVisible = true;
			this.cdRef.markForCheck();
		}
	}

	get timeUntilLogout(): number {
		return +((this.sessionExpiresOn.getTime() - new Date().getTime()) / 1000).toFixed(0);
	}

	doNothing() {
		this.isModalVisible = false;
		this.clearWatch();
		this.cdRef.markForCheck();
	}

	extendSession() {
		this.clearWatch();
		this.authService.resetSessionTimeout().subscribe(() => {
			this.initializeWatch();
			this.isModalVisible = false;
			this.cdRef.markForCheck();

		})
	}

	signOut() {
		window.location.href = '/slo'
	}

}
