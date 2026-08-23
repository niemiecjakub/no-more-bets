import type { MetadataRoute } from "next";
import { getSiteUrl } from "@/lib/site";

export default function robots(): MetadataRoute.Robots {
  const sitemap = `${getSiteUrl()}/sitemap.xml`;

  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
      },
      {
        userAgent: [
          "Googlebot",
          "Bingbot",
          "DuckDuckBot",
          "GPTBot",
          "ChatGPT-User",
          "PerplexityBot",
          "ClaudeBot",
          "anthropic-ai",
          "Google-Extended",
        ],
        allow: "/",
      },
      {
        userAgent: ["CCBot", "Bytespider", "Amazonbot", "Applebot-Extended"],
        disallow: "/",
      },
    ],
    sitemap,
    host: getSiteUrl(),
  };
}
