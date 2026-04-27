"use client";

import {
  clubLogoExtBySlug,
  leagueLogoExtBySlug,
} from "@/lib/logo-manifest.gen";

type SlugIconProps = {
  kind: "club" | "league";
  slug: string | null | undefined;
  alt: string;
  className?: string;
};

export function SlugIcon({
  kind,
  slug,
  alt,
  className = "h-6 w-6",
}: SlugIconProps) {
  const segment = (slug ?? "").trim().toLowerCase();
  if (!segment) return null;

  const ext =
    kind === "club"
      ? clubLogoExtBySlug[segment]
      : leagueLogoExtBySlug[segment];
  if (!ext) return null;

  const prefix = kind === "club" ? "/clubs/" : "/leagues/";
  const src = `${prefix}${segment}.${ext}`;

  return (
    <img
      src={src}
      alt={alt}
      className={`shrink-0 object-contain ${className}`}
    />
  );
}
