"use client";

import { useState } from "react";

type SlugIconProps = {
  kind: "club" | "league";
  slug: string;
  alt: string;
  className?: string;
};

export function SlugIcon({
  kind,
  slug,
  alt,
  className = "h-6 w-6",
}: SlugIconProps) {
  const [hidden, setHidden] = useState(false);
  if (hidden || !slug) return null;

  const src =
    kind === "club" ? `/clubs/${slug}.svg` : `/leagues/${slug}.svg`;

  return (
    <img
      src={src}
      alt={alt}
      className={`shrink-0 object-contain ${className}`}
      onError={() => setHidden(true)}
    />
  );
}
