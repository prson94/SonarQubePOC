import { CanLoad, Route, UrlSegment } from "@angular/router";
import { Injectable } from "@angular/core";
import { Observable } from 'rxjs';
import { SecurityService } from "../services/security";

@Injectable()
export class CanLoadAsAdmin implements CanLoad {
  constructor(private security: SecurityService) { }

  canLoad(route: Route, segments: UrlSegment[]): Observable<boolean> | Promise<boolean> | boolean {
    return true;//this.security.isAdmin();
  }
}
