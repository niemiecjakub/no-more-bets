"use client";

import { useEffect, useState } from "react";

const TOP_THRESHOLD_PX = 8;
const DIRECTION_DELTA_PX = 10;

export function useRevealOnScrollUp() {
  const [isVisible, setIsVisible] = useState(true);

  useEffect(() => {
    let lastScrollY = window.scrollY;

    function onScroll() {
      const scrollY = window.scrollY;

      if (scrollY <= TOP_THRESHOLD_PX) {
        setIsVisible(true);
      } else if (scrollY > lastScrollY + DIRECTION_DELTA_PX) {
        setIsVisible(false);
      } else if (scrollY < lastScrollY - DIRECTION_DELTA_PX) {
        setIsVisible(true);
      }

      lastScrollY = scrollY;
    }

    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return isVisible;
}
