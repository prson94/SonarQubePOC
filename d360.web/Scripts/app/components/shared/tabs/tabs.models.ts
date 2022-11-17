export type Tab =  {
    title: string;
    count?: number;
    url: string;
    tag?: string;
    warningMessage?: string;
    subTabsUrl?: string[] ;
    isVisible?: () => boolean;
}
