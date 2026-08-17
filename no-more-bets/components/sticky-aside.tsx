"use client";

import { useEffect, useRef, type ComponentPropsWithoutRef, type RefObject } from "react";
import { cn } from "@/lib/utils";
import { useStickyWhenFits } from "@/hooks/use-sticky-when-fits";

type StickyAsideProps = ComponentPropsWithoutRef<"aside"> & {
  asideRef?: RefObject<HTMLElement | null>;
  onStickyChange?: (shouldStick: boolean) => void;
};

export function StickyAside({
  className,
  children,
  asideRef,
  onStickyChange,
  ...props
}: StickyAsideProps) {
  const internalRef = useRef<HTMLElement>(null);
  const ref = asideRef ?? internalRef;
  const shouldStick = useStickyWhenFits(ref);

  useEffect(() => {
    onStickyChange?.(shouldStick);
  }, [onStickyChange, shouldStick]);

  return (
    <aside
      ref={ref}
      className={cn(className, shouldStick && "lg:sticky lg:top-20")}
      {...props}
    >
      {children}
    </aside>
  );
}
