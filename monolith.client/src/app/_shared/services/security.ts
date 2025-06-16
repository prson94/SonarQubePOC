import { HttpClient } from "@angular/common/http";
import { Injectable, signal } from "@angular/core";
import { CurrentUser } from "../models/CurrentUser";

@Injectable({
  providedIn: "root"
})
export class SecurityService {
  private CURRENT_USER_KEY: string = "currentUser";
  private CURRENT_ID_TOKEN: string = "idToken";
  private CURRENT_REFRESH_TOKEN: string = "refreshToken";

  private currentUserSignal = signal<CurrentUser | null>(null);
  currentUser = this.currentUserSignal.asReadonly();

  // cookieResource = resource({
  //     loader: async (params) => {
  //         const response = fetch(`${this.BaseApi}/authorization/cookies?state=${state}`);
  //         return response.then(
  //             (res) => res.json() as Promise<DatasetCardModel[]>
  //         );
  //     }
  // });

  constructor(private http: HttpClient) { }

  isAuthenticated(): boolean {
    //const user = this.getCurrentUser();
    //if (user) {
    //  return (user.exp > this.getSecondsSinceEpoch());
    //} 
    //else {
    //  return false;
    //}
    return true;
  }

  storeUserToken(token: string) {
    window.sessionStorage.setItem(this.CURRENT_ID_TOKEN, token);
  }

  getCurrentUserToken(): string | null {
    const token = window.sessionStorage.getItem(this.CURRENT_ID_TOKEN);
    return token;
  }

  loginWithForm(email: string, password: string) {
    return fetch(`/authorization/forms-login`, {
      method: 'POST',
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        Username: email,
        Password: password
      })
    }).then(
      async (response) => {
        if (response.ok) {
          const userJson = await response.json();
          //this.tokens.storeUserToken(userJson.token);
          //this.tokens.storeRefreshToken(userJson.refreshToken);
        }
        return response.ok;
      }
    );
  }

  async loginWithIdp(state: string): Promise<boolean> {
    const response = await fetch(`/authorization/idp-login?state=${state}`);
    if (response.ok) {
      const userJson = await response.json();
      this.storeUserToken(JSON.stringify(userJson));
    }
    return response.ok;
  }
}
