import { LocalStorageKey } from "../enums/general.enum";

export class LocalStorageHelper {
    static isLocalStorageKeyExist(localStorageKey: LocalStorageKey): boolean {
        return localStorage.getItem(localStorageKey) !== null;
    }
}