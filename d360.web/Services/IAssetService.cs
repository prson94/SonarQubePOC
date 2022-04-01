using d360.core.entities;

namespace d360.web.Services
{
    /// <summary>
    /// This is internal service with logic which should not be used by controllers directly.
    /// It should contains shared business logic related to asset
    /// </summary>
    internal interface IAssetService
    {
        string GetAssetName(AssetType assetType);
    }
}
