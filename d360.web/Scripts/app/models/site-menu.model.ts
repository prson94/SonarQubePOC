export class SiteMenuItem {
    Name: string;
    Url: string;
    Items: SiteMenuItem[];
}

export class SiteMenu {
    MenuID: string;
    NavigationItems: SiteMenuItem[];
    ShouldDisplay: boolean;
    SortOrder: number;
}

export class SiteMenuModel {
    MenuItems: SiteMenu[];
    IsAdmin: boolean = false;
}

export class SiteNav {
    ID: number;
    ParentID: number;
    Name: string;
    Route: string;
    SortOrder: number;
    Object: string;
    ObjectID: number;

    DisplayName: string;
    IsCustom: boolean = false;
}

