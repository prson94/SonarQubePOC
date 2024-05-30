import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { LastCallTimeService } from "../services/lastCallTime.service";

@Injectable({ providedIn: 'root' })
export class LastCallTimeInterceptor implements HttpInterceptor {

	constructor(private lastCallService: LastCallTimeService) { }

	intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
		this.lastCallService.updateLastCall();
		return next.handle(req);
	}
}

