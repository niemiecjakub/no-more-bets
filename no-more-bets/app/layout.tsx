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

export const metadata: Metadata = {
  title: "No More Bets",
  description: "Match list and betting information",
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
            <div className="flex-1">{children}</div>
            <SiteFooter />
          </div>
        </TooltipProvider>
      </body>
    </html>
  );
}
