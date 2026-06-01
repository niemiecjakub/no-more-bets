import type { Metadata } from "next";
import HomePage from "./_components/home-page";

export const metadata: Metadata = {
  title: "Matches",
};

export default function Page() {
  return <HomePage />;
}
