using System;
using System.Collections.Generic;
using System.Text;

namespace d360.core.helpers
{
    public static class IconHelper
    {
        /// <summary>
        /// Generates the icon text shown on icons that represent the Asset 
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GetIconText(string assetName)
        {
            string iconText = "Tx";
            if (string.IsNullOrEmpty(assetName))
            {
                return iconText;
            }

            var name = assetName.Trim();

            var words = name.Split(' ');
            if (words.Length > 1 && words[1].Length > 0)
            {
                if (!string.IsNullOrEmpty(words[0]))
                {
                    iconText = words[0][0].ToString().ToUpper();
                }
                
                if (!string.IsNullOrEmpty(words[1]))
                {

                    iconText += words[1][0].ToString().ToLower();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(name))
                {
                    iconText = name[0].ToString().ToUpper();
                    if (name.Length > 1)
                    {
                        iconText += name[1].ToString().ToLower();
                    }
                }
            }

            return iconText;
        }
    }
}
