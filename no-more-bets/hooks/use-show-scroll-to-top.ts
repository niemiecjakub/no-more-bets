"use client";

import { useEffect, useState } from "react";

const LG_MEDIA = "(min-width: 1024px)";
const SCROLL_THRESHOLD_PX = 240;

interface SidebarScrollState {
  filtersSticky: boolean;
  filtersVisible: boolean;
  researchSticky: boolean;
  researchVisible: boolean;
}

export function useShowScrollToTop({
  filtersSticky,
  filtersVisible,
  researchSticky,
  researchVisible,
}: SidebarScrollState): boolean {
  const [isDesktop, setIsDesktop] = useState(false);
  const [isScrolled, setIsScrolled] = useState(false);

  useEffect(() => {
    const lgQuery = window.matchMedia(LG_MEDIA);

    function updateDesktop() {
      setIsDesktop(lgQuery.matches);
    }

    function updateScroll() {
      setIsScrolled(window.scrollY > SCROLL_THRESHOLD_PX);
    }

    updateDesktop();
    updateScroll();

    lgQuery.addEventListener("change", updateDesktop);
    window.addEventListener("scroll", updateScroll, { passive: true });

    return () => {
      lgQuery.removeEventListener("change", updateDesktop);
      window.removeEventListener("scroll", updateScroll);
    };
  }, []);

  const sidebarNeedsScrollTop =
    !filtersSticky || !filtersVisible || !researchSticky || !researchVisible;

  return isDesktop && isScrolled && sidebarNeedsScrollTop;
}
