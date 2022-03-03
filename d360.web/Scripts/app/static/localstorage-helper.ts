import { LocalStorageKey } from "../enums/localstorage.enum";

export class LocalStorageHelper {
    static isLocalStorageKeyExist(localStorageKey: LocalStorageKey): boolean {
        return localStorage.getItem(localStorageKey) !== null;
    }
}
