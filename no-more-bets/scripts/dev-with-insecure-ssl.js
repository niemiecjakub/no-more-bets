/**
 * Sets NODE_TLS_REJECT_UNAUTHORIZED=0 so the dev server can fetch from
 * https://localhost (self-signed cert), then runs Next.js dev.
 */
process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const { spawn } = require("child_process");
const path = require("path");

const nextBin = path.join(__dirname, "..", "node_modules", "next", "dist", "bin", "next");
const child = spawn(process.execPath, [nextBin, "dev"], {
  stdio: "inherit",
  env: process.env,
  cwd: path.join(__dirname, ".."),
});
child.on("exit", (code) => process.exit(code ?? 0));
