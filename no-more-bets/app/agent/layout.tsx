import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Agent",
};

export default function AgentLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return children;
}
