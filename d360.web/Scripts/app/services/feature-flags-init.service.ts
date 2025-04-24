import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";

@Injectable({
    providedIn: 'root'
})
export class FeatureFlagsInitService extends BaseObservableService {

	FeatureFlags: Record<string, boolean> = null;

    constructor(private http: HttpClient, messagesService: MessagesObservableService) {
        super(messagesService);
    }

	async getFlags(): Promise<boolean>  {
		const response = await fetch('api/v2/environment/feature-flags');		
		if (response.ok) {
			this.FeatureFlags = await response.json();
		}
		return true;
	}

	async getFlagValue(flag: string): Promise<boolean> {
		if (!this.FeatureFlags) {
			await this.getFlags();
		}
		let value: boolean = false;
		if (this.FeatureFlags) {
			value = this.FeatureFlags[flag];
		}
		return value;
	}
}
