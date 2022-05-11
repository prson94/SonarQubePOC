import { TemplateRef } from "@angular/core";

export interface DropdownBadgeOption<T> {
    template: TemplateRef<any>;
    custom: boolean;
    label: string;
    value: T;
    disabled: boolean;
} 