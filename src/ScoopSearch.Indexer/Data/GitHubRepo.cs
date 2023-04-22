﻿using Newtonsoft.Json;

namespace ScoopSearch.Indexer.Data;

public class GitHubRepo
{
    [JsonConstructor]
    private GitHubRepo()
    {
    }

    [JsonProperty("html_url")] public Uri HtmlUri { get; private set; } = null!;

    [JsonProperty("stargazers_count")]
    public int Stars { get; private set; }
}
