import { HttpContextToken, HttpRequest } from "@angular/common/http";


/**
 * In case if IS_QUERY on http request is set to true, we consider it as GET-request
 * I.e. it's safe for cancellation/sharing etc
 */
export const IS_QUERY = new HttpContextToken<boolean>(() => false);

/**
 * Determins if we want to consider request as query, 
 *  i.e as something that doesn't have side-effects, 
 *  i.e. it's safe for cancellation/sharing etc
 */
export function isQueryRequest(req: HttpRequest<any>) {
    return (req.method === 'GET') || (req.context.get(IS_QUERY) === true);
}