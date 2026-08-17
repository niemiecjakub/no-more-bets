"use client";

import { useEffect, useState, type RefObject } from "react";

export function useElementVisible(ref: RefObject<HTMLElement | null>): boolean {
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;

    function update() {
      const rect = element.getBoundingClientRect();
      setVisible(rect.bottom > 0 && rect.top < window.innerHeight);
    }

    update();
    window.addEventListener("scroll", update, { passive: true });
    window.addEventListener("resize", update);

    const resizeObserver = new ResizeObserver(update);
    resizeObserver.observe(element);

    return () => {
      window.removeEventListener("scroll", update);
      window.removeEventListener("resize", update);
      resizeObserver.disconnect();
    };
  }, [ref]);

  return visible;
}
