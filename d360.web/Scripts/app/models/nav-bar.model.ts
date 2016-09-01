export class NavBarItem {
    icon: string;
    name: string;
    route: string;
    expanded = false;
    active = false;
    subItems: NavBarItem[];
    parent: NavBarItem;
    url: string;

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