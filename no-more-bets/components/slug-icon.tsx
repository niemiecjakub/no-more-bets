"use client";

import { useEffect, useState } from "react";

/** Tried in order; first existing file under /public/clubs or /public/leagues wins. */
const LOGO_EXTENSIONS = ["svg", "png"] as const;

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
  const [extIndex, setExtIndex] = useState(0);

  useEffect(() => {
    setExtIndex(0);
  }, [segment, kind]);

  if (!segment) return null;
  if (extIndex >= LOGO_EXTENSIONS.length) return null;

  const ext = LOGO_EXTENSIONS[extIndex];
  const prefix = kind === "club" ? "/clubs/" : "/leagues/";
  const src = `${prefix}${segment}.${ext}`;

  return (
    <img
      key={src}
      src={src}
      alt={alt}
      className={`shrink-0 object-contain ${className}`}
      onError={() => setExtIndex((i) => i + 1)}
    />
  );
}
