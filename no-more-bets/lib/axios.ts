import axios from "axios";

function getApiBaseUrl(): string {
  const raw = process.env.NEXT_PUBLIC_API_URL ?? "";
  const base = raw.trim().replace(/\/$/, "");
  if (!base) {
    throw new Error(
      "NEXT_PUBLIC_API_URL is not set. Add it to .env.local (e.g. NEXT_PUBLIC_API_URL=https://localhost:5001)."
    );
  }
  return base;
}

const axiosInstance = axios.create({
  baseURL: getApiBaseUrl(),
  headers: {
    "Content-Type": "application/json",
  },
});

axiosInstance.interceptors.request.use(
  (config) => config,
  (error) => Promise.reject(error)
);

export default axiosInstance;
