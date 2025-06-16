import { HttpEvent, HttpHandlerFn, HttpRequest } from "@angular/common/http";
import { Observable } from "rxjs";
import { inject } from "@angular/core";
import { SecurityService } from "../services/security";

export function authInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
  const security = inject(SecurityService);
  const token = security.getCurrentUserToken();
  req = req.clone({
    //withCredentials: true
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
  return next(req);
}
