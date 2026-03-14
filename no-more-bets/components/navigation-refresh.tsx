"use client";

import { usePathname } from "next/navigation";
import { useRouter } from "next/navigation";
import { useEffect, useRef } from "react";

/**
 * On client-side navigation, the Router Cache can show a stale RSC payload.
 * Calling router.refresh() when the pathname changes forces a fresh server
 * round-trip for the current route, so all pages get up-to-date data without
 * a full reload.
 */
export function NavigationRefresh() {
  const pathname = usePathname();
  const router = useRouter();
  const isFirstMount = useRef(true);

  useEffect(() => {
    if (isFirstMount.current) {
      isFirstMount.current = false;
      return;
    }
    router.refresh();
  }, [pathname, router]);

  return null;
}
