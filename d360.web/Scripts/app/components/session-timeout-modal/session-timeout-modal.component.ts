import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { AuthenticationService } from '../../services/authentication.service';
import { LastCallTimeService } from "../../services/lastCallTime.service";
import { throttleTime } from 'rxjs';

@Component({
	selector: 'd3s-session-timeout-modal',
	templateUrl: './session-timeout-modal.component.html',
	styleUrls: ['./session-timeout-modal.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})
export class SessionTimeoutModalComponent implements OnDestroy, OnInit {
	sessionExpiresOn: Date = null;
	modalPopupTimeInSeconds: number = 60;

	intervalCheckTime: number = 1000;

	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	sessionTimeoutCheckInterval: any;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	apiTimeoutCheck: any;

	isModalVisible: boolean = false;
	isModalSupressed: boolean = false;
	constructor(private cdRef: ChangeDetectorRef,
		private authService: AuthenticationService,
		private lastCallService: LastCallTimeService) {
	}

	ngOnInit() {
		this.initializeWatch();
		this.initializeAPIWatch();
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

	initializeAPIWatch() {
		this.lastCallService.lastAPICall$.pipe(throttleTime(this.modalPopupTimeInSeconds * 500, null, { leading: true, trailing: true})).subscribe(() => {
			if (this.apiTimeoutCheck) {
				clearTimeout(this.apiTimeoutCheck)
			}
			this.apiTimeoutCheck = setTimeout(() => this.extendSession(), 500);
		})
	}

	clearWatch = () => {
		if (this.sessionTimeoutCheckInterval) {
			clearInterval(this.sessionTimeoutCheckInterval)
		}
	}

	ngOnDestroy() {
		this.clearWatch();
		if (this.apiTimeoutCheck) {
			clearTimeout(this.apiTimeoutCheck)
		}
	}

	checkSessionTimeout() {
		if (this.timeUntilLogout <= 0) {
			this.signOut();
		}
		if (this.timeUntilLogout < this.modalPopupTimeInSeconds && !this.isModalSupressed) {
			this.isModalVisible = true;
			this.cdRef.markForCheck();
		}
	}

	get timeUntilLogout(): number {
		return +((this.sessionExpiresOn.getTime() - new Date().getTime()) / 1000).toFixed(0);
	}

	doNothing() {
		this.isModalVisible = false;
		this.isModalSupressed = true;
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
