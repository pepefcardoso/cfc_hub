/** @type {import('next').NextConfig} */
const nextConfig = {
  output: process.env.NODE_ENV === "development" ? undefined : "standalone",
  experimental: {
    cpus: 2,
  },
};

export default nextConfig;
