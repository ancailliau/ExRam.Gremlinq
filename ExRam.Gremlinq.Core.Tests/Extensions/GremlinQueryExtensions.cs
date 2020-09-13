﻿using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace ExRam.Gremlinq.Core.Tests
{
    public static class GremlinQueryExtensions
    {
        private static readonly VerifySettings Settings = new VerifySettings();

        static GremlinQueryExtensions()
        {
            Settings.UseExtension("json");
            Settings.DisableDiff();

#if (DEBUG)
            Settings.AutoVerify();
#endif
        }

        public static async Task Verify<TElement>(this IGremlinQueryBase<TElement> query, XunitContextBase contextBase)
        {
            if (contextBase is QuerySerializationTest && typeof(TElement) != typeof(object))
            {
                await query.Cast<object>().Verify(contextBase);
            }
            else if (contextBase is QueryIntegrationTest && typeof(TElement) != typeof(JToken))
            {
                await query.Cast<JToken>().Verify(contextBase);
            }
            else
            {
                var data = await query
                    .ToArrayAsync();

                await Verifier.Verify(
                    JsonConvert.SerializeObject(
                        data,
                        Formatting.Indented),
                    Settings, contextBase.SourceFile);
            }
        }
    }
}
