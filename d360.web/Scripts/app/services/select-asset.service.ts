import { Injectable } from '@angular/core';
import { forkJoin } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SelectAssetService {

  constructor() { }

  selectAsset(event: any, context: any) {
    context.selectedAsset = context.selectedReferenceItem = context.selectedTag = null;
    context.selection = event;

    if (context.selection && context.selection.HasProfiling) {
      context.sidePanelLoading = true;
      context.dataProfileService.getDataProfiles(context.selection.AssetUid).subscribe(
        (r) => {
          if (r && r.items && r.items.length > 0) {
            context.dataProfile = r.items[0];
            forkJoin(
              context.dataProfileService.getMatchCounts(context.dataProfile.assetUid, 'Structure'),
              context.dataProfileService.getMatchCounts(context.dataProfile.assetUid, 'Data')
            ).subscribe((res) => {
              context.dataProfile['matches'] = {
                structure: res[0],
                data: res[1]
              };
            });
          }
          context.sidePanelLoading = false;
        });
    }
  }
}
