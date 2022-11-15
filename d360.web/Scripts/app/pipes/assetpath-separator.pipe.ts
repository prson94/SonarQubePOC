import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'assetpathSeparator' })
export class AssetpathSeparatorPipe implements PipeTransform {
    transform(path: string[][], keyseparator: string = '<span class="assetkeyseparator">/</span>', pathseparator: string = '<i class="fa fa-angle-right assetpathseparator"></i>'): string {
        return path.map((p) => p.join(keyseparator)).join(pathseparator);
    }
}