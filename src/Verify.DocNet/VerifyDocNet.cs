using System;
using System.Collections.Generic;
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
            if (!settings.TryGetValue("VerifyDocNetPagesToInclude", out var value))
            {
                return count;
            }

            return Math.Min(count, (int) value);
        }
    }
}