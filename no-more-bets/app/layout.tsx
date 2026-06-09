import type { Metadata } from "next";
import { Geist, Geist_Mono, Sixtyfour_Convergence } from "next/font/google";
import { Suspense } from "react";
import { Navbar } from "../components/navbar";
import { SiteFooter } from "../components/site-footer";
import { NavigationRefresh } from "../components/navigation-refresh";
import { TooltipProvider } from "@/components/ui/tooltip";
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

const SITE_TITLE = "No more bets | AI football research";

export const metadata: Metadata = {
  title: {
    absolute: SITE_TITLE,
  },
  description: "Match list and betting information",
  manifest: "/site.webmanifest",
  icons: {
    icon: [
      { url: "/favicon-16x16.png", sizes: "16x16", type: "image/png" },
      { url: "/favicon-32x32.png", sizes: "32x32", type: "image/png" },
    ],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${geistSans.variable} ${geistMono.variable} ${sixtyfourConvergence.variable} antialiased`}
      >
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
