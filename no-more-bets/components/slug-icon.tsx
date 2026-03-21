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
  const segment = slug.trim().toLowerCase();
  if (hidden || !segment) return null;

  const src =
    kind === "club" ? `/clubs/${segment}.svg` : `/leagues/${segment}.svg`;

  return (
    <img
      src={src}
      alt={alt}
      className={`shrink-0 object-contain ${className}`}
      onError={() => setHidden(true)}
    />
  );
}
