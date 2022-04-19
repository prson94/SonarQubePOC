import { HttpContextToken } from "@angular/common/http";

/**
 * It says that this query cant' be cancelled when route changes
 * You don't need to put it for non-GET methods unless you put IS_QUERY
 */
export const ROUTE_INDEPENDENT_QUERY = new HttpContextToken<boolean>(() => false);
