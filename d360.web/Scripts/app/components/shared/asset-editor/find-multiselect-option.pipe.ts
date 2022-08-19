import { Pipe, PipeTransform } from "@angular/core";

@Pipe({ name: 'findMultiselectOption' })
export class FindMultiselectOptionPipe implements PipeTransform {
	transform(value: any, options: any[]): any {
		return options.find((option) => option.value === value);
	}
}