export class NavBarItem {
    icon: string;
    name: string;
    route: string;
    expanded = false;
    active = false;
    subItems: NavBarItem[];
    parent: NavBarItem;
    url: string;
    sortorder: number = 999;

    public isRootItem(): boolean {
        return this.parent == undefined;
    }
}

export enum NavBarMode {
    Default,
    Favorites,
    EditFavorites,
    AdminFavorites,
    EditAdminFavorites,
    Admin,
}