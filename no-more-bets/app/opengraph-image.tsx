import { ImageResponse } from "next/og";

export const alt = "No More Bets — AI football research agent";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function OpenGraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          background: "#0a0a0a",
          color: "#fafafa",
          padding: "72px",
        }}
      >
        <div
          style={{
            fontSize: 28,
            letterSpacing: "0.28em",
            textTransform: "uppercase",
            color: "#f87171",
            marginBottom: 24,
          }}
        >
          nomorebets.io
        </div>
        <div style={{ fontSize: 72, fontWeight: 700, lineHeight: 1.05, letterSpacing: "-0.04em" }}>
          No More Bets
        </div>
        <div style={{ marginTop: 20, fontSize: 32, color: "#a1a1aa", maxWidth: 820, lineHeight: 1.3 }}>
          AI football research agent — public briefs, slips, and bankroll.
        </div>
      </div>
    ),
    { ...size },
  );
}
