import type { Metadata } from "next";
import { Geist, Geist_Mono, Sixtyfour_Convergence } from "next/font/google";
import { Suspense } from "react";
import { Navbar } from "../components/navbar";
import { SiteFooter } from "../components/site-footer";
import { NavigationRefresh } from "../components/navigation-refresh";
import { TooltipProvider } from "@/components/ui/tooltip";
import { JsonLd } from "@/components/json-ld";
import { organizationNode, websiteNode } from "@/lib/schema";
import { DEFAULT_DESCRIPTION, DEFAULT_TITLE, getSiteUrl, SITE_NAME } from "@/lib/site";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

const sixtyfourConvergence = Sixtyfour_Convergence({
  variable: "--font-sixtyfour-convergence",
  subsets: ["latin"],
  weight: "400",
});

export const metadata: Metadata = {
  metadataBase: new URL(getSiteUrl()),
  title: {
    default: DEFAULT_TITLE,
    template: `%s | ${SITE_NAME}`,
  },
  description: DEFAULT_DESCRIPTION,
  applicationName: SITE_NAME,
  robots: { index: true, follow: true },
  openGraph: {
    type: "website",
    locale: "en_GB",
    siteName: SITE_NAME,
    title: DEFAULT_TITLE,
    description: DEFAULT_DESCRIPTION,
    url: "/",
  },
  twitter: {
    card: "summary_large_image",
    title: DEFAULT_TITLE,
    description: DEFAULT_DESCRIPTION,
    site: "@nomorebetsai",
  },
  manifest: "/site.webmanifest",
  icons: {
    icon: [
      { url: "/favicon-16x16.png", sizes: "16x16", type: "image/png" },
      { url: "/favicon-32x32.png", sizes: "32x32", type: "image/png" },
      { url: "/android-chrome-192x192.png", sizes: "192x192", type: "image/png" },
    ],
    apple: [{ url: "/apple-icon.png", sizes: "180x180" }],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <head>
        <link rel="describedby" href="/llms.txt" />
      </head>
      <body
        className={`${geistSans.variable} ${geistMono.variable} ${sixtyfourConvergence.variable} antialiased`}
      >
        <JsonLd data={{ "@context": "https://schema.org", "@graph": [organizationNode(), websiteNode()] }} />
        <TooltipProvider>
          <div className="flex min-h-dvh flex-col pb-[var(--site-footer-height)]">
            <NavigationRefresh />
            <Suspense fallback={null}>
              <Navbar />
            </Suspense>
            <div className="flex min-h-0 flex-1 flex-col bg-zinc-50 dark:bg-zinc-950">
              {children}
            </div>
            <SiteFooter />
          </div>
        </TooltipProvider>
      </body>
    </html>
  );
}
