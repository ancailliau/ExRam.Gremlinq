﻿using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VerifyXunit;

using Xunit;

namespace ExRam.Gremlinq.Core.Tests
{
    public static class GremlinQueryExtensions
    {
        private static readonly Regex IdRegex1 = new Regex("(\"id\"\\s*[:,]\\s*{\\s*\"@type\"\\s*:\\s*\"g:Int64\"\\s*,\\s*\"@value\":\\s*)([^\\s{}]+)(\\s*})", RegexOptions.IgnoreCase);
        private static readonly Regex IdRegex2 = new Regex("\"[0-9a-f]{8}[-]?([0-9a-f]{4}[-]?){3}[0-9a-f]{12}([|]PartitionKey)?\"", RegexOptions.IgnoreCase);

        public static async Task Verify<TElement>(this IGremlinQueryBase<TElement> query, VerifyBase contextBase)
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
                var data = JsonConvert.SerializeObject(
                    await query
                        .ToArrayAsync(),
                    Formatting.Indented);

                var serialized = contextBase is QueryIntegrationTest
                    ? IdRegex2.Replace(
                        IdRegex1.Replace(
                            data,
                            "$1\"scrubbed id\"$3"),
                        "\"scrubbed id\"")
                    : data;

                await contextBase.Verify(
                    serialized);
            }
        }
    }
}
