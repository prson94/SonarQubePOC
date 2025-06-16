import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { SecurityService } from "../services/security";

export const CanLoadAsAuthorized: CanActivateFn = (route, state) => {
  const security = inject(SecurityService);
  const router = inject(Router);

  if (!security.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  return security.isAuthenticated();
};
