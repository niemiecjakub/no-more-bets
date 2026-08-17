"use client";

import { useLayoutEffect, useState, type RefObject } from "react";

const LG_MEDIA = "(min-width: 1024px)";
const STICKY_TOP_PX = 80; // lg:top-20

function parseLength(value: string): number {
  const trimmed = value.trim();
  if (!trimmed) return 0;
  if (trimmed.endsWith("rem")) return parseFloat(trimmed) * 16;
  if (trimmed.endsWith("px")) return parseFloat(trimmed);
  return parseFloat(trimmed) || 0;
}

function getAvailableStickyHeight(): number {
  const footerHeight = parseLength(
    getComputedStyle(document.documentElement).getPropertyValue("--site-footer-height"),
  );
  return window.innerHeight - STICKY_TOP_PX - footerHeight;
}

export function useStickyWhenFits(ref: RefObject<HTMLElement | null>): boolean {
  const [shouldStick, setShouldStick] = useState(false);

  useLayoutEffect(() => {
    const element = ref.current;
    if (!element) return;

    const lgQuery = window.matchMedia(LG_MEDIA);

    function update() {
      if (!element || !lgQuery.matches) {
        setShouldStick(false);
        return;
      }

      setShouldStick(element.scrollHeight <= getAvailableStickyHeight());
    }

    update();

    const resizeObserver = new ResizeObserver(update);
    resizeObserver.observe(element);
    lgQuery.addEventListener("change", update);
    window.addEventListener("resize", update);

    return () => {
      resizeObserver.disconnect();
      lgQuery.removeEventListener("change", update);
      window.removeEventListener("resize", update);
    };
  }, [ref]);

  return shouldStick;
}
