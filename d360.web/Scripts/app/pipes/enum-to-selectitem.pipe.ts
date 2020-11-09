import { Pipe, PipeTransform } from "@angular/core";

@Pipe({ name: 'enumToSelectitem' })
export class EnumToSelectitemPipe implements PipeTransform {
    transform(value): Object {
        return Object.keys(value).filter(e => !isNaN(+e)).map(o => { return { value: +o, label: value[o] } });
    }
}
