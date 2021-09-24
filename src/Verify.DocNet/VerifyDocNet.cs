using System;
using System.Collections.Generic;
using Docnet.Core.Models;
using Docnet.Core.Readers;

namespace VerifyTests
{
    public static partial class VerifyDocNet
    {
        public static void Initialize()
        {
            VerifierSettings.RegisterFileConverter("pdf", Convert);
            VerifierSettings.RegisterFileConverter<IDocReader>(Convert);
        }

        public static void PagesToInclude(this VerifySettings settings, int count)
        {
            settings.Context["VerifyDocNetPagesToInclude"] = count;
        }

        public static SettingsTask PagesToInclude(this SettingsTask settings, int count)
        {
            settings.CurrentSettings.PagesToInclude(count);
            return settings;
        }

        static int GetPagesToInclude(this IReadOnlyDictionary<string, object> settings, int count)
        {
            if (settings.TryGetValue("VerifyDocNetPagesToInclude", out var value))
            {
                return Math.Min(count, (int)value);
            }

            return count;
        }

        public static void PageDimensions(this VerifySettings settings, PageDimensions pageDimensions)
        {
            settings.Context["VerifyDocNetPageDimensions"] = pageDimensions;
        }

        public static SettingsTask PageDimensions(this SettingsTask settings, PageDimensions pageDimensions)
        {
            settings.CurrentSettings.PageDimensions(pageDimensions);
            return settings;
        }

        static PageDimensions GetPageDimensions(this IReadOnlyDictionary<string, object> settings, PageDimensions pageDimensions)
        {
            if (settings.TryGetValue("VerifyDocNetPageDimensions", out var value))
            {
                return (PageDimensions)value;
            }

            return pageDimensions;
        }

        public static void PreserveTransparency(this VerifySettings settings)
        {
            settings.Context["VerifyDocNetPreserveTransparency"] = true;
        }

        public static SettingsTask PreserveTransparency(this SettingsTask settings)
        {
            settings.CurrentSettings.PreserveTransparency();
            return settings;
        }

        static bool GetPreserveTransparency(this IReadOnlyDictionary<string, object> settings, bool preserveTransparency)
        {
            if (settings.TryGetValue("VerifyDocNetPreserveTransparency", out var value))
            {
                return (bool)value;
            }

            return preserveTransparency;
        }
    }
}