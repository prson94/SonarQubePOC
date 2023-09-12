import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'assetTypeClassDisplay' })
export class AssetTypeClassDisplayPipe implements PipeTransform {
	transform(className: string): string {
		if (className === "Reference") return "Reference List";
        return `${className.replace(/Asset/g, '').trim()} Asset Type`;
    }
}