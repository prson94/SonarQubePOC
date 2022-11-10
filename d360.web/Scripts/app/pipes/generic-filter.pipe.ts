import { Pipe, PipeTransform } from '@angular/core';

// provide the array and the callback function to filter to use this when filtering ngFor loops
// callback can be implemented in the component to be object independant
// To use values from the controller make sure to use arrow functions so you can access scope from the component

@Pipe({
    name: 'genericFilter',
    pure: false
})
export class GenericFilter implements PipeTransform {
    transform(items: any[], callback: (item: any) => boolean): any {
        if (!items || !callback) {
            return items;
        }
        return items.filter((item) => callback(item));
    }
}