import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'assetTypeClassDisplay' })
export class AssetTypeClassDisplayPipe implements PipeTransform {
    transform(className: string): string {
        return `${className.replace(/Asset/g, '').trim()} Asset Type`;
    }
}