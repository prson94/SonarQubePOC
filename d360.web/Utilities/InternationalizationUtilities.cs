using System.Globalization;
using System.Threading;

namespace d360.web.Utilities
{
    public static class InternationalizationUtilities
    {
        const string DefaultLocale = "en-US";

        /// <summary>
        /// Sets the culture and UI culture to a specific culture. Allows overriding of currency
        /// and optionally disallows setting the UI culture.
        /// 
        /// You can also limit the locales that are allowed in order to minimize
        /// resource access for locales that aren't implemented at all.
        /// </summary>
        /// <param name="culture">
        /// 2 or 5 letter ietf string code for the Culture to set. 
        /// Examples: en-US or en</param>
        /// <param name="uiCulture">ietf string code for UiCulture to set</param>
        /// <param name="currencySymbol">Override the currency symbol on the culture</param>
        /// <param name="setUiCulture">
        /// if uiCulture is not set but setUiCulture is true 
        /// it's set to the same as main culture
        /// </param>
        /// <param name="allowedLocales">
        /// Names of 2 or 5 letter ietf locale codes you want to allow
        /// separated by commas. If two letter codes are used any
        /// specific version (ie. en-US, en-GB for en) are accepted.
        /// Any other locales revert to the machine's default locale.
        /// Useful reducing overhead in looking up resource sets that
        /// don't exist and using unsupported culture settings .
        /// Example: de,fr,it,en-US
        /// </param>
        public static void SetUserLocale(string culture = null,
            string uiCulture = null,
            string currencySymbol = null,
            bool setUiCulture = true,
            bool setCulture = true,
            string allowedLocales = null)
        {
            // Use browser detection in ASP.NET
            if (string.IsNullOrEmpty(culture) && System.Web.HttpContext.Current != null)
            {
                System.Web.HttpRequest Request = System.Web.HttpContext.Current.Request;

                // if no user lang leave existing but make writable
                if (Request.UserLanguages == null)
                {
                    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentCulture.Clone() as CultureInfo;
                    if (setUiCulture)
                        Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture.Clone() as CultureInfo;

                    return;
                }

                culture = Request.UserLanguages[0];
            }
            else
            {
                culture = (culture ?? "").ToLower();
            }

            if (!string.IsNullOrEmpty(uiCulture))
            {
                setUiCulture = true;
            }

            if (!string.IsNullOrEmpty(culture) && !string.IsNullOrEmpty(allowedLocales))
            {
                allowedLocales = "," + allowedLocales.ToLower() + ",";
                if (!allowedLocales.Contains("," + culture + ","))
                {
                    int i = culture.IndexOf('-');
                    if (i > 0)
                    {
                        culture = culture.Substring(0, i);
                        if (!allowedLocales.Contains("," + culture + ","))
                        {
                            // Always create writable CultureInfo
                            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentCulture.Clone() as CultureInfo;
                            if (setUiCulture)
                            {
                                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture.Clone() as CultureInfo;
                            }

                            return;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(culture))
            {
                culture = CultureInfo.InstalledUICulture.IetfLanguageTag;
            }

            if (string.IsNullOrEmpty(uiCulture))
            {
                uiCulture = culture;
            }

            try
            {
                CultureInfo Culture = new CultureInfo(culture);

                if (currencySymbol != null && currencySymbol != "")
                {
                    Culture.NumberFormat.CurrencySymbol = currencySymbol;
                }

                if (setCulture)
                {
                    Thread.CurrentThread.CurrentCulture = Culture;
                }

                if (setUiCulture)
                {
                    var UICulture = new CultureInfo(uiCulture);
                    Thread.CurrentThread.CurrentUICulture = UICulture;
                }
            }
            catch
            {
                //surpress exception
            }
        }

        public static string GetUserLocale()
        {
            var localt = Thread.CurrentThread.CurrentUICulture;
            return "fr";
        }
    }
}