import { absoluteUrl, DEFAULT_DESCRIPTION, SITE_NAME, SITE_TAGLINE } from "@/lib/site";

export function organizationNode() {
  return {
    "@type": "Organization",
    "@id": `${absoluteUrl("/")}#organization`,
    name: SITE_NAME,
    alternateName: ["nomorebets.io", "Chandler"],
    url: absoluteUrl("/"),
    logo: absoluteUrl("/logo.png"),
    description: DEFAULT_DESCRIPTION,
    sameAs: [
      "https://github.com/niemiecjakub/no-more-bets",
      "https://x.com/nomorebetsai",
    ],
  };
}

export function websiteNode() {
  return {
    "@type": "WebSite",
    "@id": `${absoluteUrl("/")}#website`,
    name: `${SITE_NAME} — ${SITE_TAGLINE}`,
    url: absoluteUrl("/"),
    description: DEFAULT_DESCRIPTION,
    inLanguage: "en",
    publisher: { "@id": `${absoluteUrl("/")}#organization` },
  };
}

export function softwareApplicationNode(url: string) {
  return {
    "@type": "SoftwareApplication",
    name: `${SITE_NAME} — ${SITE_TAGLINE}`,
    applicationCategory: "SportsApplication",
    operatingSystem: "Web",
    url,
    description: DEFAULT_DESCRIPTION,
    offers: {
      "@type": "Offer",
      price: "0",
      priceCurrency: "USD",
    },
  };
}

export function breadcrumbList(items: { name: string; path: string }[]) {
  return {
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: absoluteUrl(item.path),
    })),
  };
}
