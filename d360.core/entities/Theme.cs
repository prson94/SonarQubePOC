using d360.core.exceptions;
using d360.core.resources;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    public abstract class ThemeBase : BaseObject
    {
        [JsonProperty("backColor"), StringLength(7)]
        public string BackColor { get; set; }

        [JsonProperty("breadcrumbLinkColor"), StringLength(7)]
        public string BreadcrumbLinkColor { get; set; }

        [JsonProperty("buttonBackColor"), StringLength(7)]
        public string ButtonBackColor { get; set; }

        [JsonProperty("headerBackColor"), StringLength(7)]
        public string HeaderBackColor { get; set; }

        [JsonProperty("isCurrent")]
        public bool IsCurrent { get; set; }

        [JsonProperty("name"), Column(TypeName = "nvarchar")]
        public string Name { get; set; }

        [JsonProperty("navbarBackColor"), StringLength(7)]
        public string NavBarBackColor { get; set; }

        [JsonProperty("navbarBackColorSelected"), StringLength(7)]
        public string NavBarBackSelectedColor { get; set; }

        [JsonProperty("primaryButtonBackColor"), StringLength(7)]
        public string PrimaryButtonBackColor { get; set; }

        [JsonProperty("tableHeaderBackColor"), StringLength(7)]
        public string TableHeaderBackColor { get; set; }

        [JsonProperty("tableRowBackColor"), StringLength(7)]
        public string TableRowBackSelectedColor { get; set; }

        [JsonProperty("tabLinkColor"), StringLength(7)]
        public string TabLinkColor { get; set; }
    }

    public class GetTheme : ThemeBase
    {
        [JsonProperty("uid")]
        public Guid Uid { get; set; }

        [JsonProperty("customCssUri")]
        public string CustomCssUri { get; set; }
        [JsonProperty("customCss")]
        public string CustomCss { get; set; }

        [JsonProperty("headerLogoUri")]
        public string HeaderLogoUri { get; set; }

        [JsonProperty("homeBackgroundUri")]
        public string HomeBackgroundUri { get; set; }

        [JsonProperty("iconUri")]
        public string IconUri { get; set; }

        [JsonProperty("createdBy")]
        public GetUserModel CreatedBy { get; set; }

        [JsonProperty("createdOn")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("updatedBy")]
        public GetUserModel UpdatedBy { get; set; }

        [JsonProperty("updatedOn")]
        public DateTime UpdatedOn { get; set; }
    }

    public class ThemeBaseEdit : ThemeBase
    {
        [JsonProperty("customCss")]
        public string CustomCss { get; set; }

        [JsonProperty("headerLogo")]
        public string HeaderLogo { get; set; }

        [JsonProperty("homeBackground")]
        public string HomeBackground { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class PostTheme : ThemeBaseEdit
    {

    }

    public class PutTheme : ThemeBaseEdit
    {

    }

    public class ThemeBase64Data
    {
        [JsonProperty("customCss")]
        public string CustomCss { get; set; }

        [JsonProperty("headerLogo")]
        public string HeaderLogo { get; set; }

        [JsonProperty("homeBackground")]
        public string HomeBackground { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class Theme : ThemeBase
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string BrowserIconExtension { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public string CustomCss { get; set; }

        public string HeaderLogoExtension { get; set; }

        public string HomePageBackgroundExtension { get; set; }

        public Guid Uid { get; set; }

        public int UpdatedBy { get; set; }

        public DateTime UpdatedOn { get; set; }

        public bool Locked { get; set; }

        #region Transitive Properties

        [NotMapped]
        public byte[] BrowserIcon { get; set; }

        [NotMapped]
        public byte[] HeaderLogo { get; set; }

        [NotMapped]
        public byte[] HomePageBackground { get; set; }

        #endregion
    }

    #region Extensions

    public static class ThemeExtensions
    {
        private static string ParseBase64CustomCss(this string css)
        {
            if (!string.IsNullOrEmpty(css))
            {
                var remainder = css.Length % 4;
                if (remainder != 0)
                {
                    while (remainder > 0)
                    {
                        css += "=";
                        remainder -= 1;
                    }
                }
                var cssBytes = Convert.FromBase64String(css);
                css = System.Text.Encoding.UTF8.GetString(cssBytes);
            }
            else
            {
                css = null;
            }

            return css;
        }

        public static GetTheme ToGetModel(this Theme model, Uri baseUri, GlobalReportingResource createdBy, GlobalReportingResource updatedBy, int environmentId)
        {
            return new GetTheme
            {
                CreatedBy = new GetUserModel
                {
                    FullName = createdBy.FullName,
                    Uid = createdBy.Uid
                },
                CreatedOn = model.CreatedOn,
                Name = model.Name,
                CustomCssUri = string.IsNullOrEmpty(model.CustomCss) ?
                    null :
                    $"/api/v2/environment/themes/{model.Uid}/custom.css",
                CustomCss = model.CustomCss,
                BackColor = model.BackColor,
                BreadcrumbLinkColor = model.BreadcrumbLinkColor,
                ButtonBackColor = model.ButtonBackColor,
                HeaderBackColor = model.HeaderBackColor,
                HeaderLogoUri = string.IsNullOrEmpty(model.HeaderLogoExtension) ? null : $"{baseUri.AbsoluteUri}/{environmentId}/{model.Uid}_logo{model.HeaderLogoExtension}",
                HomeBackgroundUri = string.IsNullOrEmpty(model.HomePageBackgroundExtension) ? null : $"{baseUri.AbsoluteUri}/{environmentId}/{model.Uid}_background{model.HomePageBackgroundExtension}",
                IconUri = string.IsNullOrEmpty(model.BrowserIconExtension) ? null : $"{baseUri.AbsoluteUri}/{environmentId}/{model.Uid}_icon{model.BrowserIconExtension}",
                IsCurrent = model.IsCurrent,
                NavBarBackColor = model.NavBarBackColor,
                NavBarBackSelectedColor = model.NavBarBackSelectedColor,
                PrimaryButtonBackColor = model.PrimaryButtonBackColor,
                TableHeaderBackColor = model.TableHeaderBackColor,
                TableRowBackSelectedColor = model.TableRowBackSelectedColor,
                TabLinkColor = model.TabLinkColor,
                Uid = model.Uid,
                UpdatedBy = new GetUserModel
                {
                    FullName = updatedBy.FullName,
                    Uid = updatedBy.Uid
                },
                UpdatedOn = model.UpdatedOn
            };
        }

        public static Theme ToRepositoryModel(this PostTheme model, int resourceId)
        {
            var date = DateTime.UtcNow;

            var repoModel = new Theme
            {
                BackColor = model.BackColor,
                BreadcrumbLinkColor = model.BreadcrumbLinkColor,
                ButtonBackColor = model.ButtonBackColor,
                CreatedBy = resourceId,
                CreatedOn = date,
                HeaderBackColor = model.HeaderBackColor,
                IsCurrent = model.IsCurrent,
                Locked = false,
                NavBarBackColor = model.NavBarBackColor,
                NavBarBackSelectedColor = model.NavBarBackSelectedColor,
                PrimaryButtonBackColor = model.PrimaryButtonBackColor,
                TableHeaderBackColor = model.TableHeaderBackColor,
                TableRowBackSelectedColor = model.TableRowBackSelectedColor,
                TabLinkColor = model.TabLinkColor,
                Name = model.Name,
                Uid = Guid.NewGuid(),
                UpdatedBy = resourceId,
                UpdatedOn = date
            };
            repoModel.CustomCss = model.CustomCss.ParseBase64CustomCss();

            if (model.Icon != null)
            {
                var browserIcon = model.Icon.GetFileFromDataUrl();
                repoModel.BrowserIconExtension = browserIcon.Item1;
                repoModel.BrowserIcon = browserIcon.Item2.ToArray();
            }

            if (model.HeaderLogo != null)
            {
                var headerLogo = model.HeaderLogo.GetFileFromDataUrl();
                repoModel.HeaderLogoExtension = headerLogo.Item1;
                repoModel.HeaderLogo = headerLogo.Item2.ToArray();
            }

            if (model.HomeBackground != null)
            {
                var homePageBackground = model.HomeBackground.GetFileFromDataUrl();
                repoModel.HomePageBackgroundExtension = homePageBackground.Item1;
                repoModel.HomePageBackground = homePageBackground.Item2.ToArray();
            }

            return repoModel;
        }

        public static Theme ToRepositoryModel(this PutTheme model, Theme existing, int resourceId)
        {
            var date = DateTime.UtcNow;

            existing.BackColor = model.BackColor;
            existing.CustomCss = model.CustomCss.ParseBase64CustomCss();
            existing.Name = model.Name;
            existing.IsCurrent = model.IsCurrent;
            existing.HeaderBackColor = model.HeaderBackColor;
            existing.BreadcrumbLinkColor = model.BreadcrumbLinkColor;
            existing.ButtonBackColor = model.ButtonBackColor;
            existing.NavBarBackColor = model.NavBarBackColor;
            existing.NavBarBackSelectedColor = model.NavBarBackSelectedColor;
            existing.PrimaryButtonBackColor = model.PrimaryButtonBackColor;
            existing.TableHeaderBackColor = model.TableHeaderBackColor;
            existing.TableRowBackSelectedColor = model.TableRowBackSelectedColor;
            existing.TabLinkColor = model.TabLinkColor;
            existing.UpdatedBy = resourceId;
            existing.UpdatedOn = date;

            if (model.Icon != null)
            {
                var browserIcon = model.Icon.GetFileFromDataUrl();
                existing.BrowserIconExtension = browserIcon.Item1;
                existing.BrowserIcon = browserIcon.Item2.ToArray();
            }

            if (model.HeaderLogo != null)
            {
                var headerLogo = model.HeaderLogo.GetFileFromDataUrl();
                existing.HeaderLogoExtension = headerLogo.Item1;
                existing.HeaderLogo = headerLogo.Item2.ToArray();
            }

            if (model.HomeBackground != null)
            {
                var homePageBackground = model.HomeBackground.GetFileFromDataUrl();
                existing.HomePageBackgroundExtension = homePageBackground.Item1;
                existing.HomePageBackground = homePageBackground.Item2.ToArray();
            }

            return existing;
        }

        public static void Validate(this Theme model)
        {
            var errors = new List<string>();

            model.Name = (model.Name + "").Trim();
            if (string.IsNullOrEmpty(model.Name))
            {
                errors.Add(ThemeErrors.NameNotEmpty);
            }

            if (model.BrowserIcon != null && model.BrowserIcon.Length > 512 * 1000)
            {
                errors.Add(ThemeErrors.IconSize);
            }

            if (model.BrowserIconExtension != null && model.BrowserIconExtension != ".ico" && model.BrowserIconExtension != ".png")
            {
                errors.Add(ThemeErrors.IconType);
            }

            if (model.HeaderLogo != null && model.HeaderLogo.Length > 1024 * 1000)
            {
                errors.Add(ThemeErrors.LogoSize);
            }

            if (model.HeaderLogoExtension != null &&
                model.HeaderLogoExtension != ".gif" && model.HeaderLogoExtension != ".jpg" && model.HeaderLogoExtension != ".png")
            {
                errors.Add(ThemeErrors.LogoType);
            }

            if (model.HomePageBackground != null && model.HomePageBackground.Length > 2048 * 1000)
            {
                errors.Add(ThemeErrors.BackgroundSize);
            }

            if (model.HomePageBackgroundExtension != null &&
                model.HomePageBackgroundExtension != ".gif" && model.HomePageBackgroundExtension != ".jpg" && model.HomePageBackgroundExtension != ".png")
            {
                errors.Add(ThemeErrors.BackgroundType);
            }

            if (!model.BackColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.BackColorFormat);
            }

            if (!model.BreadcrumbLinkColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.BreadcrumbColorFormat);
            }

            if (!model.ButtonBackColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.ButtonColorFormat);
            }

            if (!model.HeaderBackColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.HeaderColorFormat);
            }

            if (!model.NavBarBackColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.NavbarColorFormat);
            }

            if (!model.NavBarBackSelectedColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.NavbarSelectedColorFormat);
            }

            if (!model.PrimaryButtonBackColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.PrimaryButtonColorFormat);
            }

            if (!model.TableHeaderBackColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.TableHeaderColorFormat);
            }

            if (!model.TableRowBackSelectedColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.TableRowColorFormat);
            }

            if (!model.TabLinkColor.IsValidRgb())
            {
                errors.Add(ThemeErrors.TabLinkColorFormat);
            }

            // Determine if we should throw an error.
            if (errors.Count > 0)
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, ThemeErrors.ThemeInvalid, string.Join("; ", errors));
            }
        }
    }

    #endregion
}
