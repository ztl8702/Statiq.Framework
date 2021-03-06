﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Statiq.Common;

namespace Statiq.Core
{
    /// <summary>
    /// Embeds a Giphy gif.
    /// </summary>
    /// <remarks>
    /// You only need the ID of the gif which can be obtained from it's URL:
    /// <code>
    /// https://giphy.com/gifs/excited-birthday-yeah-yoJC2GnSClbPOkV0eA
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;?# Giphy excited-birthday-yeah-yoJC2GnSClbPOkV0eA /?&gt;
    /// </code>
    /// </example>
    /// <parameter>The ID of the gif.</parameter>
    public class GiphyShortcode : EmbedShortcode
    {
        public override async Task<IDocument> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context) =>
            await ExecuteAsync("https://giphy.com/services/oembed", $"https://giphy.com/gifs/{args.SingleValue()}", context);
    }
}
